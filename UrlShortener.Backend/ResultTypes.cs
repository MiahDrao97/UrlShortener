using System.Net;
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
/// Error response (presumably 400-level)
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Message to show the client
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Status code to return
    /// </summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;
}
