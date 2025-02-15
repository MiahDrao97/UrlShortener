namespace UrlShortener.Backend.Services;

/// <summary>
/// Transforms an input into a shortened alias
/// </summary>
public interface IUrlTransformer
{
    /// <summary>
    /// Create an alias from a given input
    /// </summary>
    /// <remarks>
    /// Does not validate whether or not <paramref name="input"/> is actually a URL since that has no effect on the output. Caller should verify first.
    /// </remarks>
    public string CreateAlias(string input);
}
