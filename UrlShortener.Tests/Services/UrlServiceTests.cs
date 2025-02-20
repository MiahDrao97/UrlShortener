using System.Threading.Channels;
using MockQueryable;
using UrlShortener.Backend;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Data.Repositories;
using UrlShortener.Backend.Services;

namespace UrlShortener.Tests.Services;

// assert multiple doesn't report what went wrong as nicely
#pragma warning disable NUnit2045

[TestFixture]
[TestOf(nameof(UrlService))]
public sealed partial class UrlServiceTests : TestBase<UrlService>
{
    #region Inputs and Results
    [ResetMe]
    private string? _urlInput;

    [ResetMe]
    private string? _aliasLookup;

    [ResetMe]
    private UrlTelemetry? _urlTelemetry;

    [ClearValues]
    private readonly List<ShortenedUrl> _storedUrls = [];

    [ResetMe]
    private ValueResult<ShortenedUrl>? _outputResult;

    [ResetMe]
    private ValueResult<string>? _lookupResult;

    [ResetMe]
    private Result? _recordHitResult;
    #endregion

    #region Mocks
    private Mock<IUrlTransformer> Transformer => GetMockOf<IUrlTransformer>();
    private Mock<IShortenedUrlRepository> Repo => GetMockOf<IShortenedUrlRepository>();
    #endregion

    #region Overrides
    protected override UrlService InitializeTestObject()
    {
        if (IntegrationMode)
        {
            return new UrlService(
                GetRegistered<IUrlTransformer>(),
                GetRegistered<IShortenedUrlRepository>(),
                GetRegistered<Channel<UrlTelemetry>>(),
                Logger.Object);
        }
        return new UrlService(
            Transformer.Object,
            Repo.Object,
            GetRegistered<Channel<UrlTelemetry>>(),
            Logger.Object
        );
    }

    protected override void RegisterServices()
    {
        base.RegisterServices();

        AddMockOf<IUrlTransformer>();
        AddMockOf<IShortenedUrlRepository>();
        AddMockOf<Channel<UrlTelemetry>>();
        AddMockOf<ChannelWriter<UrlTelemetry>>();

        Startup startup = new();
        startup.ConfigureServices(Services);
        startup.ConfigureDatabase(Services);
    }

