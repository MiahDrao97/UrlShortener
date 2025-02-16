namespace UrlShortener.Tests;

/// <summary>
/// Use this attribute on your test method to set IntegrationMode = true
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IntegrationModeAttribute : Attribute
{
    /// <summary>
    /// Default ctor
    /// </summary>
    public IntegrationModeAttribute() : base()
    { }
}
