using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using OneOf;

namespace UrlShortener.Backend;

/// <summary>
/// Result type that's either <see cref="Ok"/> or an exception
/// </summary>
[GenerateOneOf]
public partial class Result : OneOfBase<Ok, ErrorResult>
{
    /// <summary>
    /// Error
    /// </summary>
    public ErrorResult? Error => Match<ErrorResult?>(static ok => null, static err => err);

    /// <summary>
    /// True if this was a succcessful result; otherwise error
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Success => Match(static ok => true, static err => false);

    public static Result From<T>(
        ValueResult<T> other,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        return other.Match<Result>(
            static ok => new Ok(),
            err => new ErrorResult(err, filePath, memberName, lineNumber));
    }
}
