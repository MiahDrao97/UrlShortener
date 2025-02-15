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
public partial class PayloadResult<T> : OneOfBase<Ok<T>, ErrorResponse, NotFound, Exception>, IConvertToActionResult
{
    /// <inheritdoc />
    public IActionResult Convert()
    {
        return Match<IActionResult>(
            static ok => new ObjectResult(ok.Value) { StatusCode = (int)HttpStatusCode.OK },
            static error => new ObjectResult(error.Message) { StatusCode = (int)error.StatusCode },
            static notFound => new StatusCodeResult((int)HttpStatusCode.NotFound),
            static serverError => new ObjectResult("Server has experienced an oopsie. Contact Joardy McJoardyson if the issue persists.") { StatusCode = (int)HttpStatusCode.InternalServerError }
        );
    }
}
