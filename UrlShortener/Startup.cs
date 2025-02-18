using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Services;

namespace UrlShortener;

/// <summary>
/// Startup class that handles dependency injection and app configuration
/// </summary>
public class Startup
{
    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();

        services.AddScoped<IUrlService, UrlService>();
        services.AddScoped<IShortenedUrlRepository, ShortenedUrlRepository>();

        services.AddAutoMapper(static opts => opts.AddMaps([typeof(ShortenedUrlProfile)]));

        services.AddODataQueryFilter();
    }

    /// <summary>
    /// Configure the database
    /// </summary>
    public void ConfigureDatabase(IServiceCollection services)
    {
        // TODO : SQLite for non-test
        services.AddDbContext<UrlShortenerContext>(static opts =>
        {
            opts.UseInMemoryDatabase("UrlShortener", static configure => { configure.EnableNullChecks(); });
        });
    }
}
