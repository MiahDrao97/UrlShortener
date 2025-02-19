using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    private const int _pageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? searchFilter, string? sortColumn, int? page)
    {
        try
        {
            IQueryable<ShortenedUrl> query = _urlService.Query();
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                query = query.Where(x => x.FullUrl.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));
            }
            // start the count query
            Task<int> countTask = query.CountAsync();

            // ordering
            query = sortColumn switch
            {
                "url_asc" => query.OrderBy(s => s.FullUrl),
                "url_desc" => query.OrderByDescending(s => s.FullUrl),
                "alias_asc" => query.OrderBy(s => s.UrlSafeAlias),
                "alias_desc" => query.OrderByDescending(s => s.UrlSafeAlias),
                "date_asc" => query.OrderBy(s => s.Created),
                "date_desc" => query.OrderByDescending(s => s.Created),
                "hits_asc" => query.OrderBy(s => s.Hits),
                "hits_desc" => query.OrderByDescending(s => s.Hits),
                "last_hit_asc" => query.OrderBy(s => s.LastHit),
                "last_hit_desc" => query.OrderByDescending(s => s.LastHit),
                _ => query,
            };

            // paging
            query = query.Skip(_pageSize * (page ?? 0)).Take(_pageSize);

            IEnumerable<ShortenedUrlModel> urls = (await query.ToListAsync()) // execute query before projecting to our model type
                .Select(x => new ShortenedUrlModel
                {
                    FullUrl = x.FullUrl,
                    Alias = x.UrlSafeAlias,
                    Created = x.Created.ToLocalTime(),
                    Hits = x.Hits,
                    LastHit = x.LastHit?.ToLocalTime(),
                    HostName = HttpContext.Request.Host.ToString()
                });

            return View(new UrlPaginatedListModel
            {
                ShortenedUrls = [.. urls],
                SortColumn = sortColumn,
                SearchFilter = searchFilter,
                TotalCount = await countTask,
                PageSize = _pageSize,
                PageIndex = page ?? 0,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch URLs");
            return View("Error", new ErrorViewModel
            {
                Message = "The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists."
            });
        }
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
                Created = ok.Value.Created.ToLocalTime(),
                Hits = ok.Value.Hits,
                LastHit = ok.Value.LastHit?.ToLocalTime(),
                HostName = HttpContext.Request.Host.ToString()
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
