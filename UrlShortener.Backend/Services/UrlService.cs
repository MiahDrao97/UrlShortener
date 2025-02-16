using System.Globalization;
using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Models;

namespace UrlShortener.Backend.Services;

/// <inheritdoc cref="IUrlService" />
public sealed class UrlService(
    IUrlTransformer transformer,
    IShortenedUrlRepository repository,
    IMapper mapper,
    ILogger<UrlService> logger) : IUrlService
{
    private readonly IUrlTransformer _transformer = transformer;
    private readonly IShortenedUrlRepository _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UrlService> _logger = logger;

    private static readonly UriCreationOptions _uriOpts = new() { DangerousDisablePathAndQueryCanonicalization = true };

    /// <inheritdoc />
    public Task<ValueResult<ShortenedUrlModel>> Create(UrlInputModel input, CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            // 500 on this one: the world is fundamentally broken
            _logger.LogError("Received null '{param}'", nameof(input));
            return Task.FromResult<ValueResult<ShortenedUrlModel>>(new ErrorResult
            {
                Message = $"{nameof(input)} cannot be null",
            });
        }
        if (!Uri.TryCreate(input.Url, in _uriOpts, out Uri? uri))
        {
            return Task.FromResult<ValueResult<ShortenedUrlModel>>(new ErrorResult
            {
                Message = $"Invalid URL '{input.Url}'",
                Category = Constants.Errors.ClientError
            });
        }
        if (!uri.Scheme.StartsWith("http", StringComparison.InvariantCulture))
        {
            return Task.FromResult<ValueResult<ShortenedUrlModel>>(new ErrorResult
            {
                Message = $"Submitted URL must use http(s) scheme. Found: '{input.Url}'",
                Category = Constants.Errors.ClientError,
            });
        }
        return CreateCore(input.Url, cancellationToken);
    }

    private async Task<ValueResult<ShortenedUrlModel>> CreateCore(string fullUrl, CancellationToken cancellationToken)
    {
        try
        {
            ValueResult<string> aliasResult = _transformer.CreateAlias(fullUrl);
            if (!aliasResult.IsSuccess(out string? @alias))
            {
                _logger.LogError(aliasResult.Error.Exception, "Encountered error while creating alias for '{input}': {reason} -> {calledFrom}",
                    fullUrl,
                    aliasResult.Error.Message,
                    aliasResult.Error.CalledFrom);
                return ValueResult<ShortenedUrlModel>.FromError(aliasResult);
            }

            ShortenedUrl[] existing = await _repository.GetByAlias(@alias, cancellationToken);

            // alias is 16 chars, and then the 17th handles up to 10 collisions (we're talking heat-death-of-the-universe type of collision-checking here)
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
                return new Ok<ShortenedUrlModel>(_mapper.Map<ShortenedUrlModel>(duplicate));
            }

            ShortenedUrl newRow = new()
            {
                FullUrl = fullUrl,
                Alias = @alias,
                Offset = (short)existing.Length,
                Created = DateTime.UtcNow,
            };

            newRow = await _repository.Insert(newRow, cancellationToken);
            return new Ok<ShortenedUrlModel>(_mapper.Map<ShortenedUrlModel>(newRow));
        }
        catch (Exception ex)
        {
            // should not be catching at this point
            _logger.LogError(ex, "Uncaught exception while creating shortened url for '{fullUrl}'", fullUrl);
            return new ErrorResult
            {
                Message = $"Uncaught exception while creating shortened url for '{fullUrl}'",
            };
        }
    }

    /// <inheritdoc />
    public IQueryable<ShortenedUrlModel> Query(ODataQueryOptions queryOptions)
    {
        ArgumentNullException.ThrowIfNull(queryOptions);
        // TODO : Project to model type
        throw new NotImplementedException();
        // return _repository.Query().ProjectTo<ShortenedUrlModel>();
    }

    /// <inheritdoc />
    public Task<ValueResult<string>> Lookup(string @alias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(@alias))
        {
            return Task.FromResult<ValueResult<string>>(new ErrorResult { Message = $"Shortened url cannot be null or whitespace. Was: '{@alias ?? "<null>"}'" });
        }
        if (@alias.Length != 17)
        {
            _logger.LogError("Alias '{@alias}' is not exactly 17 characters. All aliases are 17 characters long, so we're inferring the alias does not exist in this system.", @alias);
            return Task.FromResult<ValueResult<string>>(new ErrorResult
            {
                Message = $"No urls found with alias '{@alias}'",
                Category = Constants.Errors.NotFound,
            });
        }
        return LookupCore(@alias, cancellationToken);
    }

    private async Task<ValueResult<string>> LookupCore(string @alias, CancellationToken cancellationToken)
    {
        try
        {
            ShortenedUrl[] found = await _repository.GetByAlias(@alias[..16], cancellationToken);
            if (found.Length == 0)
            {
                // most likely not found scenario
                _logger.LogDebug("No urls found with alias '{alias}'", @alias);
                return new ErrorResult
                {
                    Message = $"No urls found with alias '{@alias}'",
                    Category = Constants.Errors.NotFound,
                };
            }

            if (found.Length > 1)
            {
                _logger.LogDebug("Found {count} urls stored with the same alias '{alias}'", found.Length, @alias);
                if (!int.TryParse([@alias[^1]], CultureInfo.InvariantCulture, out int offset))
                {
                    _logger.LogError("Final character {char} did not parse to a valid offset. Returning not found for alias '{alias}'", @alias[^1], @alias);
                    return new ErrorResult
                    {
                        Message = $"No urls found with alias '{@alias}'",
                        Category = Constants.Errors.NotFound,
                    };
                }

                if (found.FirstOrDefault(x => x.Offset == offset) is ShortenedUrl success)
                {
                    // TODO : record hits in bkgd service
                    return new Ok<string>(success.FullUrl);
                }

                _logger.LogError("Final character {char} did not match an existing offset. Returning not found for alias '{alias}'", @alias[^1], @alias);
                return new ErrorResult
                {
                    Message = $"No urls found with alias '{@alias}'",
                    Category = Constants.Errors.NotFound,
                };
            }

            // TODO : record hits in bkgd service
            return new Ok<string>(found.First().FullUrl);
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
}
