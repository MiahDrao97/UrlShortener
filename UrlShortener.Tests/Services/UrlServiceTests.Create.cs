using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Services;

namespace UrlShortener.Tests.Services;

public sealed partial class UrlServiceTests
{
    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenInputIsNull()
    {
        GivenUrlInput(null!);
        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<ErrorResult>(err => err.Message?.Equals("input cannot be null") == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenInputHasInvalidUrl([Values(null, "", " ", "askfjasd")] string? url)
    {
        GivenUrlInput(new UrlInput { Url = url! });
        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<ErrorResult>(err => err.Message?.Equals($"Invalid URL '{url}'") == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenInputHasNonHttpUrl([Values("//asak/asfa", @"C:\Users\repos")] string url)
    {
        GivenUrlInput(new UrlInput { Url = url });
        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<ErrorResult>(err => err.Message?.Equals($"Submitted URL must use http(s) scheme. Found: '{url}'") == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsError_WhenRepositoryThrowsOnGettingByAlias()
    {
        string url = "https://ziglang.org/documentation/master";
        GivenUrlInput(new UrlInput { Url = url });
        GivenRepositoryThrows(nameof(IShortenedUrlRepository.GetByAlias), new TaskCanceledException());

        ThenNoExceptions(WhenCreating);
        ThenOutputResultIs<ErrorResult>(err =>
            err.Message?.Equals($"Uncaught exception while creating shortened url for '{url}'") == true
            && err.Exception?.GetType() == typeof(TaskCanceledException));
    }
}
