using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlexAgent.IdentityAccess.Application;

public static class JwksRsaKeyParser
{
    public static IReadOnlyDictionary<string, RSA>? TryParse(string? jwksJson)
    {
        var parameters = TryParseParameters(jwksJson);
        if (parameters is null)
        {
            return null;
        }

        return JwksKeySnapshot.TryFromParameters(parameters)?.Keys;
    }

    public static IReadOnlyDictionary<string, RSAParameters>? TryParseParameters(string? jwksJson)
    {
        if (string.IsNullOrWhiteSpace(jwksJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jwksJson);
            if (!document.RootElement.TryGetProperty("keys", out var keys)
                || keys.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var parsed = new Dictionary<string, RSAParameters>(StringComparer.Ordinal);
            foreach (var key in keys.EnumerateArray())
            {
                if (!key.TryGetProperty("kty", out var kty)
                    || kty.ValueKind != JsonValueKind.String
                    || !string.Equals(kty.GetString(), "RSA", StringComparison.Ordinal)
                    || !key.TryGetProperty("kid", out var kidElement)
                    || kidElement.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(kidElement.GetString())
                    || !key.TryGetProperty("n", out var nElement)
                    || !key.TryGetProperty("e", out var eElement)
                    || nElement.ValueKind != JsonValueKind.String
                    || eElement.ValueKind != JsonValueKind.String
                    || !TryDecodeBase64Url(nElement.GetString(), out var modulus)
                    || !TryDecodeBase64Url(eElement.GetString(), out var exponent))
                {
                    continue;
                }

                var candidate = new RSAParameters
                {
                    Modulus = modulus,
                    Exponent = exponent,
                };
                if (!IsUsableRsaPublicKey(candidate))
                {
                    continue;
                }

                parsed[kidElement.GetString()!] = candidate;
            }

            return parsed.Count == 0 ? null : parsed;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static bool IsUsableRsaPublicKey(RSAParameters parameters)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportParameters(parameters);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase64Url(string? value, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var utf8 = Encoding.ASCII.GetBytes(value);
        bytes = new byte[Base64Url.GetMaxDecodedLength(utf8.Length)];
        if (!Base64Url.TryDecodeFromUtf8(utf8, bytes, out var written))
        {
            bytes = [];
            return false;
        }

        if (written != bytes.Length)
        {
            Array.Resize(ref bytes, written);
        }

        return true;
    }
}
