using System.ComponentModel.DataAnnotations;
using AutoMapper.Configuration.Annotations;

namespace UrlShortener.Backend.Data;

/// <summary>
/// Shortned url model
/// </summary>
public class ShortenedUrlOuput
{
    /// <summary>
    /// Key required by OData, but we're gonna ignore it because it doesn't mean anything to the client
    /// </summary>
    [Key]
    [Ignore]
    public int RowId { get; set; }

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
