namespace UrlShortener.Backend;

/// <summary>
/// All extensions in one place
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Transform a base64 string to be url-safe
    /// </summary>
    public static string Base64ToUrlSafe(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        Span<char> chars = stackalloc char[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            chars[i] = str[i] switch
            {
                '/' => '_',
                '+' => '.',
                '=' => '~',
                _ => str[i]
            };
        }
        return new string(chars);
    }

    /// <summary>
    /// Undo operation for <see cref="Base64ToUrlSafe(string)"/>
    /// </summary>
    public static string UrlSafeToStandardBase64(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        Span<char> chars = stackalloc char[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            chars[i] = str[i] switch
            {
                '_' => '/',
                '.' => '+',
                '~' => '=',
                _ => str[i]
            };
        }
        return new string(chars);
    }
}
