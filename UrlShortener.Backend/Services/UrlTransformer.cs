using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace UrlShortener.Backend.Services;

/// <inheritdoc cref="IUrlTransformer" />
public sealed class UrlTransformer(ILogger<UrlTransformer> logger) : IUrlTransformer
{
    private readonly ILogger<UrlTransformer> _logger = logger;

    /// <inheritdoc />
    public ValueResult<string> CreateAlias(string input)
    {
        // server error
        if (string.IsNullOrWhiteSpace(input))
        {
            ArgumentException argEx = new($"{nameof(input)} cannot be null or whitespace. Was '{input ?? "<null>"}'", nameof(input));
            _logger.LogError(argEx, "Received invalid value for parameter '{param}'", nameof(input));
            return argEx;
        }

        Span<byte> bytes = stackalloc byte[16]; // hash results in 128 bits (16 bytes)
        int written;
        try
        {
            // choosing to disable this warning because we're not using this for cryptographic purposes
#pragma warning disable CA5351
            // this hash algorithm effectively creates a GUID for us, based of a hash of the input
            written = MD5.HashData(Encoding.UTF8.GetBytes(input), bytes);
#pragma warning restore CA5351
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while hashing '{input}' with MD5 algorithm", input);
            return ex;
        }

        // server error
        if (written != 16)
        {
            InvalidOperationException ex = new($"Unexpected behavior: MD5 algorithm wrote {written} bytes instead of 16 for input '{input}'.");
            _logger.LogError(ex, "Encountered unexpected behavior from MD5 algorithm while creating alias for input '{input}'", input);
            return ex;
        }

        return new Ok<string>(Encoding.ASCII.GetString(bytes));
    }
}
