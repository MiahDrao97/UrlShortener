using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Services;

namespace UrlShortener.Tests.Services;

public sealed partial class UrlServiceTests
{
    [Test]
    [TestOf(nameof(UrlService.RecordHit))]
    [Category(UnitTest)]
    public void RecordHit_ReturnsError_WhenUrlTelemetryIsNull()
    {
        GivenUrlTelemetry(null!);
        ThenNoExceptions(WhenRecordingHit);
        ThenRecordHitResultIs<ErrorResult>(static err => err.Message?.Equals("telemetry cannot be null", StringComparison.Ordinal) == true);
    }

    [Test]
    [TestOf(nameof(UrlService.RecordHit))]
    [Category(UnitTest)]
    public void RecordHit_ReturnsError_WhenShortenedUrlNotFound()
    {
        GivenUrlTelemetry(new UrlTelemetry { RowId = 2, DateHit = DateTime.UtcNow });
        // already no urls by default

        ThenNoExceptions(WhenRecordingHit);
        ThenRecordHitResultIs<ErrorResult>(static err => err.Message?.Equals("Row with row id 2 was not found.", StringComparison.Ordinal) == true
            && err.Category == Constants.Errors.NotFound);
    }

    [Test]
    [TestOf(nameof(UrlService.RecordHit))]
    [Category(UnitTest)]
    public void RecordHit_ReturnsError_WhenRepositoryReturnsError([Values(true, false)] bool withException)
    {
        GivenUrlTelemetry(new UrlTelemetry { RowId = 2, DateHit = DateTime.UtcNow });
        GivenStoredUrls([
            new ShortenedUrl
            {
                RowId = 1,
                Alias = "asdf",
                FullUrl = $"https://mysite.com/{Guid.NewGuid()}"
            },
            new ShortenedUrl
            {
                RowId = 2,
                Alias = "hjkg",
                FullUrl = $"https://mysite.com/{Guid.NewGuid()}"
            },
        ]);
        GivenRepositoryError(
            nameof(IShortenedUrlRepository.Update),
            "It failed!",
            withException ? new TaskCanceledException() : null);

        ThenNoExceptions(WhenRecordingHit);
        ThenRecordHitResultIs<ErrorResult>(err => err.Message?.Equals("It failed!", StringComparison.Ordinal) == true
            && withException ? err.Exception?.GetType() == typeof(TaskCanceledException) : err.Exception is null);
    }

    [Test]
    [TestOf(nameof(UrlService.RecordHit))]
    [Category(UnitTest)]
    public void RecordHit_ReturnsOk_WhenRepositorySuccessfullyUpdates()
    {
        int rowId = 2;
        DateTime now = DateTime.UtcNow;
        GivenUrlTelemetry(new UrlTelemetry { RowId = rowId, DateHit = now });
        GivenStoredUrls([
            new ShortenedUrl
            {
                RowId = 1,
                Alias = "asdf",
                FullUrl = $"https://mysite.com/{Guid.NewGuid()}"
            },
            new ShortenedUrl
            {
                RowId = 2,
                Alias = "hjkg",
                FullUrl = $"https://mysite.com/{Guid.NewGuid()}"
            },
        ]);

        ThenNoExceptions(WhenRecordingHit);
        ThenRecordHitResultIs<Ok>();
        Repo.Verify(r => r.Update(It.Is<ShortenedUrl>(s => s.RowId == rowId && s.Hits == 1 && s.LastHit == now), It.IsAny<CancellationToken>()), Times.Once);
    }
}
