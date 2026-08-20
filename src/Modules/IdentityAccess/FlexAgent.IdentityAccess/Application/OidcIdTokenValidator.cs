using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.IdentityAccess.Application;

public sealed record OidcValidationProfile(
    string Issuer,
    string Audience,
    TimeSpan ClockSkew,
    TimeSpan MaxLifetime)
{
    public static TimeSpan MaximumClockSkew { get; } = TimeSpan.FromSeconds(60);

    public TimeSpan BoundedClockSkew =>
        ClockSkew <= TimeSpan.Zero || ClockSkew > MaximumClockSkew ? MaximumClockSkew : ClockSkew;
}

public sealed record ValidatedOidcIdToken(
    ExactIssuerSubject Identity,
    AuthenticationStrength Strength,
    string Nonce,
    string? ProviderSessionId);

public sealed record OidcValidationResult(bool Succeeded, string? ReasonCode, ValidatedOidcIdToken? Token)
{
    public static OidcValidationResult Deny(string reasonCode) => new(false, reasonCode, null);

    public static OidcValidationResult Permit(ValidatedOidcIdToken token) => new(true, null, token);
}

public static class OidcIdTokenValidator
{
    public static OidcValidationResult Validate(
        string? token,
        string expectedNonce,
        OidcValidationProfile profile,
        IReadOnlyDictionary<string, RSA> keysByKid,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedNonce);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(keysByKid);
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(token))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var segments = token.Trim().Split('.');
        if (segments.Length != 3
            || !TryReadJson(segments[0], out var header)
            || !TryReadJson(segments[1], out var payload))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        if (!header.TryGetProperty("alg", out var alg)
            || alg.ValueKind != JsonValueKind.String
            || !string.Equals(alg.GetString(), "RS256", StringComparison.Ordinal)
            || !header.TryGetProperty("kid", out var kid)
            || kid.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(kid.GetString())
            || !keysByKid.TryGetValue(kid.GetString()!, out var key))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var signingInput = Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}");
        if (!TryDecodeBase64Url(segments[2], out var signature)
            || !key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        if (!TryReadString(payload, "iss", out var issuer)
            || !string.Equals(issuer, profile.Issuer, StringComparison.Ordinal)
            || !AudienceMatches(payload, profile.Audience)
            || !TryReadString(payload, "sub", out var subject)
            || !TryReadString(payload, "nonce", out var nonce)
            || !string.Equals(nonce, expectedNonce, StringComparison.Ordinal)
            || !TryReadUnixTime(payload, "exp", out var expiresAt)
            || !TryReadUnixTime(payload, "nbf", out var notBefore))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var identity = ExactIssuerSubject.TryCreate(issuer, subject);
        if (identity is null)
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var now = clock.GetUtcNow();
        var skew = profile.BoundedClockSkew;
        if (now + skew < notBefore || now - skew >= expiresAt || expiresAt - notBefore > profile.MaxLifetime)
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        TryReadString(payload, "sid", out var providerSessionId);
        return OidcValidationResult.Permit(
            new ValidatedOidcIdToken(
                identity,
                ReadStrength(payload),
                nonce,
                string.IsNullOrWhiteSpace(providerSessionId) ? null : providerSessionId));
    }

    public static OidcValidationResult ValidateLogoutToken(
        string? token,
        OidcValidationProfile profile,
        IReadOnlyDictionary<string, RSA> keysByKid,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(keysByKid);
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(token))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var segments = token.Trim().Split('.');
        if (segments.Length != 3
            || !TryReadJson(segments[0], out var header)
            || !TryReadJson(segments[1], out var payload))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        if (!header.TryGetProperty("alg", out var alg)
            || alg.ValueKind != JsonValueKind.String
            || !string.Equals(alg.GetString(), "RS256", StringComparison.Ordinal)
            || !header.TryGetProperty("kid", out var kid)
            || kid.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(kid.GetString())
            || !keysByKid.TryGetValue(kid.GetString()!, out var key))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var signingInput = Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}");
        if (!TryDecodeBase64Url(segments[2], out var signature)
            || !key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        if (!TryReadString(payload, "iss", out var issuer)
            || !string.Equals(issuer, profile.Issuer, StringComparison.Ordinal)
            || !AudienceMatches(payload, profile.Audience)
            || !TryReadString(payload, "sid", out var providerSessionId)
            || !TryReadUnixTime(payload, "exp", out var expiresAt))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        if (payload.TryGetProperty("events", out var events)
            && events.ValueKind == JsonValueKind.Object
            && !events.TryGetProperty("http://schemas.openid.net/event/backchannel-logout", out _))
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var now = clock.GetUtcNow();
        var skew = profile.BoundedClockSkew;
        if (now - skew >= expiresAt)
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        if (TryReadUnixTime(payload, "nbf", out var notBefore) && now + skew < notBefore)
        {
            return OidcValidationResult.Deny(HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var identity = ExactIssuerSubject.TryCreate(issuer, TryReadString(payload, "sub", out var subject) ? subject : "logout");
        if (identity is null)
        {
            identity = new ExactIssuerSubject(issuer, "logout");
        }

        return OidcValidationResult.Permit(
            new ValidatedOidcIdToken(identity, AuthenticationStrength.Empty, string.Empty, providerSessionId));
    }

    private static AuthenticationStrength ReadStrength(JsonElement payload)
    {
        TryReadString(payload, "acr", out var acr);
        var amr = new List<string>();
        if (payload.TryGetProperty("amr", out var amrElement))
        {
            if (amrElement.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(amrElement.GetString()))
            {
                amr.Add(amrElement.GetString()!);
            }
            else if (amrElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in amrElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        amr.Add(item.GetString()!);
                    }
                }
            }
        }

        return new AuthenticationStrength(string.IsNullOrWhiteSpace(acr) ? null : acr, amr);
    }

    private static bool AudienceMatches(JsonElement payload, string expected)
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
        if (!payload.TryGetProperty(name, out var element)
            || element.ValueKind != JsonValueKind.Number
            || !element.TryGetInt64(out var seconds))
        {
            return false;
        }

        value = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
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
