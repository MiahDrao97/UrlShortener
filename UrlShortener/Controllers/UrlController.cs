using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Services;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[Route("urls")]
public sealed class UrlController(
    IUrlService urlService,
    ILogger<UrlController> logger) : Controller
{
    private readonly IUrlService _urlService = urlService;
    private readonly ILogger<UrlController> _logger = logger;

    [EnableQuery(MaxTop = 100)]
    [HttpGet]
    [Route("all")]
    public IQueryable<ShortenedUrlOuput> Get([ODataQueryParameterBinding] ODataQueryOptions options)
    {
        return _urlService.Query(options);
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create([Required][FromBody] UrlInput input)
    {
        // kinda redundant since the framework should catch this, but...
        if (input is null)
        {
            _logger.LogError("Somehow the framework did not catch that there was no body sent to this endpoint.");
            return BadRequest("Expected body");
        }

        _logger.LogDebug("Creating alias for '{input}'", input.Url);
        ValueResult<ShortenedUrlOuput> result = await _urlService.Create(input);
        return result.Match<IActionResult>(
            static ok => new OkObjectResult(ok.Value), // TODO : this or a view?
            err =>
            {
                _logger.LogError(err.Exception, "{category} result from alias for url '{url}': {reason} --> {calledFrom}",
                    err.Category ?? "Error",
                    input.Url,
                    err.Message,
                    err.CalledFrom);
                return err.Category switch
                {
                    Constants.Errors.ClientError => View("Error", new ErrorViewModel
                    {
                        Message = err.Message!,
                    }),
                    // No category is our "catch-all" server error
                    _ => View("Error", new ErrorViewModel
                    {
                        Message = "The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists."
                    }),
                };
            });
    }
}
