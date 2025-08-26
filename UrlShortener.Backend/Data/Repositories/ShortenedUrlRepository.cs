using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend.Data.Repositories;

/// <inheritdoc cref="IShortenedUrlRepository" />
public sealed class ShortenedUrlRepository(
    UrlShortenerContext context,
    ILogger<ShortenedUrlRepository> logger) : IShortenedUrlRepository
{
    private readonly UrlShortenerContext _context = context;
    private readonly ILogger<ShortenedUrlRepository> _logger = logger;

    /// <inheritdoc />
    public Task<Attempt<ShortenedUrl[]>> GetByAlias(string urlAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(urlAlias))
        {
            return Task.FromResult<Attempt<ShortenedUrl[]>>(new Err { Message = $"{nameof(urlAlias)} cannot be null/white space. Found: '{urlAlias ?? "<null>"}'" });
        }
        return GetByAliasCore(urlAlias, cancellationToken);
    }

    private async Task<Attempt<ShortenedUrl[]>> GetByAliasCore(string alias, CancellationToken cancellationToken)
    {
        try
        {
            return await _context
                .ShortenedUrls
                .Where(s => s.Alias == alias)
                .ToArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch all shortened url's with alias '{alias}'", alias);
            return new Err
            {
                Exception = ex,
                Message = $"Failed to fetch all shortened url's with alias '{alias}'",
            };
        }
    }

    /// <inheritdoc />
    public IQueryable<ShortenedUrl> Query() => _context.ShortenedUrls;

    /// <inheritdoc />
    public Task<Attempt<ShortenedUrl>> Insert(ShortenedUrl row, CancellationToken cancellationToken = default)
    {
        if (row is null)
        {
            return Task.FromResult<Attempt<ShortenedUrl>>(new Err { Message = $"{row} cannot be null" });
        }
        return InsertCore(row, cancellationToken);
    }

    private async Task<Attempt<ShortenedUrl>> InsertCore(ShortenedUrl row, CancellationToken cancellationToken)
    {
        try
        {
            EntityEntry<ShortenedUrl> added = await _context.ShortenedUrls.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully inserted row with alias '{alias}' (actual: {actualAlias}, offset: {offset}) from url '{url}'",
                row.UrlSafeAlias,
                row.Alias,
                row.Offset,
                row.FullUrl);
            return added.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert row with alias '{alias}' (actual: {actualAlias}, offset: {offset}) from url '{url}'",
                row.UrlSafeAlias,
                row.Alias,
                row.Offset,
                row.FullUrl);
            return new Err
            {
                Exception = ex,
                Message = $"Failed to insert row with alias '{row.UrlSafeAlias}' (actual: {row.Alias}, offset: {row.Offset}) from url '{row.FullUrl}'",
            };
        }
    }

    /// <inheritdoc />
    public Task<Attempt> Update(ShortenedUrl row, CancellationToken cancellationToken = default)
    {
        if (row is null)
        {
            return Task.FromResult<Attempt>(new Err { Message = $"{row} cannot be null" });
        }
        return UpdateCore(row, cancellationToken);
    }

    private async Task<Attempt> UpdateCore(ShortenedUrl row, CancellationToken cancellationToken)
    {
        try
        {
            _context.Update(row);
            await _context.SaveChangesAsync(cancellationToken);
            return Attempt.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update row with alias '{alias}' (actual: {actualAlias}, offset: {offset}) from url '{url}'",
                row.UrlSafeAlias,
                row.Alias,
                row.Offset,
                row.FullUrl);
            return new Err
            {
                Exception = ex,
                Message = $"Failed to update row with alias '{row.UrlSafeAlias}' (actual: {row.Alias}, offset: {row.Offset}) from url '{row.FullUrl}'",
            };
        }
    }
}
