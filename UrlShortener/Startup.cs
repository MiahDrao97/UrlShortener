namespace UrlShortener;

/// <summary>
/// Startup class that handles dependency injection and app configuration
/// </summary>
public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
    }
}
