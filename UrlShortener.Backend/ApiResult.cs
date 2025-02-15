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
public partial class ApiResult : OneOfBase<NoContent, ErrorResponse, NotFound, Exception>, IConvertToActionResult
{
    /// <inheritdoc />
    public IActionResult Convert()
    {
        return Match<IActionResult>(
            static noContent => new StatusCodeResult((int)HttpStatusCode.NoContent),
            static error => new ObjectResult(error.Message) { StatusCode = (int)error.StatusCode },
            static notFound => new StatusCodeResult((int)HttpStatusCode.NotFound),
            static serverError => new ObjectResult("Server has experienced an oopsie. Contact Joardy McJoardyson if the issue persists.") { StatusCode = (int)HttpStatusCode.InternalServerError }
        );
    }
}
