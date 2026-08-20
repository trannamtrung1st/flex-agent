namespace FlexAgent.IdentityAccess.Application;

public static class WorkloadIdentityProfiles
{
    public const string SyntheticConfiguredActor = "synthetic.configured_actor";
    public const string OAuthClientCredentialsJwt = "oauth_client_credentials_jwt";
}

public static class WorkloadAuthenticationMethods
{
    public const string ConfiguredActor = "configured_actor";
    public const string OAuthClientCredentialsSignedJwt = "oauth_client_credentials_signed_jwt";
}

public static class WorkloadAuthenticationReasonCodes
{
    public const string MissingToken = "workload.missing_token";
    public const string OpaqueToken = "workload.opaque_token";
    public const string MalformedToken = "workload.malformed_token";
    public const string UnsignedToken = "workload.unsigned_token";
    public const string AlgorithmMismatch = "workload.algorithm_mismatch";
    public const string UnknownKey = "workload.unknown_key";
    public const string InvalidSignature = "workload.invalid_signature";
    public const string IssuerMismatch = "workload.issuer_mismatch";
    public const string AudienceMismatch = "workload.audience_mismatch";
    public const string SubjectMismatch = "workload.subject_mismatch";
    public const string ClientMismatch = "workload.client_mismatch";
    public const string NotYetValid = "workload.not_yet_valid";
    public const string Expired = "workload.expired";
    public const string IssuedAtInvalid = "workload.issued_at_invalid";
    public const string LifetimeExceeded = "workload.lifetime_exceeded";
    public const string ProductClaimRejected = "workload.product_claim_rejected";
    public const string SecretUnavailable = "workload.secret_unavailable";
    public const string TokenEndpointUnavailable = "workload.token_endpoint_unavailable";
}

public static class RecoverableAuthorityStates
{
    public const string Disabled = "disabled";
    public const string Authenticating = "authenticating";
    public const string Ready = "ready";
    public const string RefreshDegraded = "refresh_degraded";
    public const string IdentityDenied = "identity_denied";
    public const string DependencyUnavailable = "dependency_unavailable";
    public const string Stopping = "stopping";
}

public sealed record WorkloadJwtValidationProfile(
    string Issuer,
    string Audience,
    string ExpectedSubject,
    string? ExpectedClientId,
    TimeSpan ClockSkew,
    TimeSpan MaxLifetime,
    IReadOnlyList<string> AllowedAlgorithms)
{
    public static WorkloadJwtValidationProfile Reference(
        string issuer,
        string audience,
        string expectedSubject,
        string? expectedClientId = null) =>
        new(
            issuer,
            audience,
            expectedSubject,
            expectedClientId,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(5),
            ["RS256"]);
}

public sealed record ValidatedWorkloadProof(
    string Issuer,
    string Subject,
    string? ClientId,
    string Audience,
    DateTimeOffset IssuedAt,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt,
    DateTimeOffset ValidatedAt);

public sealed record WorkloadAuthenticationResult(
    bool IsAuthenticated,
    string? ReasonCode,
    ValidatedWorkloadProof? Proof)
{
    public static WorkloadAuthenticationResult Permit(ValidatedWorkloadProof proof) =>
        new(true, null, proof);

    public static WorkloadAuthenticationResult Deny(string reasonCode) =>
        new(false, reasonCode, null);
}

public sealed record AuthenticatedWorkloadContext(
    string Profile,
    string Method,
    string Issuer,
    string Subject,
    string? ClientId,
    string Audience,
    DateTimeOffset IssuedAt,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt,
    DateTimeOffset ValidatedAt,
    Guid ServiceActorId,
    Guid BindingId,
    long BindingVersion,
    string CorrelationReference)
{
    public bool IsProofValidAt(DateTimeOffset utcNow) =>
        utcNow >= NotBefore && utcNow < ExpiresAt;
}

public interface ISecretSource
{
    Task<string?> TryReadAsync(string secretName, CancellationToken cancellationToken = default);
}

public interface IAuthenticatedWorkloadContextSource
{
    Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(CancellationToken cancellationToken = default);
}

public interface IWorkloadTokenClient
{
    Task<string?> RequestClientCredentialsTokenAsync(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string audience,
        CancellationToken cancellationToken = default);
}

public interface IJwksKeySource
{
    Task<JwksKeySnapshot?> TryGetKeysAsync(
        string jwksUri,
        CancellationToken cancellationToken = default);

    Task<JwksKeySnapshot?> TryGetKeysAsync(
        string jwksUri,
        string? requiredKid,
        CancellationToken cancellationToken = default);
}

public interface IRecoverableAuthorityGate
{
    string State { get; }

    bool CanAcceptProtectedWork();

    void SetState(string state);
}

public sealed class RecoverableAuthorityGate : IRecoverableAuthorityGate
{
    private string _state = RecoverableAuthorityStates.Authenticating;

    public string State => Volatile.Read(ref _state);

    public bool CanAcceptProtectedWork() =>
        State is RecoverableAuthorityStates.Ready or RecoverableAuthorityStates.RefreshDegraded;

    public void SetState(string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        while (true)
        {
            var current = Volatile.Read(ref _state);
            if (string.Equals(current, RecoverableAuthorityStates.Stopping, StringComparison.Ordinal)
                && !string.Equals(state, RecoverableAuthorityStates.Stopping, StringComparison.Ordinal))
            {
                return;
            }

            if (ReferenceEquals(Interlocked.CompareExchange(ref _state, state, current), current))
            {
                return;
            }
        }
    }
}
