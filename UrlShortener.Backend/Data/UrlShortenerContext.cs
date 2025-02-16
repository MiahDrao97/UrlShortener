using System.Reflection;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend.Data;

/// <summary>
/// DB Context for this url shortener
/// </summary>
public sealed class UrlShortenerContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ShortenedUrl> ShortenedUrls => Set<ShortenedUrl>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
