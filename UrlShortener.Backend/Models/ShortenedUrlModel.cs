namespace UrlShortener.Backend.Models;

/// <summary>
/// Shortned url model
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
}
