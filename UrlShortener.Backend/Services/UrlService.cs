using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;

namespace UrlShortener.Backend.Services;

/// <inheritdoc cref="IUrlService" />
public sealed class UrlService(
    IUrlTransformer transformer,
    IShortenedUrlRepository repository,
    Channel<UrlTelemetry> channel,
    ILogger<UrlService> logger) : IUrlService
{
    private readonly IUrlTransformer _transformer = transformer;
    private readonly IShortenedUrlRepository _repository = repository;
    private readonly Channel<UrlTelemetry> _channel = channel;
    private readonly ILogger<UrlService> _logger = logger;

    private static readonly UriCreationOptions _uriOpts = new() { DangerousDisablePathAndQueryCanonicalization = true };

    /// <inheritdoc />
    public Task<ValueResult<ShortenedUrl>> Create(string input, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(input, in _uriOpts, out Uri? uri))
        {
            return Task.FromResult<ValueResult<ShortenedUrl>>(new ErrorResult
            {
                Message = $"Invalid URL '{input}'",
                Category = Constants.Errors.ClientError
            });
        }
        if (!uri.Scheme.StartsWith("http", StringComparison.InvariantCulture))
        {
            return Task.FromResult<ValueResult<ShortenedUrl>>(new ErrorResult
            {
                Message = $"Submitted URL must use http(s) scheme. Found: '{input}'",
                Category = Constants.Errors.ClientError,
            });
        }
        return CreateCore(input, uri, cancellationToken);
    }

    private async Task<ValueResult<ShortenedUrl>> CreateCore(string fullUrl, Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            ValueResult<string> aliasResult = _transformer.CreateAlias($"{uri.DnsSafeHost}{uri.PathAndQuery}"); // leave out scheme
            if (!aliasResult.IsSuccess(out string? @alias))
            {
                _logger.LogError(aliasResult.Error.Exception, "Encountered unexpected error while creating alias for '{input}': {reason} -> {calledFrom}",
                    fullUrl,
                    aliasResult.Error.Message,
                    aliasResult.Error.CalledFrom);
                return ValueResult<ShortenedUrl>.FromError(aliasResult);
            }

            ValueResult<ShortenedUrl[]> queryResult = await _repository.GetByAlias(@alias, cancellationToken);
            if (!queryResult.IsSuccess(out ShortenedUrl[]? existing))
            {
                _logger.LogError(queryResult.Error.Exception, "Encountered unexpected error while querying for alias '{alias}': {reason} -> {calledFrom}",
                    @alias,
                    queryResult.Error.Message,
                    queryResult.Error.CalledFrom);
                return ValueResult<ShortenedUrl>.FromError(queryResult);
            }

            // alias is 16 chars, and then the 17th handles up to 10 collisions (seems like a safe amount of collision-checking here)
            if (existing.Length >= 10)
            {
                _logger.LogCritical("Reached 10 collisions for alias '{alias}'. A new alias creation strategy is likely necessary.", @alias);
                return new ErrorResult
                {
                    Message = $"Reached 10 collisions for alias '{@alias}'",
                };
            }

            if (existing.FirstOrDefault(e => e.FullUrl.Equals(fullUrl, StringComparison.Ordinal)) is ShortenedUrl duplicate)
            {
                _logger.LogDebug("Encountered an existing entry for url: '{fullUrl}' (row id = {rid}).", fullUrl, duplicate.RowId);
                return new Ok<ShortenedUrl>(duplicate);
            }

            ShortenedUrl newRow = new()
            {
                FullUrl = fullUrl,
                Alias = @alias,
                Offset = (short)existing.Length,
                Created = DateTime.UtcNow,
            };
            newRow.UrlSafeAlias = _transformer.CreateUrlSafeAlias(@alias, newRow.Offset);

            return await _repository.Insert(newRow, cancellationToken);
        }
        catch (Exception ex)
        {
            // should not be catching at this point
            _logger.LogCritical(ex, "Uncaught exception while creating shortened url for '{fullUrl}'", fullUrl);
            return new ErrorResult
            {
                Message = $"Uncaught exception while creating shortened url for '{fullUrl}'",
                Exception = ex,
            };
        }
    }

    /// <inheritdoc />
    public IQueryable<ShortenedUrl> Query()
    {
        return _repository.Query().AsNoTracking();
    }

    /// <inheritdoc />
    public Task<ValueResult<string>> Lookup(string @alias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(@alias))
        {
            // returning "NotFound" on null/whitespace input
            return Task.FromResult<ValueResult<string>>(new ErrorResult
            {
                Message = $"Shortened url cannot be null or whitespace. Was: '{@alias ?? "<null>"}'",
                Category = Constants.Errors.NotFound
            });
        }

        ValueResult<(string, short)> decodeResult = _transformer.FromUrlSafeAlias(@alias);
        if (!decodeResult.IsSuccess(out (string Alias, short Offset) decoded))
        {
            _logger.LogError(decodeResult.Error.Exception, "Assuming this system did not create alias '{alias}' since it could not be decoded: {reason} --> {calledFrom}",
                @alias,
                decodeResult.Error.Message,
                decodeResult.Error.CalledFrom);

            // change to "NotFound" since our system could not have created this alias
            decodeResult.Error.Category = Constants.Errors.NotFound;

            return Task.FromResult(ValueResult<string>.FromError(decodeResult));
        }

        return LookupCore(@alias, decoded.Alias, decoded.Offset, cancellationToken);
    }

    private async Task<ValueResult<string>> LookupCore(string @alias, string trueAlias, short offset, CancellationToken cancellationToken)
    {
        try
        {
            ValueResult<ShortenedUrl[]> queryResult = await _repository.GetByAlias(trueAlias, cancellationToken);
            if (!queryResult.IsSuccess(out ShortenedUrl[]? found))
            {
                _logger.LogError(queryResult.Error.Exception, "Encountered unexpected error while querying for alias '{alias}': {reason} -> {calledFrom}",
                    @alias,
                    queryResult.Error.Message,
                    queryResult.Error.CalledFrom);
                return ValueResult<string>.FromError(queryResult);
            }

            if (found.Length == 0)
            {
                // most likely scenario for not finding anything
                _logger.LogDebug("No urls found with alias '{alias}'", @alias);
                return new ErrorResult
                {
                    Message = $"No urls found with alias '{@alias}'",
                    Category = Constants.Errors.NotFound,
                };
            }

            _logger.LogDebug("Found {count} urls stored with the same alias '{alias}'", found.Length, @alias);
            if (found.FirstOrDefault(x => x.Offset == offset) is ShortenedUrl success)
            {
                // notify background service to record telemetry
                await _channel.Writer.WriteAsync(new UrlTelemetry
                {
                    DateHit = DateTime.UtcNow,
                    RowId = success.RowId
                }, cancellationToken);
                return new Ok<string>(success.FullUrl);
            }
            else
            {
                _logger.LogWarning("Found {count} urls stored with the same alias '{alias}', but none with offset {offset}", found.Length, @alias, offset);
                return new ErrorResult
                {
                    Message = $"No urls found with alias '{@alias}'",
                    Category = Constants.Errors.NotFound,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while looking up stored url with alias '{alias}'", @alias);
            return new ErrorResult
            {
                Message = $"Unexpected error while looking up stored url with alias '{alias}'",
            };
        }
    }

    /// <inheritdoc />
    public Task<Result> RecordHit(UrlTelemetry telemetry, CancellationToken cancellationToken = default)
    {
        if (telemetry is null)
        {
            return Task.FromResult<Result>(new ErrorResult { Message = $"{nameof(telemetry)} cannot be null" });
        }
        return RecordHitCore(telemetry, cancellationToken);
    }

    private async Task<Result> RecordHitCore(UrlTelemetry telemetry, CancellationToken cancellationToken)
    {
        try
        {
            ShortenedUrl? row = await _repository.Query().FirstOrDefaultAsync(u => u.RowId == telemetry.RowId, cancellationToken);
            if (row is null)
            {
                return new ErrorResult
                {
                    Category = Constants.Errors.NotFound,
                    Message = $"Row with row id {telemetry.RowId} was not found.",
                };
            }

            row.Hits += 1;
            row.LastHit = telemetry.DateHit;
            return await _repository.Update(row, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Encountered unexpected error while recording hit on url {rid}", telemetry.RowId);
            return new ErrorResult
            {
                Exception = ex,
                Message = $"Encountered unexpected error while recording hit on url {telemetry.RowId}",
            };
        }
    }
}
