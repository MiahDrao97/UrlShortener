using OneOf;

namespace UrlShortener.Backend;

/// <summary>
/// Result type that's either <see cref="Ok"/> or an exception
/// </summary>
[GenerateOneOf]
public partial class Result : OneOfBase<Ok, Exception>
{ }
