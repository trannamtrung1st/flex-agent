using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Runtime.Tests;

public sealed class OidcIdTokenValidatorTests
{
    private const string Issuer = "https://issuer.example/realms/flex";
    private const string Audience = "flex-agent-api";
    private static readonly TimeProvider Clock = TimeProvider.System;
    private static readonly OidcValidationProfile Profile = new(
        Issuer,
        Audience,
        TimeSpan.FromSeconds(60),
        TimeSpan.FromMinutes(10));

    [Fact]
    public void Id_token_requires_iat_and_accepts_missing_nbf()
    {
        using var rsa = RSA.Create(2048);
        var keys = Keys(rsa);
        var nonce = "nonce-1";
        var withoutNbf = Sign(rsa, IdClaims(nonce, includeIat: true, includeNbf: false));
        var withoutIat = Sign(rsa, IdClaims(nonce, includeIat: false, includeNbf: true));

        var accepted = OidcIdTokenValidator.Validate(withoutNbf, nonce, Profile, keys, Clock);
        var rejected = OidcIdTokenValidator.Validate(withoutIat, nonce, Profile, keys, Clock);

        Assert.True(accepted.Succeeded);
        Assert.Equal("subject-1", accepted.Token!.Identity.Subject);
        Assert.True(accepted.Token.IssuedAt > DateTimeOffset.UtcNow.AddMinutes(-2));
        Assert.Null(accepted.Token.SeatedDisplayName);
        Assert.False(rejected.Succeeded);
        Assert.Equal(HumanAuthenticationReasonCodes.InvalidProviderResponse, rejected.ReasonCode);
    }

    [Fact]
    public void Id_token_profile_claims_compose_the_seated_display_name()
    {
        using var rsa = RSA.Create(2048);
        var keys = Keys(rsa);
        var nonce = "nonce-display";
        var claims = IdClaims(nonce, includeIat: true, includeNbf: false);
        claims["given_name"] = "Demo";
        claims["family_name"] = "Participant";
        claims["preferred_username"] = "demo.participant";

        var accepted = OidcIdTokenValidator.Validate(Sign(rsa, claims), nonce, Profile, keys, Clock);

        Assert.True(accepted.Succeeded);
        Assert.Equal("Demo Participant", accepted.Token!.SeatedDisplayName);
    }

    [Fact]
    public void Id_token_lifetime_is_measured_from_iat_to_exp()
    {
        using var rsa = RSA.Create(2048);
        var keys = Keys(rsa);
        var nonce = "nonce-2";
        var now = DateTimeOffset.UtcNow;
        var oversized = Sign(rsa, IdClaims(
            nonce,
            includeIat: true,
            includeNbf: true,
            iat: now.AddMinutes(-20),
            nbf: now.AddMinutes(-1),
            exp: now.AddMinutes(1)));

        var rejected = OidcIdTokenValidator.Validate(oversized, nonce, Profile, keys, Clock);

        Assert.False(rejected.Succeeded);
    }

    [Fact]
    public void Present_nbf_is_enforced_when_the_token_is_not_yet_valid()
    {
        using var rsa = RSA.Create(2048);
        var keys = Keys(rsa);
        var nonce = "nonce-3";
        var now = DateTimeOffset.UtcNow;
        var futureNbf = Sign(rsa, IdClaims(
            nonce,
            includeIat: true,
            includeNbf: true,
            iat: now,
            nbf: now.AddMinutes(5),
            exp: now.AddMinutes(8)));

        var rejected = OidcIdTokenValidator.Validate(futureNbf, nonce, Profile, keys, Clock);

        Assert.False(rejected.Succeeded);
    }

