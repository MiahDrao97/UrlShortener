using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace UrlShortener.Backend;

/// <summary>
/// Server response that can be converted to <see cref="IActionResult"/>
/// </summary>
[GenerateOneOf]
public partial class ValueResult<T> : OneOfBase<Ok<T>, Exception>
{
    /// <summary>
    /// Exception
    /// </summary>
    public Exception? Exception => Match<Exception?>(static ok => null, static ex => ex);

    /// <summary>
    /// Determine if this result was a success or not
    /// </summary>
    [MemberNotNullWhen(false, nameof(Exception))]
    public bool IsSuccess([MaybeNullWhen(false)] out T ok)
    {
        T? okResult = default;
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
    public static ValueResult<T> FromError<TOther>(ValueResult<TOther> other)
    {
        return other.Match<ValueResult<T>>(
            static ok => throw new InvalidOperationException($"Expected error result, not ok."),
            static ex => ex);
    }
#pragma warning restore CA1000
}
