using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UrlShortener.Backend;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Services;
using UrlShortener.Controllers;
using UrlShortener.Models;

namespace UrlShortener.Tests.Controllers;

#pragma warning disable NUnit2045

public sealed partial class UrlControllerTests : TestBase<UrlController>
{
    [ResetMe]
    private ShortenedUrlModel? _urlInput;

    [ResetMe]
    private IActionResult? _actionResult;

    private Mock<IUrlService> UrlService => GetMockOf<IUrlService>();

    protected override UrlController InitializeTestObject()
    {
        if (IntegrationMode)
        {
            // use live services
            return new UrlController(GetRegistered<IUrlService>(), Logger.Object);
        }
        return new UrlController(UrlService.Object, Logger.Object);
    }

    protected override void RegisterServices()
    {
        base.RegisterServices();

        Startup startup = new();
        startup.ConfigureServices(Services);
        startup.ConfigureDatabase(Services);

        AddMockOf<IUrlService>();
    }

    private void GivenUrlInput(string input) => _urlInput = new ShortenedUrlModel { UrlInput = input, Alias = null!, FullUrl = null! };

    private void GivenUrlServiceReturns(ValueResult<ShortenedUrl> output)
    {
        UrlService.Setup(static u => u.Create(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(output);
    }

    private async Task WhenCreating()
    {
        _actionResult = await ToTest.Create(_urlInput!);
    }

    private void ThenActionResultIs<T>([NotNull] out T result)
    {
        Assert.That(_actionResult, Is.TypeOf<T>());
        result = (T)_actionResult!;
    }

    #region Test Cases
    [Test]
    [TestOf(nameof(UrlController.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsErrorPage_WhenInputIsInvalid([Values(null, "", "  ", "asdf")] string? url)
    {
        string errMessage = "blarf";
        GivenUrlInput(url!);
        GivenUrlServiceReturns(new ErrorResult { Message = errMessage, Category = Constants.Errors.ClientError });

        ThenNoExceptions(WhenCreating);
        Assert.That(_actionResult is ViewResult view
            && view.ViewName == "Error"
            && view.Model is ErrorViewModel errView
            && errView.Message == errMessage);
    }

    [Test]
    [TestOf(nameof(UrlController.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsErrorPage_WhenUnexpectedErrorOccurs()
    {
        GivenUrlInput("Asdf");
        GivenUrlServiceReturns(new ErrorResult { Message = "blarf" });

        ThenNoExceptions(WhenCreating);
        Assert.That(_actionResult is ViewResult view
            && view.ViewName == "Error"
            && view.Model is ErrorViewModel errView
            && errView.Message == "The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists.");
    }

    [Test]
    [TestOf(nameof(UrlController.Create))]
    [TestOf(nameof(HomeController.Get))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Create_Ok()
    {
        const string expectedAlias = "Pw0_dBc_Pz9YPxtOPz8_NzA~";
        const string url = "https://ziglang.org/documentation/master/";
        GivenUrlInput(url);

        ThenNoExceptions(WhenCreating);
        ThenActionResultIs(out RedirectToActionResult redirect);
        Assert.That(redirect.RouteValues?["Alias"], Is.EqualTo(expectedAlias));
        Assert.That(redirect.RouteValues?["FullUrl"], Is.EqualTo(url));

        // now can we get it?
        HomeController home = new(GetRegistered<IUrlService>(), new Mock<ILogger<HomeController>>().Object);
        IActionResult? result = null;
        Assert.DoesNotThrowAsync(async () => result = await home.Get(expectedAlias));
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<RedirectResult>());
        Assert.That(((RedirectResult)result!).Url, Is.EqualTo(url));
    }
    #endregion
}
