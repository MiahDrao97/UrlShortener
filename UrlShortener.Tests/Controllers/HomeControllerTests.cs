using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Backend;
using UrlShortener.Backend.Services;
using UrlShortener.Controllers;
using UrlShortener.Models;

namespace UrlShortener.Tests.Controllers;

/// <summary>
/// Unit-testing <see cref="HomeController"/>
/// </summary>
/// <remarks>
/// Integration tests are done on <see cref="UrlControllerTests"/>, which includes a GET call on <see cref="HomeController"/>
/// </remarks>
[TestFixture]
[TestOf(nameof(HomeController))]
public sealed partial class HomeControllerTests : TestBase<HomeController>
{
    [ResetMe]
    IActionResult? _actionResult;

    [ResetMe]
    string? _alias;

    private Mock<IUrlService> UrlService => GetMockOf<IUrlService>();

    protected override HomeController InitializeTestObject()
    {
        if (IntegrationMode)
        {
            // use live services
            return new HomeController(GetRegistered<IUrlService>(), Logger.Object);
        }
        return new HomeController(UrlService.Object, Logger.Object);
    }

    protected override void RegisterServices()
    {
        base.RegisterServices();

        Startup startup = new();
        startup.ConfigureServices(Services);
        startup.ConfigureDatabase(Services);

        AddMockOf<IUrlService>();
    }

    private void GivenAlias(string @alias) => _alias = @alias;

    private void GivenUrlServiceReturns(ValueResult<string> result)
    {
        UrlService.Setup(u => u.Lookup(_alias!, It.IsAny<CancellationToken>())).ReturnsAsync(result);
    }

    private async Task WhenHittingAlias()
    {
        _actionResult = await ToTest.Get(_alias!);
    }

    private void ThenActionResultIs<T>([NotNull] out T result)
    {
        Assert.That(_actionResult, Is.TypeOf<T>());
        result = (T)_actionResult!;
    }

    #region Test Cases
    [Test]
    [TestOf(nameof(HomeController.Get))]
    [Category(UnitTest)]
    public void Get_ReturnsErrorView_WhenUrlServiceReturnsError(
        [Values(Constants.Errors.NotFound, Constants.Errors.ClientError, null)] string? errorCategory)
    {
        GivenAlias("asdf");
        GivenUrlServiceReturns(new ErrorResult { Message = "It failed!", Category = errorCategory });

        ThenNoExceptions(WhenHittingAlias);
        ThenActionResultIs<ViewResult>(out ViewResult? view);
        if (errorCategory == Constants.Errors.NotFound)
        {
            Assert.That(view.Model, Is.TypeOf<UrlNotFoundModel>());
            Assert.That(((UrlNotFoundModel)view.Model!).Alias, Is.EqualTo("asdf"));
        }
        else
        {
            Assert.That(view.Model, Is.TypeOf<ErrorViewModel>());
            Assert.That(((ErrorViewModel)view.Model!).Message, Is.EqualTo("The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists."));
        }
    }

    [Test]
    [TestOf(nameof(HomeController.Get))]
    [Category(UnitTest)]
    public void Get_ReturnsRedirectResult_WhenUrlServiceReturnsUrl()
    {
        string url = $"https://mysite.com/{Guid.NewGuid()}";
        GivenAlias("asdf");
        GivenUrlServiceReturns(new Ok<string>(url));

        ThenNoExceptions(WhenHittingAlias);
        ThenActionResultIs<RedirectResult>(out RedirectResult? redirect);
        Assert.That(redirect.Url, Is.EqualTo(url));
    }
    #endregion
}
