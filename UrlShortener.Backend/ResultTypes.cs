using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UrlShortener.Backend;

/// <summary>
/// Ok result (200)
/// </summary>
/// <typeparam name="T">Body of the returned response</param>
public readonly struct Ok<T>(T value)
{
    public T Value { get; } = value;
}

/// <summary>
/// Ok result with "void" value (helpful for a no content response)
/// </summary>
[StructLayout(LayoutKind.Auto, Size = 1)]
public readonly struct Ok
{ }

/// <summary>
/// Error result
/// </summary>
public class ErrorResult
{
    /// <summary>
    /// Construct an error response
    /// </summary>
    /// <remarks>
    /// Leave the arguments as default since that will automatically capture the caller file path, calling member, and line number
    /// </remarks>
    public ErrorResult([CallerFilePath] string? filePath = null, [CallerMemberName] string? memberName = null, [CallerLineNumber] int lineNumber = 0)
    {
        CalledFrom = $"{filePath}:{memberName}, {lineNumber}";
    }

    internal ErrorResult(ErrorResult other, string? filePath, string? memberName, int lineNumber)
    {
        Message = other.Message;
        Exception = other.Exception;
        StatusCode = other.StatusCode;
        CalledFrom = $"{other.CalledFrom}\n\tcalled from --> {filePath}:{memberName}, {lineNumber}";
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
    /// Status code to return, if relevant
    /// </summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.BadRequest;
}
