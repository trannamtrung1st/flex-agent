using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.IdentityAccess.Application;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexAgent.Runtime.Tests;

public sealed class WorkloadIdentityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Signed_rs256_token_authenticates_against_the_pinned_profile()
    {
        using var rsa = RSA.Create(2048);
        var token = WorkloadJwtTestSupport.CreateToken(rsa, T0);
        var clock = new FrozenTimeProvider(T0.AddMinutes(1));

        var result = SignedJwtAccessTokenValidator.Validate(
            token,
            WorkloadJwtTestSupport.Profile(),
            WorkloadJwtTestSupport.Keys(rsa),
            clock);

        Assert.True(result.IsAuthenticated);
        Assert.Equal("worker-client", result.Proof!.Subject);
        Assert.DoesNotContain("eyJ", result.Proof.Subject, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("opaque-access-token")]
    [InlineData("not.a.jwt.extra")]
    public void Missing_or_opaque_tokens_are_rejected(string token)
    {
        using var rsa = RSA.Create(2048);

        var result = SignedJwtAccessTokenValidator.Validate(
            token,
            WorkloadJwtTestSupport.Profile(),
            WorkloadJwtTestSupport.Keys(rsa),
            new FrozenTimeProvider(T0));

        Assert.False(result.IsAuthenticated);
        Assert.Contains(
            result.ReasonCode,
            (string[])
            [
                WorkloadAuthenticationReasonCodes.MissingToken,
                WorkloadAuthenticationReasonCodes.OpaqueToken,
                WorkloadAuthenticationReasonCodes.MalformedToken,
            ],
            StringComparer.Ordinal);
    }

    [Fact]
    public void Unsigned_and_algorithm_confused_tokens_are_rejected()
    {
        using var rsa = RSA.Create(2048);
        var none = WorkloadJwtTestSupport.CreateToken(rsa, T0, algorithm: "none", sign: false);
        var hs = WorkloadJwtTestSupport.CreateToken(rsa, T0, algorithm: "HS256", sign: false);

        Assert.Equal(
            WorkloadAuthenticationReasonCodes.UnsignedToken,
            SignedJwtAccessTokenValidator.Validate(
                none,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0)).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.AlgorithmMismatch,
            SignedJwtAccessTokenValidator.Validate(
                hs,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0)).ReasonCode);
    }

    [Fact]
    public void Wrong_key_issuer_audience_subject_and_client_are_rejected()
    {
        using var rsa = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var clock = new FrozenTimeProvider(T0.AddMinutes(1));
        var token = WorkloadJwtTestSupport.CreateToken(rsa, T0);

        Assert.Equal(
            WorkloadAuthenticationReasonCodes.InvalidSignature,
            SignedJwtAccessTokenValidator.Validate(
                token,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(other),
                clock).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.UnknownKey,
            SignedJwtAccessTokenValidator.Validate(
                token,
                WorkloadJwtTestSupport.Profile(),
                new Dictionary<string, RSA>(StringComparer.Ordinal),
                clock).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.IssuerMismatch,
            SignedJwtAccessTokenValidator.Validate(
                WorkloadJwtTestSupport.CreateToken(rsa, T0, issuer: "https://other.example"),
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                clock).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.AudienceMismatch,
            SignedJwtAccessTokenValidator.Validate(
                WorkloadJwtTestSupport.CreateToken(rsa, T0, audience: "other-audience"),
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                clock).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.SubjectMismatch,
            SignedJwtAccessTokenValidator.Validate(
                WorkloadJwtTestSupport.CreateToken(rsa, T0, subject: "other-worker"),
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                clock).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.ClientMismatch,
            SignedJwtAccessTokenValidator.Validate(
                WorkloadJwtTestSupport.CreateToken(rsa, T0, clientId: "other-client"),
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                clock).ReasonCode);
    }

    [Fact]
    public void Not_yet_valid_expired_and_overlong_tokens_are_rejected()
    {
        using var rsa = RSA.Create(2048);
        var future = WorkloadJwtTestSupport.CreateToken(rsa, T0, notBeforeOffset: TimeSpan.FromMinutes(10));
        var expired = WorkloadJwtTestSupport.CreateToken(rsa, T0, lifetime: TimeSpan.FromMinutes(1));
        var overlong = WorkloadJwtTestSupport.CreateToken(rsa, T0, lifetime: TimeSpan.FromHours(2));

        Assert.Equal(
            WorkloadAuthenticationReasonCodes.NotYetValid,
            SignedJwtAccessTokenValidator.Validate(
                future,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0)).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.Expired,
            SignedJwtAccessTokenValidator.Validate(
                expired,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0.AddMinutes(2))).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.LifetimeExceeded,
            SignedJwtAccessTokenValidator.Validate(
                overlong,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0.AddMinutes(1))).ReasonCode);
    }

    [Fact]
    public void Future_or_inverted_issued_at_does_not_bypass_max_lifetime()
    {
        using var rsa = RSA.Create(2048);
        var futureIssuedAt = WorkloadJwtTestSupport.CreateToken(rsa, T0, extraClaims: new Dictionary<string, object>
        {
            ["iat"] = T0.AddHours(2).ToUnixTimeSeconds(),
        });
        var invertedIssuedAt = WorkloadJwtTestSupport.CreateToken(
            rsa,
            T0,
            lifetime: TimeSpan.FromMinutes(5),
            extraClaims: new Dictionary<string, object>
            {
                ["iat"] = T0.AddHours(1).ToUnixTimeSeconds(),
            });

        Assert.Equal(
            WorkloadAuthenticationReasonCodes.IssuedAtInvalid,
            SignedJwtAccessTokenValidator.Validate(
                futureIssuedAt,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0)).ReasonCode);
        Assert.Equal(
            WorkloadAuthenticationReasonCodes.IssuedAtInvalid,
            SignedJwtAccessTokenValidator.Validate(
                invertedIssuedAt,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0)).ReasonCode);
    }

    [Fact]
    public void Product_permission_claims_are_rejected()
    {
        using var rsa = RSA.Create(2048);
        var token = WorkloadJwtTestSupport.CreateToken(rsa, T0, extraClaims: new Dictionary<string, object>
        {
            ["organization_id"] = Guid.NewGuid().ToString("D"),
        });

        Assert.Equal(
            WorkloadAuthenticationReasonCodes.ProductClaimRejected,
            SignedJwtAccessTokenValidator.Validate(
                token,
                WorkloadJwtTestSupport.Profile(),
                WorkloadJwtTestSupport.Keys(rsa),
                new FrozenTimeProvider(T0.AddMinutes(1))).ReasonCode);
    }

    [Fact]
    public async Task Mounted_file_secret_source_reads_only_files_under_the_root()
    {
        var root = Directory.CreateTempSubdirectory("flexagent-secrets");
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root.FullName, "client-secret"),
                "s3cret-value\n",
                TestContext.Current.CancellationToken);
            var source = new FlexAgent.IdentityAccess.Infrastructure.MountedFileSecretSource(root.FullName);

            Assert.Equal(
                "s3cret-value",
                await source.TryReadAsync("client-secret", TestContext.Current.CancellationToken));
            Assert.Null(await source.TryReadAsync("../client-secret", TestContext.Current.CancellationToken));
            Assert.Null(await source.TryReadAsync("missing", TestContext.Current.CancellationToken));
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Recoverable_authority_gate_is_independent_of_the_shutdown_gate()
    {
        var shutdown = new FlexAgent.Worker.WorkClaimGate();
        var authority = new RecoverableAuthorityGate();
        authority.SetState(RecoverableAuthorityStates.Ready);
        Assert.True(shutdown.TryClaimWork());
        Assert.True(authority.CanAcceptProtectedWork());

        authority.SetState(RecoverableAuthorityStates.IdentityDenied);
        Assert.True(shutdown.TryClaimWork());
        Assert.False(authority.CanAcceptProtectedWork());

        authority.SetState(RecoverableAuthorityStates.Ready);
        shutdown.StopAcceptingWork();
        Assert.False(shutdown.TryClaimWork());
        Assert.True(authority.CanAcceptProtectedWork());

        authority.SetState(RecoverableAuthorityStates.RefreshDegraded);
        Assert.False(shutdown.TryClaimWork());
        Assert.True(authority.CanAcceptProtectedWork());
    }

    [Fact]
    public void Recoverable_authority_gate_does_not_leave_stopping()
    {
        var authority = new RecoverableAuthorityGate();
        authority.SetState(RecoverableAuthorityStates.Stopping);
        authority.SetState(RecoverableAuthorityStates.Ready);
        Assert.Equal(RecoverableAuthorityStates.Stopping, authority.State);
        Assert.False(authority.CanAcceptProtectedWork());
    }

    [Fact]
    public async Task Ready_check_degrades_when_identity_source_has_no_current_principal()
    {
        var shutdown = new FlexAgent.Worker.WorkClaimGate();
        var authority = new RecoverableAuthorityGate();
        authority.SetState(RecoverableAuthorityStates.Ready);
        var check = new FlexAgent.Worker.WorkerReadinessCheck(
            shutdown,
            authority,
            new FlexAgent.Worker.WorkerRuntimeCapabilities { DurableWorkClaimingEnabled = true },
            new MissingWorkloadIdentitySource());

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(RecoverableAuthorityStates.IdentityDenied, result.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Refresh_does_not_replace_identity_denied_with_dependency_unavailable()
    {
        var authority = new RecoverableAuthorityGate();
        authority.SetState(RecoverableAuthorityStates.IdentityDenied);
        FlexAgent.Worker.WorkloadIdentityRefreshService.ApplyObservation(authority, context: null);
        Assert.Equal(RecoverableAuthorityStates.IdentityDenied, authority.State);
    }

    [Fact]
    public void Jwks_parser_imports_rsa_verification_keys()
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(false);
        var jwks = $$"""
            {"keys":[{"kty":"RSA","kid":"{{WorkloadJwtTestSupport.KeyId}}","n":"{{Encode(parameters.Modulus!)}}","e":"{{Encode(parameters.Exponent!)}}"}]}
            """;

        var keys = JwksRsaKeyParser.TryParse(jwks);

        Assert.NotNull(keys);
        Assert.True(keys.ContainsKey(WorkloadJwtTestSupport.KeyId));
        var token = WorkloadJwtTestSupport.CreateToken(rsa, T0);
        Assert.True(
            SignedJwtAccessTokenValidator.Validate(
                token,
                WorkloadJwtTestSupport.Profile(),
                keys,
                new FrozenTimeProvider(T0.AddMinutes(1))).IsAuthenticated);
    }

    [Fact]
    public async Task Client_credentials_token_client_reads_access_token_without_logging_it()
    {
        using var handler = new StubJsonHandler("""{"access_token":"hdr.payload.sig","token_type":"Bearer"}""");
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
        var client = new FlexAgent.IdentityAccess.Infrastructure.HttpWorkloadTokenClient(http);

        var token = await client.RequestClientCredentialsTokenAsync(
            "https://issuer.example/token",
            "worker-client",
            "unused-secret",
            "flex-agent-worker",
            TestContext.Current.CancellationToken);

        Assert.Equal("hdr.payload.sig", token);
        Assert.Contains("grant_type=client_credentials", handler.LastBody, StringComparison.Ordinal);
        Assert.DoesNotContain("unused-secret", token, StringComparison.Ordinal);
    }

    private static string Encode(byte[] bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }

    private sealed class MissingWorkloadIdentitySource : IAuthenticatedWorkloadContextSource
    {
        public Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthenticatedWorkloadContext?>(null);
    }

    private sealed class StubJsonHandler(string json) : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

internal static class WorkloadJwtTestSupport
{
    public const string Issuer = "https://issuer.example/realms/flex-agent";
    public const string Audience = "flex-agent-worker";
    public const string Subject = "worker-client";
    public const string ClientId = "worker-client";
    public const string KeyId = "worker-key-1";

    public static WorkloadJwtValidationProfile Profile() =>
        WorkloadJwtValidationProfile.Reference(Issuer, Audience, Subject, ClientId);

    public static IReadOnlyDictionary<string, RSA> Keys(RSA rsa) =>
        new Dictionary<string, RSA>(StringComparer.Ordinal) { [KeyId] = rsa };

    public static string CreateToken(
        RSA rsa,
        DateTimeOffset now,
        string issuer = Issuer,
        string audience = Audience,
        string subject = Subject,
        string clientId = ClientId,
        string algorithm = "RS256",
        bool sign = true,
        TimeSpan? lifetime = null,
        TimeSpan? notBeforeOffset = null,
        IReadOnlyDictionary<string, object>? extraClaims = null)
    {
        var nbf = now + (notBeforeOffset ?? TimeSpan.Zero);
        var exp = nbf + (lifetime ?? TimeSpan.FromMinutes(5));
        var header = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["alg"] = algorithm,
            ["typ"] = "JWT",
            ["kid"] = KeyId,
        });
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = issuer,
            ["aud"] = audience,
            ["sub"] = subject,
            ["azp"] = clientId,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = nbf.ToUnixTimeSeconds(),
            ["exp"] = exp.ToUnixTimeSeconds(),
        };
        if (extraClaims is not null)
        {
            foreach (var pair in extraClaims)
            {
                payload[pair.Key] = pair.Value;
            }
        }

        var payloadJson = JsonSerializer.Serialize(payload);
        var encodedHeader = Encode(Encoding.UTF8.GetBytes(header));
        var encodedPayload = Encode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = sign
            ? Encode(rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            : Encode("not-a-signature"u8.ToArray());
        return $"{signingInput}.{signature}";
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }
}
