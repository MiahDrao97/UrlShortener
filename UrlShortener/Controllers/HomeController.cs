using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Backend;
using UrlShortener.Backend.Services;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

public class HomeController(
    IUrlService urlService,
    ILogger<HomeController> logger) : Controller
{
    private readonly IUrlService _urlService = urlService;
    private readonly ILogger<HomeController> _logger = logger;

    public IActionResult Index(ShortenedUrlModel? model)
    {
        return View("Index", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier, Message = string.Empty });
    }

    [HttpGet]
    [Route("/{alias}")]
    public async Task<IActionResult> Get([FromRoute] string @alias)
    {
        Attempt<string> result = await _urlService.Lookup(@alias);
        if (result.IsSuccess(out string? redirectUrl))
        {
            return new RedirectResult(redirectUrl);
        }

        _logger.LogError(result.Err.Exception, "{category} result from looking up stored url for alias '{alias}': {reason} --> {calledFrom}",
            result.Err.Code,
            @alias,
            result.Err.Message,
            result.Err.CalledFrom);
        return result.Err.Code switch
        {
            Constants.Errors.NotFound => View("UrlNotFound", new UrlNotFoundModel
            {
                Alias = @alias,
                HostName = HttpContext?.Request?.Host.ToString()
            }),
            // No category is our "catch-all" server error
            _ => View("Error", new ErrorViewModel
            {
                Message = "The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists."
            }),
        };
    }
}
