using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend.Services;

// this analyzer warning is pointing to `alias` as a keyword, but seems like a false positive with the `@` character in front of it
#pragma warning disable CA1716

/// <summary>
/// Service that creates new shortened urls and queries what's in the persistence layer
/// </summary>
public interface IUrlService
{
    /// <summary>
    /// Create a shortened url from client input
    /// </summary>
    public Task<ValueResult<ShortenedUrl>> Create(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query url's stored in the server
    /// </summary>
    public IQueryable<ShortenedUrl> Query();

    /// <summary>
    /// Lookup a url with a shortned alias
    /// </summary>
    public Task<ValueResult<string>> Lookup(string @alias, CancellationToken cancellationToken = default);
}
