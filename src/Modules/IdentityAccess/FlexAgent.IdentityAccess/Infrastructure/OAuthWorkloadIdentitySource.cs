using FlexAgent.IdentityAccess.Application;
using FlexAgent.Postgres;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class OAuthWorkloadIdentitySource(
    ISecretSource secrets,
    IWorkloadTokenClient tokenClient,
    IJwksKeySource jwksKeySource,
    PostgresConnectionAccessor connectionAccessor,
    WorkloadJwtValidationProfile validationProfile,
    string tokenEndpoint,
    string jwksUri,
    string clientSecretName,
    Guid expectedServiceActorId,
    TimeProvider clock,
    TimeSpan refreshMargin) : IAuthenticatedWorkloadContextSource
{
    private readonly object _gate = new();
    private AuthenticatedWorkloadContext? _current;

    public async Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        lock (_gate)
        {
            if (_current is not null && _current.ExpiresAt - now > refreshMargin && _current.IsProofValidAt(now))
            {
                return _current;
            }
        }

        var refreshed = await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            if (refreshed is not null)
            {
                _current = refreshed;
                return _current;
            }

            if (_current is not null && _current.IsProofValidAt(clock.GetUtcNow()))
            {
                return _current;
            }

            _current = null;
            return null;
        }
    }

    private async Task<AuthenticatedWorkloadContext?> AuthenticateAsync(CancellationToken cancellationToken)
    {
        var secret = await secrets.TryReadAsync(clientSecretName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        string? token;
        try
        {
            token = await tokenClient.RequestClientCredentialsTokenAsync(
                tokenEndpoint,
                validationProfile.ExpectedClientId ?? validationProfile.ExpectedSubject,
                secret,
                validationProfile.Audience,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }

        IReadOnlyDictionary<string, System.Security.Cryptography.RSA>? keys;
        try
        {
            keys = await jwksKeySource.TryGetKeysAsync(jwksUri, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }

        if (keys is null)
        {
            return null;
        }

        var authentication = SignedJwtAccessTokenValidator.Validate(token, validationProfile, keys, clock);
        if (!authentication.IsAuthenticated || authentication.Proof is null)
        {
            return null;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var binding = await PostgresServicePrincipalBindingCoordinator.LoadCurrentAsync(
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                authentication.Proof.Issuer,
                authentication.Proof.Subject,
                authentication.Proof.Audience,
                scope.Transaction,
                cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            if (binding is null
                || binding.ServiceActorId != expectedServiceActorId
                || binding.EffectiveAt > clock.GetUtcNow())
            {
                return null;
            }

            var proof = authentication.Proof;
            return new AuthenticatedWorkloadContext(
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                proof.Issuer,
                proof.Subject,
                proof.ClientId,
                proof.Audience,
                proof.IssuedAt,
                proof.NotBefore,
                proof.ExpiresAt,
                proof.ValidatedAt,
                binding.ServiceActorId,
                binding.BindingId,
                binding.BindingVersion,
                $"{binding.BindingId:N}:{binding.BindingVersion}");
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
