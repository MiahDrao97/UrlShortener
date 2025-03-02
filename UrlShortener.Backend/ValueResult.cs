using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace UrlShortener.Backend;

/// <summary>
/// Server response that can be converted to <see cref="IActionResult"/>
/// </summary>
[GenerateOneOf]
public partial class ValueResult<T> : OneOfBase<Ok<T>, ErrorResult>
{
    /// <summary>
    /// Exception
    /// </summary>
    public ErrorResult? Error => Match<ErrorResult?>(static ok => null, static ex => ex);

    /// <summary>
    /// Determine if this result was a success or not
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess([MaybeNullWhen(false)] out T ok)
    {
        T okResult = default!;
        bool success = Match(
            ok =>
            {
                okResult = ok.Value;
                return true;
            },
            static ex => false
        );
        ok = okResult;
        return success;
    }

#pragma warning disable CA1000
    /// <summary>
    /// Transform an error value result of one type to another
    /// </summary>
    /// <remarks>
    /// Adds to the pseudo-stack trace, so please keep the default parameters default so that stack info is not obscured
    /// </remarks>
    public static ValueResult<T> FromError<TOther>(
        ValueResult<TOther> other,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        return other.Match<ValueResult<T>>(
            static ok => throw new InvalidOperationException($"Expected error result, not ok."),
            err => new ErrorResult(err, filePath, memberName, lineNumber)); // captures stack trace
    }
#pragma warning restore CA1000
}
