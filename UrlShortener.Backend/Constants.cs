namespace UrlShortener.Backend;

/// <summary>
/// Constant values
/// </summary>
public static class Constants
{
    /// <summary>
    /// Specific error categories we'll be returning
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Resource was not found
        /// </summary>
        public const string NotFound = nameof(NotFound);

        /// <summary>
        /// Input from client failed validation
        /// </summary>
        public const string ClientError = nameof(ClientError);
    }
}
