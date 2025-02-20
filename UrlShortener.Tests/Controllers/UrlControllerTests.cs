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
    private string? _searchFilter;

    [ResetMe]
    private string? _sortColumn;

    [ResetMe]
    private int? _page;

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

    private void GivenSearchFilter(string? filter) => _searchFilter = filter;

    private void GivenSortColumn(string? order) => _sortColumn = order;

    private void GivenPage(int? page) => _page = page;

    private void GivenQueryThrows(Exception ex)
    {
        UrlService.Setup(static u => u.Query()).Throws(ex);
    }

    private async Task WhenCreating()
    {
        _actionResult = await ToTest.Create(_urlInput!);
    }

    private async Task WhenHittingIndex()
    {
        _actionResult = await ToTest.Index(_searchFilter, _sortColumn, _page);
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

    [Test]
    [TestOf(nameof(UrlController.Index))]
    [Category(UnitTest)]
    public void Index_ReturnsErrorView_WhenUrlServiceThrows([Values(null, "", "  ", "asdf")] string? filter)
    {
        GivenSearchFilter(filter);
        GivenQueryThrows(new InvalidOperationException("It failed!"));

        ThenNoExceptions(WhenHittingIndex);
        ThenActionResultIs<ViewResult>(out ViewResult? view);
        Assert.That(view.Model, Is.Not.Null);
        Assert.That(view.Model, Is.TypeOf<ErrorViewModel>());
        Assert.That(((ErrorViewModel)view.Model!).Message, Is.EqualTo("The server has experienced an oopsie. Please contact Joardy McJoardyson at (248) 434-5508 if the issue persists."));
    }

    [Test]
    [TestOf(nameof(UrlController.Create))]
    [TestOf(nameof(UrlController.Index))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Index_Ok()
    {
        // 12 urls
        string[] urls = [
            "https://ziglang.org/documentation/master/",
            "https://www.thesaurus.com/browse/scurry",
            "https://vim.rtorr.com/",
            "https://www.esv.org/Isaiah+49/",
            "https://wezterm.org/features.html",
            "https://www.openmymind.net/Switching-On-Strings-In-Zig/",
            "https://ziggit.dev/t/explanation-of-std-builtin-atomicorder-enumerations/5897",
            "https://doc.rust-lang.org/book/ch12-05-working-with-environment-variables.html",
            "https://www.metal-archives.com/bands/Abigor/1066",
            "https://sovereigngracemusic.com/music/songs/our-song-from-age-to-age/",
            "https://github.com/MiahDrao97/iter_z/tree/main",
            "https://github.com/ziglang/zig/pull/22605",
        ];
        // insert data
        foreach (string url in urls)
        {
            GivenUrlInput(url);
            ThenNoExceptions(WhenCreating);
        }

        // no query params
        ThenNoExceptions(WhenHittingIndex);
        ThenActionResultIs<ViewResult>(out ViewResult? view);
        Assert.That(view.Model, Is.Not.Null);
        Assert.That(view.Model, Is.TypeOf<UrlPaginatedListModel>());
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls, Has.Count.EqualTo(10));

        // most recent on top, get 2nd page
        GivenSortColumn("date_asc");
        GivenPage(1);
        ThenNoExceptions(WhenHittingIndex);
        ThenActionResultIs<ViewResult>(out view);
        Assert.That(view.Model, Is.Not.Null);
        Assert.That(view.Model, Is.TypeOf<UrlPaginatedListModel>());
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls, Has.Count.EqualTo(2)); // expecting only 2 on the second page
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls[0].FullUrl, Is.EqualTo(urls[^2])); // second to last

        GivenSearchFilter("zig");
        GivenSortColumn("date_desc");
        GivenPage(null);
        ThenNoExceptions(WhenHittingIndex);
        ThenActionResultIs<ViewResult>(out view);
        Assert.That(view.Model, Is.Not.Null);
        Assert.That(view.Model, Is.TypeOf<UrlPaginatedListModel>());
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls, Has.Count.EqualTo(4));
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls[0].FullUrl, Is.EqualTo(urls[^1]));

        GivenSearchFilter(null);
        GivenSortColumn(null);
        GivenPage(10);
        ThenNoExceptions(WhenHittingIndex);
        ThenActionResultIs<ViewResult>(out view);
        Assert.That(view.Model, Is.Not.Null);
        Assert.That(view.Model, Is.TypeOf<UrlPaginatedListModel>());
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls, Has.Count.EqualTo(0));

        GivenSearchFilter(null);
        GivenSortColumn(null);
        GivenPage(-1); // negative page translates to 0
        ThenNoExceptions(WhenHittingIndex);
        ThenActionResultIs<ViewResult>(out view);
        Assert.That(view.Model, Is.Not.Null);
        Assert.That(view.Model, Is.TypeOf<UrlPaginatedListModel>());
        Assert.That(((UrlPaginatedListModel)view.Model!).ShortenedUrls, Has.Count.EqualTo(10));
    }
    #endregion
}