    [Fact]
    public void Logout_token_requires_iat_jti_events_and_rejects_nonce()
    {
        using var rsa = RSA.Create(2048);
        var keys = Keys(rsa);
        var valid = Sign(rsa, LogoutClaims(includeIat: true, includeJti: true, includeEvents: true, includeSid: true));
        var missingIat = Sign(rsa, LogoutClaims(includeIat: false, includeJti: true, includeEvents: true, includeSid: true));
        var missingJti = Sign(rsa, LogoutClaims(includeIat: true, includeJti: false, includeEvents: true, includeSid: true));
        var missingEvents = Sign(rsa, LogoutClaims(includeIat: true, includeJti: true, includeEvents: false, includeSid: true));
        var withNonce = Sign(rsa, LogoutClaims(includeIat: true, includeJti: true, includeEvents: true, includeSid: true, includeNonce: true));

        Assert.True(OidcIdTokenValidator.ValidateLogoutToken(valid, Profile, keys, Clock).Succeeded);
        Assert.False(OidcIdTokenValidator.ValidateLogoutToken(missingIat, Profile, keys, Clock).Succeeded);
        Assert.False(OidcIdTokenValidator.ValidateLogoutToken(missingJti, Profile, keys, Clock).Succeeded);
        Assert.False(OidcIdTokenValidator.ValidateLogoutToken(missingEvents, Profile, keys, Clock).Succeeded);
        Assert.False(OidcIdTokenValidator.ValidateLogoutToken(withNonce, Profile, keys, Clock).Succeeded);
    }

    [Fact]
    public void Logout_token_accepts_sub_only_and_requires_sub_or_sid()
    {
        using var rsa = RSA.Create(2048);
        var keys = Keys(rsa);
        var subOnly = Sign(rsa, LogoutClaims(includeIat: true, includeJti: true, includeEvents: true, includeSid: false, includeSub: true));
        var neither = Sign(rsa, LogoutClaims(includeIat: true, includeJti: true, includeEvents: true, includeSid: false, includeSub: false));

        var accepted = OidcIdTokenValidator.ValidateLogoutToken(subOnly, Profile, keys, Clock);
        var rejected = OidcIdTokenValidator.ValidateLogoutToken(neither, Profile, keys, Clock);

        Assert.True(accepted.Succeeded);
        Assert.Equal("subject-1", accepted.LogoutToken!.Subject);
        Assert.Null(accepted.LogoutToken.ProviderSessionId);
        Assert.True(accepted.LogoutToken.IssuedAt > DateTimeOffset.UtcNow.AddMinutes(-2));
        Assert.False(rejected.Succeeded);
    }

    private static Dictionary<string, RSA> Keys(RSA rsa) =>
        new(StringComparer.Ordinal) { ["test"] = rsa };

    private static Dictionary<string, object?> IdClaims(
        string nonce,
        bool includeIat,
        bool includeNbf,
        DateTimeOffset? iat = null,
        DateTimeOffset? nbf = null,
        DateTimeOffset? exp = null)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["sub"] = "subject-1",
            ["nonce"] = nonce,
            ["sid"] = "sid-1",
            ["exp"] = (exp ?? now.AddMinutes(5)).ToUnixTimeSeconds(),
        };
        if (includeIat)
        {
            claims["iat"] = (iat ?? now).ToUnixTimeSeconds();
        }

        if (includeNbf)
        {
            claims["nbf"] = (nbf ?? now.AddMinutes(-1)).ToUnixTimeSeconds();
        }

        return claims;
    }

    private static Dictionary<string, object?> LogoutClaims(
        bool includeIat,
        bool includeJti,
        bool includeEvents,
        bool includeSid,
        bool includeSub = true,
        bool includeNonce = false)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
        };
        if (includeIat)
        {
            claims["iat"] = now.ToUnixTimeSeconds();
        }

        if (includeJti)
        {
            claims["jti"] = "jti-" + Guid.NewGuid().ToString("N");
        }

        if (includeEvents)
        {
            claims["events"] = new Dictionary<string, object>
            {
                ["http://schemas.openid.net/event/backchannel-logout"] = new { },
            };
        }

        if (includeSid)
        {
            claims["sid"] = "sid-1";
        }

        if (includeSub)
        {
            claims["sub"] = "subject-1";
        }

        if (includeNonce)
        {
            claims["nonce"] = "must-not-be-present";
        }

        return claims;
    }

    private static string Sign(RSA rsa, Dictionary<string, object?> claims)
    {
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = "test" });
        var payload = JsonSerializer.Serialize(claims);
        var encodedHeader = Encode(Encoding.UTF8.GetBytes(header));
        var encodedPayload = Encode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = Encode(rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return $"{signingInput}.{signature}";
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }
}
