using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Services;
using UrlShortener.Controllers;
using UrlShortener.Models;

namespace UrlShortener.Tests.Services;

#pragma warning disable NUnit2045

public sealed partial class UrlControllerTests : TestBase<UrlController>
{
    [ResetMe]
    private UrlInput? _urlInput;

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

    private void GivenUrlInput(UrlInput input) => _urlInput = input;

    private void GivenUrlServiceReturns(ValueResult<ShortenedUrlOuput> output)
    {
        UrlService.Setup(static u => u.Create(It.IsAny<UrlInput>(), It.IsAny<CancellationToken>())).ReturnsAsync(output);
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
    public void Create_Returns400Result_WhenInputIsNull()
    {
        GivenUrlInput(null!);
        ThenNoExceptions(WhenCreating);
        Assert.That(_actionResult is BadRequestObjectResult badRequest
            && (string)badRequest.Value! == "Expected body");
    }

    [Test]
    [TestOf(nameof(UrlController.Create))]
    [Category(UnitTest)]
    public void Create_ReturnsErrorPage_WhenInputIsInvalid()
    {
        string errMessage = "blarf";
        GivenUrlInput(new UrlInput { Url = "Asdf" });
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
        GivenUrlInput(new UrlInput { Url = "Asdf" });
        GivenUrlServiceReturns(new ErrorResult { Message = "blarf" });

        ThenNoExceptions(WhenCreating);
        Assert.That(_actionResult is ViewResult view
            && view.ViewName == "Error"
            && view.Model is ErrorViewModel errView
            && errView.Message == "The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists.");
    }

    [Test]
    [TestOf(nameof(UrlController.Create))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Create_Ok()
    {
        const string expectedAlias = "Pz8OFjIBP01oI2BBUwE_PzA~";
        const string url = "https://www.metal-archives.com/bands/Abigor/1066";
        GivenUrlInput(new UrlInput { Url = url });

        ThenNoExceptions(WhenCreating);
        ThenActionResultIs(out OkObjectResult ok);
        Assert.That(ok.Value, Is.TypeOf<ShortenedUrlOuput>());
        Assert.That(((ShortenedUrlOuput)ok.Value!).Alias, Is.EqualTo(expectedAlias));
        Assert.That(((ShortenedUrlOuput)ok.Value!).FullUrl, Is.EqualTo(url));
    }
    #endregion
}
