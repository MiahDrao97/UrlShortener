using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Backend.Models;

/// <summary>
/// Input model
/// </summary>
public class UrlInputModel
{
    /// <summary>
    /// Url input from the client
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = null!;
}
