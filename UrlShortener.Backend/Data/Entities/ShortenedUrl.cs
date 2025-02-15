using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Backend.Data.Entities;

/// <summary>
/// Our shortened url database table
/// </summary>
public sealed class ShortenedUrl
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// The shortened url
    /// </summary>
    [Required]
    public string Alias { get; set; } = null!;

    /// <summary>
    /// Full url passed in from client (we redirect here)
    /// </summary>
    [Required]
    public string FullUrl { get; set; } = null!;

    /// <summary>
    /// Date created
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Number of times this alias has been used
    /// </summary>
    public int Hits { get; set; }
}
