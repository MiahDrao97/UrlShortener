using System.Runtime.CompilerServices;

namespace UrlShortener.Backend;

public class Err
{
    /// <summary>
    /// Construct an error response
    /// </summary>
    /// <remarks>
    /// Leave the arguments as default since that will automatically capture the caller file path, calling member, and line number
    /// </remarks>
    public Err(
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        CalledFrom = $"{filePath}<{memberName}>:{lineNumber}";
    }

    internal Err(Err other, string? filePath, string? memberName, int lineNumber)
    {
        Message = other.Message;
        Exception = other.Exception;
        Code = other.Code;
        CalledFrom = $"{other.CalledFrom}\n    {filePath}<{memberName}>:{lineNumber}";
    }

    /// <summary>
    /// Message to show the client
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Exception, if relevant
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Called from this location
    /// </summary>
    public string CalledFrom { get; }

    /// <summary>
    /// Error category, if relevant
    /// </summary>
    public int Code { get; init; }
}
