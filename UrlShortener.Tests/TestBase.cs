/* The following test harness is something I developed for a personal project that I adapted for work */

global using System;
global using Microsoft.Extensions.DependencyInjection;
global using Moq;
global using NUnit.Framework;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UrlShortener.Tests;

/// <summary>
/// Base class for unit test classes
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// Unit test category
    /// </summary>
    public const string UnitTest = "UnitTest";

    /// <summary>
    /// Integration test category
    /// </summary>
    public const string IntegrationTest = "IntegrationTest";

    private IServiceScope? _serviceScope;

    private IServiceProvider? _serviceProvider;

    private readonly ServiceCollection _services = new();

    /// <summary>
    /// Inject directly into the service collection
    /// </summary>
    protected IServiceCollection Services => _services;

    /// <summary>
    /// Use this for logic attached to integration tests (i.e. using un-mocked services)
    /// </summary>
    /// <remarks>
    /// To have this set to true for a test, decorate your test method with the <see cref="IntegrationModeAttribute"/>
    /// </remarks>
    protected bool IntegrationMode { get; private set; }

    /// <summary>
    /// Override this method to inject mocks or other things into your test's service collection
    /// </summary>
    protected virtual void RegisterServices()
    { }

    /// <summary>
    /// Configures service into service collection
    /// </summary>
    /// <remarks>
    /// Calls <see cref="RegisterServices"/>
    /// </remarks>
    [OneTimeSetUp]
    public virtual void ConfigureServices()
    {
        RegisterServices();
    }

    /// <summary>
    /// Set up before each test
    /// </summary>
    /// <remarks>
    /// This base method creates the service provider and service scope.
    /// Also calls <see cref="InitialSetups"/>.
    /// </remarks>
    [SetUp]
    public virtual void SetUp()
    {
        _serviceProvider = _services.BuildServiceProvider();
        _serviceScope = _serviceProvider.CreateScope();

        InitialSetups();
    }

    /// <summary>
    /// Tear down after each test
    /// </summary>
    /// <remarks>
    /// This base method clears out our service scope, and resets all fields/properties with the <see cref="ResetMeAttribute"/>.
    /// </remarks>
    [TearDown]
    public virtual void TearDown()
    {
        IntegrationMode = false;

        ResetMarkedFieldsAndProperties();
    }

    /// <summary>
    /// Define initial setups for each test
    /// </summary>
    /// <remarks>
    /// Base method sets up whether or not we're in integration mode (i.e. if you've assigned <see cref="IntegrationModeAttribute"/> to your test method)
    /// </remarks>
    protected virtual void InitialSetups()
    {
        string? methodName = TestContext.CurrentContext.Test.MethodName;

        if (!string.IsNullOrWhiteSpace(methodName))
        {
            MethodInfo? testMethod = GetType().GetMethod(methodName);

            IntegrationModeAttribute? integrationMode = testMethod?.GetCustomAttribute<IntegrationModeAttribute>();

            if (integrationMode != null)
            {
                IntegrationMode = true;
            }
        }
    }

    /// <summary>
    /// Clear the scope of services (useful for repeating method calls)
    /// </summary>
    protected void NewServiceScope()
    {
        _serviceScope = _serviceProvider?.CreateScope();
    }

    #region Get/Add Services
    /// <summary>
    /// Register a class into the service collection
    /// </summary>
    /// <typeparam name="T">Class's type</typeparam>
    /// <param name="lifetime">Lifetime for instances of <typeparamref name="T"/></param>
    protected void Register<T>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T : class
    {
        Register<T, T>(lifetime);
    }

    /// <summary>
    /// Register a class into the service collection
    /// </summary>
    /// <typeparam name="T">Class's type</typeparam>
    /// <param name="instanceFactory">Delegate to produce an instance of <typeparamref name="T"/></param>
    /// <param name="lifetime">Lifetime for instances of <typeparamref name="T"/></param>
    protected void Register<T>(Func<IServiceProvider, T> instanceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T : class
    {
        Register<T, T>(instanceFactory, lifetime);
    }

    /// <summary>
    /// Register a class into the service collection
    /// </summary>
    /// <remarks>
    /// When registering with an instance, we'll be registering it as a singleton.
    /// </remarks>
    /// <typeparam name="T">Class's type</typeparam>
    /// <param name="instance">Instance of <typeparamref name="T"/></param>
    protected void Register<T>(T instance)
        where T : class
    {
        Register(sp => instance, ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Register a class into the service collection
    /// </summary>
    /// <typeparam name="T">Class's type</typeparam>
    /// <typeparam name="TImplementation">Class's implementation</typeparam>
    /// <param name="lifetime">Lifetime for instances of <typeparamref name="T"/></param>
    protected void Register<T, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T : class
        where TImplementation : class, T
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                _services.AddSingleton<T, TImplementation>();
                break;
            case ServiceLifetime.Scoped:
                _services.AddScoped<T, TImplementation>();
                break;
            case ServiceLifetime.Transient:
                _services.AddTransient<T, TImplementation>();
                break;
        }
    }

    /// <summary>
    /// Register a class into the service collection
    /// </summary>
    /// <typeparam name="T">Class's type</typeparam>
    /// <typeparam name="TImplementation">Class's implementation</typeparam>
    /// <param name="instanceFactory">Delegate to produce an instance of <typeparamref name="TImplementation"/></param>
    /// <param name="lifetime">Lifetime for instances of <typeparamref name="T"/></param>
    protected void Register<T, TImplementation>(Func<IServiceProvider, TImplementation> instanceFactory, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T : class
        where TImplementation : class, T
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                _services.AddSingleton<T, TImplementation>(instanceFactory);
                break;
            case ServiceLifetime.Scoped:
                _services.AddScoped<T, TImplementation>(instanceFactory);
                break;
            case ServiceLifetime.Transient:
                _services.AddTransient<T, TImplementation>(instanceFactory);
                break;
        }
    }

    /// <summary>
    /// Register an instance of a class into the service collection
    /// </summary>
    /// <remarks>
    /// When registering with an instance, we'll be registering it as a singleton.
    /// </remarks>
    /// <typeparam name="T">Class's type</typeparam>
    /// <typeparam name="TImplementation">Class's implementation</typeparam>
    /// <param name="instance">Instance of <typeparamref name="TImplementation"/></param>
    protected void Register<T, TImplementation>(TImplementation instance)
        where T : class
        where TImplementation : class, T
    {
        Register(sp => instance, ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Register <see cref="Mock"/>&lt;<typeparamref name="TMock"/>&gt; to service collection.
    /// </summary>
    /// <remarks>
    /// NOTE: Mocks are always registered as singletons so that their invocations and setups don't get blown away in the middle of tests.
    ///
    /// </remarks>
    /// <typeparam name="TMock">Mocked type</typeparam>
    /// <param name="registerMockAsImplementation">(Optional) If true, then will register this mock as an implementation of <typeparamref name="TMock"/></param>
    protected void AddMockOf<TMock>(bool registerMockAsImplementation = false)
        where TMock : class
    {
        Register<Mock<TMock>>(ServiceLifetime.Singleton);

        if (registerMockAsImplementation)
        {
            Register(sp => GetMockOf<TMock>().Object, ServiceLifetime.Singleton);
        }
    }

    /// <summary>
    /// Register <see cref="Mock"/>&lt;<typeparamref name="TMock"/>&gt; to service collection with a specific instance
    /// </summary>
    /// <remarks>
    /// NOTE: Mocks are always registered as singletons so that their invocations and setups don't get blown away in the middle of tests.
    ///
    /// </remarks>
    /// <typeparam name="TMock">Mocked type</typeparam>
    /// <param name="registerMockAsImplementation">(Optional) If true, then will register this mock as an implementation of <typeparamref name="TMock"/></param>
    protected void AddMock<TMock>(Mock<TMock> mockInstance, bool registerMockAsImplementation = false)
        where TMock : class
    {
        Register(mockInstance);

        if (registerMockAsImplementation)
        {
            Register(sp => mockInstance.Object, ServiceLifetime.Singleton);
        }
    }

    /// <summary>
    /// Register <see cref="Mock"/>&lt;<typeparamref name="TMock"/>&gt; to service collection with a specific instance
    /// </summary>
    /// <remarks>
    /// NOTE: Mocks are always registered as singletons so that their invocations and setups don't get blown away in the middle of tests.
    ///
    /// </remarks>
    /// <typeparam name="TMock">Mocked type</typeparam>
    /// <param name="registerMocksAsImplementation">(Optional) If true, then will register these mocks as an implementations of <typeparamref name="TMock"/></param>
    protected void AddMultipleMocksOf<TMock>(int howMany, bool registerMocksAsImplementation = false)
        where TMock : class
    {
        for (int i = 0; i < howMany; i++)
        {
            AddMockOf<TMock>(registerMocksAsImplementation);
        }
    }

    /// <summary>
    /// Registers a singleton instance of <see cref="IOptions{TOptions}"/>
    /// </summary>
    /// <typeparam name="TOptions">Options class</typeparam>
    /// <param name="options">Configure <typeparamref name="TOptions"/> action</param>
    protected void AddOptions<TOptions>(Action<TOptions> options)
        where TOptions : class
    {
        _services.Configure(options);
    }

    /// <summary>
    /// Get a mock of <typeparamref name="TMock"/>
    /// </summary>
    /// <remarks>
    /// When getting a mock, service scoping will be ignored (since mocks are all considered to be singletons).
    /// </remarks>
    /// <typeparam name="TMock">Service type we're mocking</typeparam>
    /// <returns>Mock of <typeparamref name="TMock"/></returns>
    protected Mock<TMock> GetMockOf<TMock>()
        where TMock : class
    {
        return GetRegistered<Mock<TMock>>();
    }

    /// <summary>
    /// Get all mocks of <typeparamref name="TMock"/>
    /// </summary>
    /// <remarks>
    /// When getting a mock, service scoping will be ignored (since mocks are all considered to be singletons).
    /// </remarks>
    /// <typeparam name="TMock">Service type we're mocking</typeparam>
    /// <returns>All registered mocks of <typeparamref name="TMock"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected IEnumerable<Mock<TMock>> GetAllMocksOf<TMock>()
        where TMock : class
    {
        return GetAllRegistered<Mock<TMock>>();
    }

    /// <summary>
    /// Get a registered service from the service container
    /// </summary>
    /// <typeparam name="T">Type of service/object to get back</typeparam>
    /// <returns>Instance of <typeparamref name="T"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected T GetRegistered<T>()
        where T : class
    {
        return _serviceScope?.ServiceProvider.GetRequiredService<T>()
            ?? throw new InvalidOperationException($"Current service scope was null. Was {nameof(SetUp)}() somehow not run?");
    }

    /// <summary>
    /// Get all registered implementations of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Type of service/object to get back</typeparam>
    /// <returns>All registered implementations of <typeparamref name="T"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected IEnumerable<T> GetAllRegistered<T>()
        where T : class
    {
        return _serviceScope?.ServiceProvider.GetServices<T>()
            ?? throw new InvalidOperationException($"Current service scope was null. Was {nameof(SetUp)}() somehow not run?");
    }

    /// <summary>
    /// Get a specific implemention of a registered service from the service container
    /// </summary>
    /// <typeparam name="T">Type of service/object that was registered</typeparam>
    /// <typeparam name="TImplementation">Implementation of <typeparamref name="T"/></typeparam>
    /// <returns>Registered instance of <typeparamref name="TImplementation"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected TImplementation GetRegistered<T, TImplementation>()
        where T : class
        where TImplementation : class, T
    {
        IEnumerable<T> services = GetAllRegistered<T>();

        return services.OfType<TImplementation>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Implementation type {typeof(TImplementation)} was not registered with type {typeof(T)}. Add this registration to {nameof(RegisterServices)}() method.");
    }

    /// <summary>
    /// Get registered <see cref="IOptions{TOptions}"/>
    /// </summary>
    /// <typeparam name="TOptions">Options type</typeparam>
    /// <returns>Registered options</returns>
    protected IOptions<TOptions> GetOptions<TOptions>()
        where TOptions : class
    {
        return GetRegistered<IOptions<TOptions>>();
    }
    #endregion

    #region Reset/Clear
    private void ResetMarkedFieldsAndProperties()
    {
        Type type = GetType();

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            ResetMeAttribute? reset = property.GetCustomAttribute<ResetMeAttribute>();
            ClearValuesAttribute? clear = property.GetCustomAttribute<ClearValuesAttribute>();

            reset?.Reset(property, this);
            clear?.Clear(property, this);
        }

        foreach (FieldInfo field in fields)
        {
            ResetMeAttribute? reset = field.GetCustomAttribute<ResetMeAttribute>();
            ClearValuesAttribute? clear = field.GetCustomAttribute<ClearValuesAttribute>();

            reset?.Reset(field, this);
            clear?.Clear(field, this);
        }
    }
    #endregion

    #region Then
    /// <summary>
    /// Assert that no exceptions are thrown from your test scenario
    /// </summary>
    /// <param name="when">Test scenario</param>
    protected static void ThenNoExceptions(TestDelegate when)
    {
        Assert.DoesNotThrow(when);
    }

    /// <summary>
    /// Assert that no exceptions are thrown from your test scenario
    /// </summary>
    /// <param name="when">Test scenario</param>
    protected static void ThenNoExceptions(AsyncTestDelegate when)
    {
        Assert.DoesNotThrowAsync(when);
    }

    /// <summary>
    /// Assert <typeparamref name="TException"/> is thrown from your test scenario
    /// </summary>
    /// <typeparam name="TException">Expected exception type</typeparam>
    /// <param name="when">Test scenario</param>
    protected static void ThenExceptionThrown<TException>(TestDelegate when)
        where TException : Exception
    {
        ThenExceptionThrown<TException>(when, out _);
    }

    /// <summary>
    /// Assert <typeparamref name="TException"/> is thrown from your test scenario
    /// </summary>
    /// <typeparam name="TException">Expected exception type</typeparam>
    /// <param name="when">Test scenario</param>
    /// <param name="exception">The thrown exception</param>
    protected static void ThenExceptionThrown<TException>(TestDelegate when, out TException? exception)
        where TException : Exception
    {
        exception = Assert.Throws<TException>(when);
    }

    /// <summary>
    /// Assert <typeparamref name="TException"/> is thrown from your test scenario
    /// </summary>
    /// <typeparam name="TException">Expected exception type</typeparam>
    /// <param name="when">Test scenario</param>
    protected static void ThenExceptionThrown<TException>(AsyncTestDelegate when)
        where TException : Exception
    {
        ThenExceptionThrown<TException>(when, out _);
    }

    /// <summary>
    /// Assert <typeparamref name="TException"/> is thrown from your test scenario
    /// </summary>
    /// <typeparam name="TException">Expected exception type</typeparam>
    /// <param name="when">Test scenario</param>
    /// <param name="exception">The thrown exception</param>
    protected static void ThenExceptionThrown<TException>(AsyncTestDelegate when, out TException? exception)
        where TException : Exception
    {
        exception = Assert.ThrowsAsync<TException>(when);
    }

    protected static void ThenExceptionPropertyHadThisValue<TException>(
        TException? exception,
        string propertyName,
        object? propertyValue)
        where TException : Exception
    {
        Type type = typeof(TException);

        PropertyInfo propertyInfo = type.GetProperty(propertyName)
            ?? throw new AssertionException($"Expecting exception of type {type} to have property {propertyName}.");

        object? actualValue = propertyInfo.GetValue(exception, null);

        Assert.That(actualValue, Is.EqualTo(propertyValue));
    }
    #endregion

    #region Util
    /// <summary>
    /// Generate a unique id
    /// </summary>
    /// <returns>A unique id as a string</returns>
    protected static string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Generate a number of unique ids
    /// </summary>
    /// <remarks>
    /// To retain this collection ids, call ToList() or ToArray()
    /// </remarks>
    /// <param name="howMany">Number of ids to generate</param>
    /// <returns>Collection of unique ids</returns>
    protected static IEnumerable<string> GenerateIds(int howMany)
    {
        for (int i = 0; i < howMany; i++)
        {
            yield return GenerateId();
        }
    }
    #endregion
}

