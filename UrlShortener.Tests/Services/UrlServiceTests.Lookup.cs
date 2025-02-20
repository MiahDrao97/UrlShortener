using System.Text;
using System.Threading.Channels;
using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Services;

namespace UrlShortener.Tests.Services;

public sealed partial class UrlServiceTests
{
    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(UnitTest)]
    public void Lookup_ReturnsError_OnNullOrWhitespaceInput([Values(null, "", "  ")] string? input)
    {
        GivenAlias(input!);
        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(err => err.Message?.Equals($"Shortened url cannot be null or whitespace. Was: '{input ?? "<null>"}'", StringComparison.Ordinal) == true);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Lookup_ReturnsError_WhenUnableToDecodeBase64()
    {
        string input = "as&10!*0";
        GivenAlias(input);

        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(err => err.Message?.Equals($"Alias '{input}' is not base64-encoded.", StringComparison.Ordinal) == true
            && err.Category == Constants.Errors.NotFound);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Lookup_ReturnsError_WhenAliasIsNot17Characters()
    {
        string b64Input = Convert.ToBase64String(Encoding.ASCII.GetBytes("asdf"));
        GivenAlias(b64Input);

        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(static err => err.Message?.Contains($"is not at least 17 bytes (ASCII characters).", StringComparison.Ordinal) == true
            && err.Category == Constants.Errors.NotFound);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(IntegrationTest)]
    [IntegrationMode]
    public void Lookup_ReturnsError_WhenAliasLastByteIsNotDigit()
    {
        string input = "abcdefghijklmnopq";
        string b64Input = Convert.ToBase64String(Encoding.ASCII.GetBytes(input));
        GivenAlias(b64Input);

        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(err => err.Message?.Contains($"did not parse to a valid offset for alias '{b64Input}' (decoded: {input}).", StringComparison.Ordinal) == true
            && err.Category == Constants.Errors.NotFound);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(UnitTest)]
    public void Lookup_ReturnsError_WhenRepositoryReturnsError([Values(true, false)] bool withException)
    {
        GivenAlias("asdf");
        // assuming that url transformer works just fine (even tho this would normally be bad input)
        GivenRepositoryError(
            nameof(IShortenedUrlRepository.GetByAlias),
            "It failed!",
            withException ? new TaskCanceledException() : null);

        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(err => err.Message?.Equals("It failed!", StringComparison.Ordinal) == true
            && withException ? err.Exception?.GetType() == typeof(TaskCanceledException) : err.Exception is null);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(UnitTest)]
    public void Lookup_ReturnsError_WhenRepositoryReturnsEmptyForGivenAlias()
    {
        GivenAlias("asdf");
        // assuming that url transformer works just fine (even tho this would normally be bad input)

        // already empty repository by default
        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(static err => err.Message?.Equals($"No urls found with alias 'asdf'", StringComparison.Ordinal) == true
            && err.Category == Constants.Errors.NotFound);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(UnitTest)]
    public void Lookup_ReturnsError_WhenRepositoryReturnsResultsButNoMatchingOffset()
    {
        GivenAlias("asdf");
        // assuming that url transformer works just fine (even tho this would normally be bad input)
        GivenStoredUrls([
            new ShortenedUrl
            {
                RowId = 1,
                Alias = "blarf",
                Offset = 0
            }
        ]);
        GivenDecodedAlias("blarf", 1);

        // already empty repository by default
        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<ErrorResult>(static err => err.Message?.Equals($"No urls found with alias 'asdf'", StringComparison.Ordinal) == true
            && err.Category == Constants.Errors.NotFound);
    }

    [Test]
    [TestOf(nameof(UrlService.Lookup))]
    [Category(UnitTest)]
    public void Lookup_ReturnsOk_WhenRepositoryReturnsResultsWithMatchingOffset()
    {
        string url = $"https://mysite.com/{Guid.NewGuid()}";
        GivenAlias("asdf");
        // assuming that url transformer works just fine (even tho this would normally be bad input)
        GivenStoredUrls([
            new ShortenedUrl
            {
                RowId = 1,
                Alias = "blarf",
                Offset = 0,
                FullUrl = url,
            }
        ]);
        GivenDecodedAlias("blarf", 0);

        // already empty repository by default
        ThenNoExceptions(WhenLookingUp);
        ThenLookupResultIs<Ok<string>>(ok => ok.Value.Equals(url, StringComparison.Ordinal));
        // make sure we emit telemetry event to channel
        Assert.That(GetRegistered<Channel<UrlTelemetry>>().Reader.Count, Is.EqualTo(1));
    }
}
