using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Backend;
using UrlShortener.Backend.Data.Entities;
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

    [HttpGet]
    [Route("all")]
    public IActionResult All()
    {
        return View(new AllUrlsModel(_urlService));
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create([Required] ShortenedUrlModel url)
    {
        // a bit redundant, but...
        if (url is null)
        {
            _logger.LogError("Somehow the framework did not catch that url was null.");
            return BadRequest("Expected url input");
        }

        _logger.LogDebug("Creating alias for '{input}'", url.UrlInput);
        // let the service validate it
        ValueResult<ShortenedUrl> result = await _urlService.Create(url.UrlInput!);
        return result.Match<IActionResult>(
            ok => new RedirectToActionResult("Index", "Home", new ShortenedUrlModel
            {
                FullUrl = ok.Value.FullUrl,
                Alias = ok.Value.UrlSafeAlias,
                HostName = HttpContext?.Request?.Host.ToString()
            }),
            err =>
            {
                _logger.LogError(err.Exception, "{category} result from alias for url '{url}': {reason} --> {calledFrom}",
                    err.Category ?? "Error",
                    url.UrlInput,
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