    protected override void InitialSetups()
    {
        base.InitialSetups();

        Transformer.Setup(static t => t.CreateAlias(It.IsAny<string>())).Returns(new Ok<string>("blarf"));
        Transformer.Setup(static t => t.FromUrlSafeAlias(It.IsAny<string>())).Returns(new Ok<(string, short)>(("blarf", 0)));

        Repo.Setup(static r => r.GetByAlias(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync<string, CancellationToken, IShortenedUrlRepository, ValueResult<ShortenedUrl[]>>((@alias, _) =>
            {
                return new Ok<ShortenedUrl[]>([.. _storedUrls.Where(s => s.Alias == @alias)]);
            });
        Repo.Setup(static r => r.Insert(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortenedUrl, CancellationToken>((url, _) => _storedUrls.Add(url))
            .ReturnsAsync<ShortenedUrl, CancellationToken, IShortenedUrlRepository, ValueResult<ShortenedUrl>>(static (url, _) => new Ok<ShortenedUrl>(url));
        Repo.Setup(static r => r.Update(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
            .Callback<ShortenedUrl, CancellationToken>((url, _) =>
            {
                if (_storedUrls.FirstOrDefault(s => s.Alias == url.Alias && s.Offset == url.Offset) is ShortenedUrl found)
                {
                    _storedUrls.Remove(found);
                    _storedUrls.Add(url);
                }
            })
            .ReturnsAsync(new Ok());
        Repo.Setup(static r => r.Query()).Returns(_storedUrls.BuildMock());
    }
    #endregion

    #region Given
    private void GivenUrlInput(string input) => _urlInput = input;

    private void GivenAlias(string @alias) => _aliasLookup = @alias;

    private void GivenRepositoryError(string method, string? message = null, Exception? ex = null)
    {
        switch (method)
        {
            case nameof(IShortenedUrlRepository.GetByAlias):
                Repo.Setup(static r => r.GetByAlias(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ErrorResult { Message = message, Exception = ex });
                break;
            case nameof(IShortenedUrlRepository.Insert):
                Repo.Setup(static r => r.Insert(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ErrorResult { Message = message, Exception = ex });
                break;
            case nameof(IShortenedUrlRepository.Update):
                Repo.Setup(static r => r.Update(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ErrorResult { Message = message, Exception = ex });
                break;
            default:
                throw new NotSupportedException($"No method on {typeof(IShortenedUrlRepository)} called '{method}'");
        }
    }

    private void GivenStoredUrls(IEnumerable<ShortenedUrl> urls)
    {
        _storedUrls.AddRange(urls);
    }

    private void GivenDecodedAlias(string decoded, short offset)
    {
        Transformer.Setup(static t => t.FromUrlSafeAlias(It.IsAny<string>())).Returns(new Ok<(string, short)>((decoded, offset)));
    }

    private void GivenUrlTelemetry(UrlTelemetry telem) => _urlTelemetry = telem;
    #endregion

    #region When
    private async Task WhenCreating()
    {
        _outputResult = await ToTest.Create(_urlInput!, default);
    }

    private async Task WhenLookingUp()
    {
        _lookupResult = await ToTest.Lookup(_aliasLookup!, default);
    }

    private async Task WhenRecordingHit()
    {
        _recordHitResult = await ToTest.RecordHit(_urlTelemetry!);
    }
    #endregion

    #region Then
    private void ThenOutputResultIs<T>(Func<T, bool>? assert = null)
    {
        assert ??= static (_) => true;

        try
        {
            Assert.That(_outputResult, Is.Not.Null);
            Assert.That(_outputResult!.Value, Is.TypeOf<T>());
            Assert.That(assert((T)_outputResult.Value), Is.True);
        }
        catch (AssertionException)
        {
            Console.WriteLine($"Expected that result was type '{typeof(T)}' but found: '{_outputResult?.Value.GetType()}'");
            if (_outputResult?.Value is ErrorResult err)
            {
                Console.WriteLine($"Error result: {err.Message} --> {err.CalledFrom}\nException: {err.Exception}");
            }
            throw;
        }
    }

    private void ThenLookupResultIs<T>(Func<T, bool>? assert = null)
    {
        assert ??= static (_) => true;

        try
        {
            Assert.That(_lookupResult, Is.Not.Null);
            Assert.That(_lookupResult!.Value, Is.TypeOf<T>());
            Assert.That(assert((T)_lookupResult.Value), Is.True);
        }
        catch (AssertionException)
        {
            Console.WriteLine($"Expected that result was type '{typeof(T)}' but found: '{_lookupResult?.Value.GetType()}'");
            if (_lookupResult?.Value is ErrorResult err)
            {
                Console.WriteLine($"Error result: {err.Message} --> {err.CalledFrom}\nException: {err.Exception}");
            }
            throw;
        }
    }

    private void ThenRecordHitResultIs<T>(Func<T, bool>? assert = null)
    {
        assert ??= static (_) => true;

        try
        {
            Assert.That(_recordHitResult, Is.Not.Null);
            Assert.That(_recordHitResult!.Value, Is.TypeOf<T>());
            Assert.That(assert((T)_recordHitResult.Value), Is.True);
        }
        catch (AssertionException)
        {
            Console.WriteLine($"Expected that result was type '{typeof(T)}' but found: '{_recordHitResult?.Value.GetType()}'");
            if (_recordHitResult?.Value is ErrorResult err)
            {
                Console.WriteLine($"Error result: {err.Message} --> {err.CalledFrom}\nException: {err.Exception}");
            }
            throw;
        }
    }
    #endregion
}
