using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Backend.Services;

namespace UrlShortener.Models;

public sealed class AllUrlsModel(IUrlService urlService) : PageModel
{
    private readonly IUrlService _urlService = urlService;

    // TODO : configure this as app setting
    private const int _pageSize = 20;

    public string? FullUrlSort { get; set; }
    public string? FullAliasUrlSort { get; set; }
    public string? CreatedSort { get; set; }
    public string? Filter { get; set; }

    public IList<ShortenedUrlModel> ShortenedUrls { get; private set; } = [];

    public async Task OnGetAsync(string? sortOrder, string? searchString, int? page)
    {
        switch (sortOrder)
        {
            default:
                break;
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            ShortenedUrls = await _urlService
                .Query()
                .Where(x => x.FullUrl.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .Skip(page ?? 0)
                .Take(_pageSize)
                .Select(x => new ShortenedUrlModel
                {
                    FullUrl = x.FullUrl,
                    Alias = x.UrlSafeAlias,
                    HostName = HttpContext.Request.Host.ToString()
                })
                .ToListAsync();
        }

        ShortenedUrls = await _urlService
            .Query()
            .Skip(page ?? 0)
            .Take(_pageSize)
            .Select(x => new ShortenedUrlModel
            {
                FullUrl = x.FullUrl,
                Alias = x.UrlSafeAlias,
                HostName = HttpContext.Request.Host.ToString()
            })
            .ToListAsync();
    }
}
