using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using OneOf;
using OneOf.Types;

namespace UrlShortener.Backend;

/// <summary>
/// Server response that can be converted to <see cref="IActionResult"/>
/// </summary>
[GenerateOneOf]
public partial class ApiResult<T> : OneOfBase<Ok<T>, ErrorResponse, NotFound, Exception>, IConvertToActionResult
{
    /// <inheritdoc />
    public IActionResult Convert()
    {
        return Match<IActionResult>(
            static ok => new ObjectResult(ok) { StatusCode = (int)HttpStatusCode.OK },
            static error => new ObjectResult(error.Message) { StatusCode = (int)error.StatusCode },
            static notFound => new StatusCodeResult((int)HttpStatusCode.NotFound),
            static serverError => new ObjectResult("Server has experienced an oopsie. Contact Joardy McJoardyson if the issue persists.") { StatusCode = (int)HttpStatusCode.InternalServerError }
        );
    }

#pragma warning disable CA1000
    public static ApiResult<T> FromError<TOther>(ValueResult<TOther> other)
    {
        return other.Match<ApiResult<T>>(
            static ok => throw new InvalidOperationException($"Expected error result, not ok."),
            static ex => ex);
    }
#pragma warning restore CA1000
}
