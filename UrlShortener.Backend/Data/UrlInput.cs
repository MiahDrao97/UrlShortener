using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Backend.Data;

/// <summary>
/// Input model
/// </summary>
public class UrlInput
{
    /// <summary>
    /// Url input from the client
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = null!;
}
