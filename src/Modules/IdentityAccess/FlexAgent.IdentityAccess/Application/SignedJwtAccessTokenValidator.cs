using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlexAgent.IdentityAccess.Application;

public static class SignedJwtAccessTokenValidator
{
    private static readonly HashSet<string> RejectedProductClaims =
    [
        "organization_id",
        "org",
        "role",
        "roles",
        "scope",
        "actor_id",
        "permission",
        "permissions",
    ];

    public static WorkloadAuthenticationResult Validate(
        string? token,
        WorkloadJwtValidationProfile profile,
        IReadOnlyDictionary<string, RSA> keysByKid,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(keysByKid);
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(token))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.MissingToken);
        }

        var trimmed = token.Trim();
        var segments = trimmed.Split('.');
        if (segments.Length != 3)
        {
            return WorkloadAuthenticationResult.Deny(
                segments.Length < 3
                    ? WorkloadAuthenticationReasonCodes.OpaqueToken
                    : WorkloadAuthenticationReasonCodes.MalformedToken);
        }

        if (!TryReadJson(segments[0], out var header)
            || !TryReadJson(segments[1], out var payload))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.MalformedToken);
        }

        if (!header.TryGetProperty("alg", out var algElement)
            || algElement.ValueKind != JsonValueKind.String)
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.UnsignedToken);
        }

        var algorithm = algElement.GetString();
        if (string.IsNullOrWhiteSpace(algorithm)
            || string.Equals(algorithm, "none", StringComparison.OrdinalIgnoreCase))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.UnsignedToken);
        }

        if (!profile.AllowedAlgorithms.Contains(algorithm, StringComparer.Ordinal))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.AlgorithmMismatch);
        }

        if (!header.TryGetProperty("kid", out var kidElement)
            || kidElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(kidElement.GetString())
            || !keysByKid.TryGetValue(kidElement.GetString()!, out var key))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.UnknownKey);
        }

        var signingInput = Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}");
        if (!TryDecodeBase64Url(segments[2], out var signature)
            || !key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.InvalidSignature);
        }

        foreach (var claim in RejectedProductClaims)
        {
            if (payload.TryGetProperty(claim, out _))
            {
                return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.ProductClaimRejected);
            }
        }

        if (!TryReadString(payload, "iss", out var issuer)
            || !string.Equals(issuer, profile.Issuer, StringComparison.Ordinal))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.IssuerMismatch);
        }

        if (!TryReadAudience(payload, profile.Audience))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.AudienceMismatch);
        }

        if (!TryReadString(payload, "sub", out var subject)
            || !string.Equals(subject, profile.ExpectedSubject, StringComparison.Ordinal))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.SubjectMismatch);
        }

        var clientId = TryReadString(payload, "azp", out var azp)
            ? azp
            : TryReadString(payload, "client_id", out var client)
                ? client
                : null;
        if (!string.IsNullOrWhiteSpace(profile.ExpectedClientId)
            && !string.Equals(clientId, profile.ExpectedClientId, StringComparison.Ordinal))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.ClientMismatch);
        }

        if (!TryReadUnixTime(payload, "exp", out var expiresAt)
            || !TryReadUnixTime(payload, "nbf", out var notBefore))
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.MalformedToken);
        }

        var issuedAt = TryReadUnixTime(payload, "iat", out var iat) ? iat : notBefore;
        var now = clock.GetUtcNow();
        if (now + profile.ClockSkew < notBefore)
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.NotYetValid);
        }

        if (now - profile.ClockSkew >= expiresAt)
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.Expired);
        }

        if (issuedAt > expiresAt || now + profile.ClockSkew < issuedAt)
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.IssuedAtInvalid);
        }

        if (expiresAt - notBefore > profile.MaxLifetime)
        {
            return WorkloadAuthenticationResult.Deny(WorkloadAuthenticationReasonCodes.LifetimeExceeded);
        }

        return WorkloadAuthenticationResult.Permit(
            new ValidatedWorkloadProof(
                issuer,
                subject,
                clientId,
                profile.Audience,
                issuedAt,
                notBefore,
                expiresAt,
                now));
    }

    private static bool TryReadAudience(JsonElement payload, string expected)
    {
        if (!payload.TryGetProperty("aud", out var audience))
        {
            return false;
        }

        if (audience.ValueKind == JsonValueKind.String)
        {
            return string.Equals(audience.GetString(), expected, StringComparison.Ordinal);
        }

        if (audience.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in audience.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String
                && string.Equals(item.GetString(), expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadString(JsonElement payload, string name, out string value)
    {
        value = string.Empty;
        if (!payload.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var text = element.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        value = text;
        return true;
    }

    private static bool TryReadUnixTime(JsonElement payload, string name, out DateTimeOffset value)
    {
        value = default;
        if (!payload.TryGetProperty(name, out var element))
        {
            return false;
        }

        long seconds;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out seconds))
        {
            value = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return true;
        }

        return false;
    }

    private static bool TryReadJson(string segment, out JsonElement element)
    {
        element = default;
        if (!TryDecodeBase64Url(segment, out var bytes))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(bytes);
            element = document.RootElement.Clone();
            return element.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase64Url(string segment, out byte[] bytes)
    {
        bytes = [];
        var utf8 = Encoding.ASCII.GetBytes(segment);
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
