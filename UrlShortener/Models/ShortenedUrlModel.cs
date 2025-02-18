namespace UrlShortener.Models;

/// <summary>
/// Model for a shortened url
/// </summary>
public class ShortenedUrlModel
{
    /// <summary>
    /// Url that's being aliased
    /// </summary>
    public required string FullUrl { get; set; }

    /// <summary>
    /// Alias for <see cref="FullUrl"/>
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Hostname of this server (ya know, cuz it hosts these aliases)
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Full alias url (alias appended to host name)
    /// </summary>
    public string FullAliasUrl => string.IsNullOrWhiteSpace(HostName) ? Alias : $"{HostName}/{Alias}";
}
