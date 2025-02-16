namespace UrlShortener.Models;

/// <summary>
/// Indicate that a given alias wasn't found
/// </summary>
public class UrlNotFoundModel
{
    /// <summary>
    /// Alias in question
    /// </summary>
    public required string Alias { get; init; }

    /// <remarks>
    /// Current hostname
    /// </remarks>
    public string? HostName { get; init; }

    /// <summary>
    /// Full alias path including hostname
    /// </summary>
    public string FullAliasPath => string.IsNullOrEmpty(HostName) ? Alias : $"https://{HostName}/{Alias}";
}
