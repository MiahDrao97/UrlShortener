using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UrlShortener.Backend.Data;

namespace UrlShortener.Backend.Services;

/// <summary>
/// Background service that handles telemetry on our shortened URL usage
/// </summary>
public sealed class TelemetryBackgroundService(
    IServiceProvider services,
    Channel<UrlTelemetry> channel,
    ILogger<TelemetryBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScope _scope = services.CreateScope();
    private readonly Channel<UrlTelemetry> _channel = channel;
    private readonly ILogger<TelemetryBackgroundService> _logger = logger;

    private IUrlService? _urlService;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_channel.Reader.TryRead(out UrlTelemetry? item))
            {
                Result result = await GetUrlService().RecordHit(item, stoppingToken);
                if (!result.Success)
                {
                    _logger.LogError(result.Error.Exception, "Encountered failure while recording hit for url {rid}: {reason} --> {calledFrom}",
                        item.RowId,
                        result.Error.Message,
                        result.Error.CalledFrom);
                }
                else if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Successfully recorded hit for url {rid}", item.RowId);
                }
            }
            _logger.LogTrace("Channel did not have an item to read. Waiting...");
            // this should be an app setting
            await Task.Delay(500, stoppingToken);
        }
    }

    private IUrlService GetUrlService()
    {
        return _urlService ??= _scope.ServiceProvider.GetRequiredService<IUrlService>();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _scope.Dispose();
        base.Dispose();
    }
}
