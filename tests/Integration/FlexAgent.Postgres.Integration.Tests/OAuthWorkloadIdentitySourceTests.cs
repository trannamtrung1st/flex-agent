using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class OAuthWorkloadIdentitySourceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    private const string Issuer = "https://issuer.example/realms/flex-agent";
    private const string Audience = "flex-agent-worker";
    private const string KeyId = "worker-jwt-key";

    [Fact]
    public async Task Revoked_binding_does_not_mint_another_token_while_the_proof_is_valid()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.RevokeServicePrincipalBinding);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        var subject = $"worker-client-{Guid.NewGuid():N}";
        var bindingId = Guid.NewGuid();
        var mutation = new ServiceDelegationMutationContext(
            organization.Actor,
            Guid.NewGuid(),
            "operator.command",
            "oauth.observer.binding");
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
                organization.OrganizationId,
                new ServicePrincipalBindingProvision(
                    bindingId,
                    WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                    WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                    Issuer,
                    subject,
                    subject,
                    Audience,
                    workerActorId,
                    "worker.session_runtime",
                    DateTimeOffset.UtcNow),
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var tokens = new CountingTokenClient(CreateToken(rsa, now, subject));
        var keys = new StaticJwksSource(rsa);
        var authority = new RecoverableAuthorityGate();
        var source = new OAuthWorkloadIdentitySource(
            new StaticSecretSource("client-secret", "unused-secret"),
            tokens,
            keys,
            Fixture.Services.ConnectionAccessor,
            WorkloadJwtValidationProfile.Reference(Issuer, Audience, subject, subject),
            "https://issuer.example/token",
            "https://issuer.example/jwks",
            "client-secret",
            workerActorId,
            TimeProvider.System,
            TimeSpan.FromSeconds(60),
            authority);

        var permitted = await source.TryGetCurrentAsync(CancellationToken);
        Assert.NotNull(permitted);
        Assert.Equal(1, tokens.Requests);
        Assert.Equal(bindingId, permitted!.BindingId);

        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.RevokeInTransactionAsync(
                organization.OrganizationId,
                bindingId,
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        Assert.Null(await source.TryGetCurrentAsync(CancellationToken));
        Assert.Null(await source.TryGetCurrentAsync(CancellationToken));
        Assert.Equal(1, tokens.Requests);
        Assert.Equal(RecoverableAuthorityStates.IdentityDenied, authority.State);
    }

    [Fact]
    public async Task Near_expiry_refresh_mints_once_while_the_binding_remains_usable()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        var subject = $"worker-client-{Guid.NewGuid():N}";
        var bindingId = Guid.NewGuid();
        var mutation = new ServiceDelegationMutationContext(
            organization.Actor,
            Guid.NewGuid(),
            "operator.command",
            "oauth.observer.refresh");
        var issuedAt = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
                organization.OrganizationId,
                new ServicePrincipalBindingProvision(
                    bindingId,
                    WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                    WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                    Issuer,
                    subject,
                    subject,
                    Audience,
                    workerActorId,
                    "worker.session_runtime",
                    issuedAt),
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        using var rsa = RSA.Create(2048);
        var clock = new MutableTimeProvider(issuedAt);
        var tokens = new CountingTokenClient(CreateToken(rsa, issuedAt, subject));
        var source = new OAuthWorkloadIdentitySource(
            new StaticSecretSource("client-secret", "unused-secret"),
            tokens,
            new StaticJwksSource(rsa),
            Fixture.Services.ConnectionAccessor,
            WorkloadJwtValidationProfile.Reference(Issuer, Audience, subject, subject),
            "https://issuer.example/token",
            "https://issuer.example/jwks",
            "client-secret",
            workerActorId,
            clock,
            TimeSpan.FromMinutes(2));

        Assert.NotNull(await source.TryGetCurrentAsync(CancellationToken));
        Assert.Equal(1, tokens.Requests);

        clock.SetUtcNow(issuedAt.AddMinutes(3).AddSeconds(30));
        var refreshed = await source.TryGetCurrentAsync(CancellationToken);
        Assert.NotNull(refreshed);
        Assert.Equal(bindingId, refreshed!.BindingId);
        Assert.Equal(2, tokens.Requests);
    }

    private static string CreateToken(RSA rsa, DateTimeOffset now, string subject)
    {
        var nbf = now;
        var exp = nbf.AddMinutes(5);
        var header = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["alg"] = "RS256",
            ["typ"] = "JWT",
            ["kid"] = KeyId,
        });
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["sub"] = subject,
            ["azp"] = subject,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = nbf.ToUnixTimeSeconds(),
            ["exp"] = exp.ToUnixTimeSeconds(),
        });
        var encodedHeader = Encode(Encoding.UTF8.GetBytes(header));
        var encodedPayload = Encode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{encodedHeader}.{encodedPayload}";
        var signature = Encode(
            rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return $"{signingInput}.{signature}";
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }

    private sealed class CountingTokenClient(string token) : IWorkloadTokenClient
    {
        public int Requests { get; private set; }

        public Task<string?> RequestClientCredentialsTokenAsync(
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            string audience,
            CancellationToken cancellationToken = default)
        {
            Requests++;
            return Task.FromResult<string?>(token);
        }
    }

    private sealed class StaticJwksSource(RSA rsa) : IJwksKeySource
    {
        public Task<IReadOnlyDictionary<string, RSA>?> TryGetKeysAsync(
            string jwksUri,
            CancellationToken cancellationToken = default) =>
            TryGetKeysAsync(jwksUri, requiredKid: null, cancellationToken);

        public Task<IReadOnlyDictionary<string, RSA>?> TryGetKeysAsync(
            string jwksUri,
            string? requiredKid,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, RSA>?>(
                new Dictionary<string, RSA>(StringComparer.Ordinal) { [KeyId] = rsa });
    }

    private sealed class StaticSecretSource(string name, string value) : ISecretSource
    {
        public Task<string?> TryReadAsync(string secretName, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Equals(secretName, name, StringComparison.Ordinal) ? value : null);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void SetUtcNow(DateTimeOffset value) => utcNow = value;
    }
}
