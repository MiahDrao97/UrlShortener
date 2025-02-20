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
            _logger.LogError("Received invalid value for parameter '{param}'", nameof(input));
            return new ErrorResult
            {
                Message = $"{nameof(input)} cannot be null/whitespace. Was: '{input ?? "<null>"}'",
            };
        }

        _logger.LogDebug("Creating hash for '{input}'", input);
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
            return new ErrorResult
            {
                Exception = ex,
                Message = $"Unexpected error while hashing '{input}' with MD5 algorithm.",
            };
        }

        // server error
        if (written != 16)
        {
            _logger.LogError("Encountered unexpected behavior from MD5 algorithm while creating alias for input '{input}'", input);
            return new ErrorResult
            {
                Message = $"Unexpected behavior: MD5 algorithm wrote {written} bytes instead of 16 for input '{input}'.",
            };
        }

        return new Ok<string>(Encoding.ASCII.GetString(bytes));
    }

    /// <inheritdoc />
    public string CreateUrlSafeAlias(string? @alias, short offset)
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{@alias}{offset}")).Base64ToUrlSafe();
    }

    /// <inheritdoc />
    public ValueResult<(string, short)> FromUrlSafeAlias(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new ErrorResult
            {
                Message = $"{nameof(input)} cannot be null/empty.",
            };
        }

        Span<byte> aliasBytes = stackalloc byte[17]; // 16 bytes for MD5 hash + 1 for offset

        if (!Convert.TryFromBase64Chars(input.UrlSafeToStandardBase64(), aliasBytes, out int bytesWritten))
        {
            _logger.LogError("Alias '{alias}' is not base64-encoded.", input);
            return new ErrorResult
            {
                Message = $"Alias '{input}' is not base64-encoded.",
            };
        }

        string decoded = Encoding.ASCII.GetString(aliasBytes);
        if (bytesWritten != 17)
        {
            // unfortunately does not detect if larger than 17 bytes
            _logger.LogError("Alias '{@alias}' (decoded '{decoded}') is not at least 17 bytes (ASCII characters).", input, decoded);
            return new ErrorResult
            {
                Message = $"Alias '{input}' (decoded '{decoded}') is not at least 17 bytes (ASCII characters).",
            };
        }

        if (!short.TryParse([decoded[^1]], out short offset))
        {
            _logger.LogError("Final character '{char}' did not parse to a valid offset for alias '{alias}' (decoded: {decoded})", decoded[^1], input, decoded);
            return new ErrorResult
            {
                Message = $"Final character '{decoded[^1]}' did not parse to a valid offset for alias '{input}' (decoded: {decoded}).",
            };
        }

        return new Ok<(string, short)>((decoded[..16], offset));
    }
}
