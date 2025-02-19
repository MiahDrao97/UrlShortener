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
    public Task<ShortenedUrl[]> GetByAlias(string urlAlias, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlAlias);
        return GetByAliasCore(urlAlias, cancellationToken);
    }

    private async Task<ShortenedUrl[]> GetByAliasCore(string alias, CancellationToken cancellationToken)
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
            throw;
        }
    }

    /// <inheritdoc />
    public IQueryable<ShortenedUrl> Query() => _context.ShortenedUrls;

    /// <inheritdoc />
    public Task<ShortenedUrl> Insert(ShortenedUrl row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(row);
        return InsertCore(row, cancellationToken);
    }

    private async Task<ShortenedUrl> InsertCore(ShortenedUrl row, CancellationToken cancellationToken)
    {
        try
        {
            EntityEntry<ShortenedUrl> added = await _context.ShortenedUrls.AddAsync(row, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully inserted row with alias '{alias}' from url '{url}'", row.Alias, row.FullUrl);
            return added.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert row with alias '{alias}' from url '{url}'", row.Alias, row.FullUrl);
            throw;
        }
    }

    /// <inheritdoc />
    public Task Update(ShortenedUrl row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(row);
        return UpdateCore(row, cancellationToken);
    }

    private async Task UpdateCore(ShortenedUrl row, CancellationToken cancellationToken)
    {
        try
        {
            _context.Update(row);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update row with alias '{alias}' from url '{url}'", row.Alias, row.FullUrl);
            throw;
        }
    }
}
