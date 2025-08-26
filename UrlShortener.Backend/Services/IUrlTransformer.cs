namespace UrlShortener.Backend.Services;

#pragma warning disable CA1716

/// <summary>
/// Transforms a URL to an alias
/// </summary>
public interface IUrlTransformer
{
    /// <summary>
    /// Create an alias
    /// </summary>
    public Attempt<string> CreateAlias(string input);

    /// <summary>
    /// Transform an alias to be url-safe, using both its original form and offset
    /// </summary>
    public string CreateUrlSafeAlias(string? @alias, short offset);

    /// <summary>
    /// Takens a url-safe alias and transforms it into its original value
    /// </summary>
    /// <returns>A tuple containing the input's original alias value and the offset for tracking collisions</returns>
    public Attempt<(string, short)> FromUrlSafeAlias(string input);
}
