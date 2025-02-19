namespace UrlShortener.Models;

public sealed class UrlPaginatedListModel
{
    /// <summary>
    /// Which column to sort by, if any
    /// </summary>
    public string? SortColumn { get; set; }

    /// <summary>
    /// Search filter, if any
    /// </summary>
    public string? SearchFilter { get; set; }

    /// <summary>
    /// All the results
    /// </summary>
    public required IReadOnlyList<ShortenedUrlModel> ShortenedUrls { get; set; }

    /// <summary>
    /// How many items are on each page
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// Which page we're on (0-based)
    /// </summary>
    public required int PageIndex { get; set; }

    /// <summary>
    /// Total result count
    /// </summary>
    public required int TotalCount { get; set; }

    /// <summary>
    /// If true, more results await on the next page
    /// </summary>
    public bool HasNext => TotalCount > ShortenedUrls.Count && ShortenedUrls.Count != 0;

    /// <summary>
    /// If true, more results await on the previous page
    /// </summary>
    public bool HasPrev => PageIndex > 0;
}