/// <summary>
/// Base class to inherit when testing a specific class or struct
/// </summary>
/// <typeparam name="T">Class or struct to test</typeparam>
public abstract class TestBase<T> : TestBase
{
    private T? _toTest;

    /// <summary>
    /// Get the instance of <typeparamref name="T"/> we're testing
    /// </summary>
    [ResetMe]
    protected T ToTest
    {
        get
        {
            _toTest ??= InitializeTestObject();
            return _toTest ?? throw new InvalidOperationException($"{nameof(ToTest)} cannot be null.");
        }
        set => _toTest = value;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Base method adds mock of <see cref="ILogger"/>&lt;<typeparamref name="T"/>&gt;
    /// </remarks>
    protected override void RegisterServices()
    {
        base.RegisterServices();

        // adding logger
        AddMockOf<ILogger<T>>(registerMockAsImplementation: true);
    }

    /// <summary>
    /// Mock logger instance
    /// </summary>
    protected Mock<ILogger<T>> Logger => GetMockOf<ILogger<T>>();

    /// <summary>
    /// Override this method to initialize an instance of <typeparamref name="T"/>
    /// </summary>
    /// <returns>New instance of <typeparamref name="T"/></returns>
    protected abstract T InitializeTestObject();

    /// <summary>
    /// Test scenario: Initializing an instance of <typeparamref name="T"/>
    /// </summary>
    protected virtual void WhenInitializing()
    {
        ToTest = InitializeTestObject();
    }
}
