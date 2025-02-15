using Microsoft.AspNetCore.OData.Query;
using UrlShortener.Backend.Models;

namespace UrlShortener.Backend.Services;

/// <summary>
/// Service that creates new shortened urls and queries what's in the persistence layer
/// </summary>
public interface IUrlService
{
    /// <summary>
    /// Create a shortened url from client input
    /// </summary>
    public Task<PayloadResult<ShortenedUrlModel>> Create(UrlInputModel input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query url's stored in the server
    /// </summary>
    public IQueryable<ShortenedUrlModel> Query(ODataQueryOptions queryOptions);

    /// <summary>
    /// Lookup a url with a shortned alias
    /// </summary>
    public Task<PayloadResult<string>> Lookup(string urlAlias, CancellationToken cancellationToken = default);
}
