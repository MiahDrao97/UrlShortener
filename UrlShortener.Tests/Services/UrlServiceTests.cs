using AutoMapper;
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
    private UrlInput? _urlInput;

    [ResetMe]
    private string? _aliasLookup;

    [ResetMe]
    private ValueResult<ShortenedUrlOuput>? _outputResult;

    [ResetMe]
    private ValueResult<string>? _lookupResult;
    #endregion

    #region Mocks
    private Mock<IShortenedUrlRepository> Repo => GetMockOf<IShortenedUrlRepository>();
    #endregion

    #region Overrides
    protected override UrlService InitializeTestObject()
    {
        return new UrlService(
            Repo.Object,
            GetRegistered<IMapper>(),
            GetRegistered<IConfigurationProvider>(),
            Logger.Object);
    }

    protected override void RegisterServices()
    {
        base.RegisterServices();

        AddMockOf<IShortenedUrlRepository>();
        Services.AddAutoMapper(static opts => opts.AddMaps([typeof(ShortenedUrlProfile)]));
    }
    #endregion

    #region Given
    private void GivenUrlInput(UrlInput input) => _urlInput = input;

    private void GivenAlias(string @alias) => _aliasLookup = @alias;

    private void GivenRepositoryThrows(string method, Exception ex)
    {
        switch (method)
        {
            case nameof(IShortenedUrlRepository.GetByAlias):
                Repo.Setup(static r => r.GetByAlias(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);
                break;
            case nameof(IShortenedUrlRepository.Insert):
                Repo.Setup(static r => r.Insert(It.IsAny<ShortenedUrl>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);
                break;
            default:
                throw new NotSupportedException($"No method on {typeof(IShortenedUrlRepository)} called '{method}'");
        }
    }
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
            Console.WriteLine($"Expected that result was type '{typeof(T)}' but found: '{_outputResult?.Value.GetType()}'");
            if (_outputResult?.Value is ErrorResult err)
            {
                Console.WriteLine($"Error result: {err.Message} --> {err.CalledFrom}\nException: {err.Exception}");
            }
            throw;
        }
    }
    #endregion
}
