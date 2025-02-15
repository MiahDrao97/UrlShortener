using AutoMapper;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using OneOf.Types;
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
    public Task<PayloadResult<ShortenedUrlModel>> Create(UrlInputModel input, CancellationToken cancellationToken = default)
    {
        if (input is null)
        {
            // 500 on this one: the world is fundamentally broken
            ArgumentNullException ex = new(nameof(input));
            _logger.LogError(ex, "Received null '{param}'", nameof(input));
            return Task.FromResult<PayloadResult<ShortenedUrlModel>>(ex);
        }
        if (!Uri.TryCreate(input.Url, in _uriOpts, out Uri? uri))
        {
            return Task.FromResult<PayloadResult<ShortenedUrlModel>>(new ErrorResponse { Message = $"Invalid URL '{input.Url}'" });
        }
        if (!uri.Scheme.StartsWith("http", StringComparison.InvariantCulture))
        {
            return Task.FromResult<PayloadResult<ShortenedUrlModel>>(new ErrorResponse { Message = $"Submitted URL must use http(s) scheme. Found: '{input.Url}'" });
        }
        return CreateCore(input.Url, cancellationToken);
    }

    private async Task<PayloadResult<ShortenedUrlModel>> CreateCore(string fullUrl, CancellationToken cancellationToken)
    {
        try
        {
            string alias = _transformer.CreateAlias(fullUrl);

            ShortenedUrl newRow = new()
            {
                FullUrl = fullUrl,
                Alias = _transformer.CreateAlias(fullUrl),
                Created = DateTime.UtcNow,
            };

            // TODO : collision detection

            newRow = await _repository.Insert(newRow, cancellationToken);
            return new Ok<ShortenedUrlModel>(_mapper.Map<ShortenedUrlModel>(newRow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating shortened url for '{fullUrl}'", fullUrl);
            return ex;
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
    public Task<PayloadResult<string>> Lookup(string urlAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(urlAlias))
        {
            return Task.FromResult<PayloadResult<string>>(new ErrorResponse { Message = $"Shortened url cannot be null or whitespace. Was: '{urlAlias ?? "<null>"}'" });
        }
        return LookupCore(urlAlias, cancellationToken);
    }

    private async Task<PayloadResult<string>> LookupCore(string alias, CancellationToken cancellationToken)
    {
        try
        {
            ShortenedUrl[] found = await _repository.GetByAlias(alias, cancellationToken);
            if (found.Length == 0)
            {
                _logger.LogDebug("No urls found with alias '{alias}'", alias);
                return new NotFound();
            }

            if (found.Length > 1)
            {
                // shouldn't be a possible scenario since we're handling collisions, but just in case...
                _logger.LogWarning("Found {count} urls stored with the same alias '{alias}'", found.Length, alias);
            }

            // TODO : record hits in bkgd service
            return new Ok<string>(found.First().FullUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while looking up stored url with alias '{alias}'", alias);
            return ex;
        }
    }
}
