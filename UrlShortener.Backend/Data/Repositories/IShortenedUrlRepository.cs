using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend.Data.Repositories;

/// <summary>
/// Repository for <see cref="ShortenedUrl"/> table
/// </summary>
public interface IShortenedUrlRepository
{
    /// <summary>
    /// Get all <see cref="ShortenedUrl"/> that match the given <paramref name="urlAlias"/>
    /// </summary>
    /// <param name="urlAlias">Shortened url alias</param>
    public Task<ValueResult<ShortenedUrl[]>> GetByAlias(string urlAlias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query the <see cref="ShortenedUrl"/> table
    /// </summary>
    public IQueryable<ShortenedUrl> Query();

    /// <summary>
    /// Insert a new <see cref="ShortenedUrl"/> row
    /// </summary>
    public Task<ValueResult<ShortenedUrl>> Insert(ShortenedUrl row, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a <see cref="ShortenedUrl"/>
    /// </summary>
    public Task<Result> Update(ShortenedUrl row, CancellationToken cancellationToken = default);
}
