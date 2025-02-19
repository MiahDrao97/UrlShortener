namespace UrlShortener.Backend.Data;

/// <summary>
/// Used to record telemetry on our shortened url usage
/// </summary>
public sealed class UrlTelemetry
{
    /// <summary>
    /// Datetime the shortened url was hit
    /// </summary>
    public required DateTime DateHit { get; set; }

    /// <summary>
    /// Which url was hit (using row id)
    /// </summary>
    public required int RowId { get; set; }
}
