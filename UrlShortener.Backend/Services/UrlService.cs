using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;

namespace UrlShortener.Backend.Services;

/// <inheritdoc cref="IUrlService" />
public sealed class UrlService(
    IShortenedUrlRepository repository,
    ILogger<UrlService> logger) : IUrlService
{
    private readonly IShortenedUrlRepository _repository = repository;
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
            ValueResult<string> aliasResult = CreateAlias($"{uri.DnsSafeHost}{uri.PathAndQuery}"); // leave out scheme
            if (!aliasResult.IsSuccess(out string? @alias))
            {
                _logger.LogError(aliasResult.Error.Exception, "Encountered unexpected error while creating alias for '{input}': {reason} -> {calledFrom}",
                    fullUrl,
                    aliasResult.Error.Message,
                    aliasResult.Error.CalledFrom);
                return ValueResult<ShortenedUrl>.FromError(aliasResult);
            }

            ShortenedUrl[] existing = await _repository.GetByAlias(@alias, cancellationToken);

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
            string urlSafeAlias = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{@alias}{existing.Length}")).Base64ToUrlSafe();
            newRow.UrlSafeAlias = urlSafeAlias;

            newRow = await _repository.Insert(newRow, cancellationToken);
            return new Ok<ShortenedUrl>(newRow);
        }
        catch (Exception ex)
        {
            // should not be catching at this point
            _logger.LogError(ex, "Uncaught exception while creating shortened url for '{fullUrl}'", fullUrl);
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
            return Task.FromResult<ValueResult<string>>(new ErrorResult { Message = $"Shortened url cannot be null or whitespace. Was: '{@alias ?? "<null>"}'" });
        }
        // should be base64
        Span<byte> aliasBytes = stackalloc byte[17]; // 16 bytes for MD5 hash + 1 for offset
        if (!Convert.TryFromBase64Chars(@alias.UrlSafeToStandardBase64(), aliasBytes, out int bytesWritten))
        {
            _logger.LogError("Alias '{alias}' is not base64-encoded. All aliases are returned as a base64-encoded string, so we can safely assume this alias does not exist in this system.", @alias);
            return Task.FromResult<ValueResult<string>>(new ErrorResult
            {
                Message = $"No urls found with alias '{@alias}'",
                Category = Constants.Errors.NotFound,
            });
        }
        string trueAlias = Encoding.ASCII.GetString(aliasBytes);
        if (trueAlias.Length != 17)
        {
            _logger.LogError("Alias '{@alias}' (decoded '{decoded}') is not exactly 17 bytes (ASCII characters). All aliases are 17 characters long, so we're inferring the alias does not exist in this system.", @alias, trueAlias);
            return Task.FromResult<ValueResult<string>>(new ErrorResult
            {
                Message = $"No urls found with alias '{@alias}'",
                Category = Constants.Errors.NotFound,
            });
        }
        return LookupCore(trueAlias, cancellationToken);
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

    private ValueResult<string> CreateAlias(string input)
    {
        _logger.LogDebug("Creating hash for '{input}'", input);
        Span<byte> bytes = stackalloc byte[16]; // hash results in 128 bits (16 bytes)
        int written;
        try
        {
            // choosing to disable this warning because we're not using this for cryptographic purposes
#pragma warning disable CA5351
            // this hash algorithm creates a 128-bit hash, regardless of the input: perfect for creating aliases of the same length
            written = MD5.HashData(Encoding.UTF8.GetBytes(input), bytes);
#pragma warning restore CA5351
        }
        catch (Exception ex)
        {
            // not entirely sure what would cause this...
            _logger.LogError(ex, "Unexpected error while hashing '{input}' with MD5 algorithm", input);
            return new ErrorResult
            {
                Exception = ex,
                Message = $"Unexpected error while hashing '{input}' with MD5 algorithm.",
            };
        }

        // or this...
        if (written != 16)
        {
            _logger.LogError("Encountered unexpected behavior from MD5 algorithm while creating alias for input '{input}'", input);
            return new ErrorResult
            {
                Message = $"Unexpected behavior: MD5 algorithm wrote {written} bytes instead of 16 for input '{input}'.",
            };
        }

        return new Ok<string>(Encoding.ASCII.GetString(bytes));

    }
}
