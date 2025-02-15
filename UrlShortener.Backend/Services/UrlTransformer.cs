using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace UrlShortener.Backend.Services;

/// <inheritdoc cref="IUrlTransformer" />
public sealed class UrlTransformer(ILogger<UrlTransformer> logger) : IUrlTransformer
{
    private readonly ILogger<UrlTransformer> _logger = logger;

    /// <inheritdoc />
    public string CreateAlias(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        Span<byte> bytes = stackalloc byte[16]; // hash results in 128 bits (16 bytes)
        // this hash algorithm effectively creates a GUID for us, based of a hash of the input
        int written = MD5.HashData(Encoding.UTF8.GetBytes(input), bytes);
        if (written != 16)
        {
            _logger.LogWarning("MD5 algorithm wrote {actual} bytes instead of 16 for input '{input}'.", written, input);
            // throw?
        }
        return Encoding.ASCII.GetString(bytes);
    }
}
