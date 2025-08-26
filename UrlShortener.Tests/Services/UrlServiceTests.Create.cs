using UrlShortener.Backend;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Services;

namespace UrlShortener.Tests.Services;

public sealed partial class UrlServiceTests
{
    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenInputHasInvalidUrl([Values(null, "", " ", "askfjasd")] string? url)
    {
        GivenUrlInput(url!);
        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<Err>(err => err.Message?.Equals($"Invalid URL '{url}'", StringComparison.Ordinal) == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenInputHasNonHttpUrl([Values("//asak/asfa", @"C:\Users\repos")] string url)
    {
        GivenUrlInput(url);
        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<Err>(err => err.Message?.Equals($"Submitted URL must use http(s) scheme. Found: '{url}'", StringComparison.Ordinal) == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenRepositoryErrorOnGettingByAlias([Values(true, false)] bool withException)
    {
        string url = "https://ziglang.org/documentation/master";
        GivenUrlInput(url);
        GivenRepositoryError(
            nameof(IShortenedUrlRepository.GetByAlias),
            "It failed!",
            withException ? new TaskCanceledException() : null);

        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<Err>(err =>
            err.Message?.Contains("It failed!", StringComparison.Ordinal) == true
            && withException ? err.Exception?.GetType() == typeof(TaskCanceledException) : err.Exception is null);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenRepositoryErrorOnInsert([Values(true, false)] bool withException)
    {
        string url = "https://ziglang.org/documentation/master";
        GivenUrlInput(url);
        GivenRepositoryError(
            nameof(IShortenedUrlRepository.Insert),
            "It failed!",
            withException ? new TaskCanceledException() : null);

        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<Err>(err =>
            err.Message?.Contains("It failed!", StringComparison.Ordinal) == true
            && withException ? err.Exception?.GetType() == typeof(TaskCanceledException) : err.Exception is null);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_When10thCollisionOccurs()
    {
        string url = "https://ziglang.org/documentation/master/";
        GivenUrlInput(url);
        GivenStoredUrls(Enumerable.Range(0, 10).Select(static x => new ShortenedUrl
        {
            RowId = x + 1,
            Alias = "blarf",
            FullUrl = $"https://mysite.com/{Guid.NewGuid()}",
            Offset = (short)x
        }));

        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<Err>(static err => err.Message?.StartsWith("Reached 10 collisions for alias", StringComparison.Ordinal) == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Create_ReturnsOk_WhenNewUrlSuccessfullyCreated()
    {
        string url = "https://ziglang.org/documentation/master/";
        const string expectedAlias = "Pw0_dBc_Pz9YPxtOPz8_NzA~";
        GivenUrlInput(url);

        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<ShortenedUrl>(static ok => ok.UrlSafeAlias.Equals(expectedAlias, StringComparison.Ordinal));
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [TestOf(nameof(UrlService.Query))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Create_ReturnsOk_WhenDuplicateUrlEntered()
    {
        string url = "https://ziglang.org/documentation/master/";
        const string expectedAlias = "Pw0_dBc_Pz9YPxtOPz8_NzA~";
        GivenUrlInput(url);

        ThenNoExceptions(WhenCreating);
        ThenNoExceptions(WhenCreating); // hit it again with same input
        // expecting same output as before
        ThenOutputResultIs<ShortenedUrl>(static ok => ok.UrlSafeAlias.Equals(expectedAlias, StringComparison.Ordinal));

        // shouldn't have more than 1 url
        Assert.That(ToTest.Query().Where(u => u.FullUrl.Equals(url, StringComparison.Ordinal)).Count(), Is.EqualTo(1));
    }
}
