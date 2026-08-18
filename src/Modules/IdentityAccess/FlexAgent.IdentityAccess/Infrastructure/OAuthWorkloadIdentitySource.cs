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
    TimeSpan refreshMargin,
    IRecoverableAuthorityGate? authorityGate = null) : IAuthenticatedWorkloadContextSource
{
    private readonly object _gate = new();
    private AuthenticatedWorkloadContext? _current;

    public async Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        AuthenticatedWorkloadContext? cached;
        lock (_gate)
        {
            cached = _current is not null && _current.IsProofValidAt(now) ? _current : null;
        }

        if (cached is not null)
        {
            var observed = await ObserveBindingAsync(cached, cancellationToken).ConfigureAwait(false);
            if (observed is null)
            {
                authorityGate?.SetState(RecoverableAuthorityStates.IdentityDenied);
                return null;
            }

            if (observed.ExpiresAt - now > refreshMargin)
            {
                return observed;
            }

            var rotated = await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
            if (rotated is not null)
            {
                return rotated;
            }

            observed = await ObserveBindingAsync(cached, cancellationToken).ConfigureAwait(false);
            if (observed is null)
            {
                authorityGate?.SetState(RecoverableAuthorityStates.IdentityDenied);
                return null;
            }

            return observed;
        }

        return await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
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

        var proof = authentication.Proof;
        var binding = await LoadCurrentBindingAsync(
            proof.Issuer,
            proof.Subject,
            proof.Audience,
            cancellationToken).ConfigureAwait(false);
        var context = CreateContext(proof, binding);
        lock (_gate)
        {
            _current = context;
        }

        if (!IsUsableBinding(binding))
        {
            authorityGate?.SetState(RecoverableAuthorityStates.IdentityDenied);
            return null;
        }

        return context;
    }

    private async Task<AuthenticatedWorkloadContext?> ObserveBindingAsync(
        AuthenticatedWorkloadContext cached,
        CancellationToken cancellationToken)
    {
        var binding = await LoadCurrentBindingAsync(
            cached.Issuer,
            cached.Subject,
            cached.Audience,
            cancellationToken).ConfigureAwait(false);
        if (!IsUsableBinding(binding))
        {
            return null;
        }

        var observed = cached with
        {
            ServiceActorId = binding!.ServiceActorId,
            BindingId = binding.BindingId,
            BindingVersion = binding.BindingVersion,
            CorrelationReference = $"{binding.BindingId:N}:{binding.BindingVersion}",
        };
        lock (_gate)
        {
            if (_current is not null && _current.IsProofValidAt(clock.GetUtcNow()))
            {
                _current = _current with
                {
                    ServiceActorId = observed.ServiceActorId,
                    BindingId = observed.BindingId,
                    BindingVersion = observed.BindingVersion,
                    CorrelationReference = observed.CorrelationReference,
                };
            }
        }

        return observed;
    }

    private async Task<ServicePrincipalBindingRecord?> LoadCurrentBindingAsync(
        string issuer,
        string subject,
        string audience,
        CancellationToken cancellationToken)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var binding = await PostgresServicePrincipalBindingCoordinator.LoadCurrentAsync(
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                issuer,
                subject,
                audience,
                scope.Transaction,
                cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return binding;
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private bool IsUsableBinding(ServicePrincipalBindingRecord? binding) =>
        binding is not null
        && binding.ServiceActorId == expectedServiceActorId
        && binding.EffectiveAt <= clock.GetUtcNow();

    private AuthenticatedWorkloadContext CreateContext(
        ValidatedWorkloadProof proof,
        ServicePrincipalBindingRecord? binding) =>
        new(
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
            binding?.ServiceActorId ?? expectedServiceActorId,
            binding?.BindingId ?? Guid.Empty,
            binding?.BindingVersion ?? 0,
            binding is null ? "unbound" : $"{binding.BindingId:N}:{binding.BindingVersion}");
}
