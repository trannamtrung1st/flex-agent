using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.IdentityAccess.Application;

public sealed record ValidatedHumanLogin(
    ExactIssuerSubject Identity,
    AuthenticationStrength Strength,
    string? ProviderSessionId);

public sealed record HumanAuthenticationResult(
    bool Succeeded,
    string? ReasonCode,
    Guid? ApplicationSessionId,
    Guid? ActorId,
    Guid? OrganizationId,
    string? RawCredential)
{
    public static HumanAuthenticationResult Deny(string reasonCode) =>
        new(false, reasonCode, null, null, null, null);

    public static HumanAuthenticationResult Permit(
        Guid applicationSessionId,
        Guid actorId,
        Guid organizationId,
        string rawCredential) =>
        new(true, null, applicationSessionId, actorId, organizationId, rawCredential);
}

public sealed record AuthenticatedApplicationSession(
    Guid ApplicationSessionId,
    Guid ActorId,
    Guid OrganizationId,
    AuthenticationStrength Strength,
    ExactIssuerSubject Identity);

public sealed record OidcLoginTransaction(
    Guid TransactionId,
    string StateDigest,
    string Nonce,
    string CodeVerifier,
    string ReturnPath,
    DateTimeOffset ExpiresAt,
    Guid CorrelationId);

public sealed record AuthenticationSecurityEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string EventType,
    string Outcome,
    string ReasonCode,
    Guid CorrelationId,
    Guid? ActorId,
    Guid? OrganizationId,
    Guid? ApplicationSessionId);

public interface IDatabaseClock
{
    Task<DateTimeOffset> GetUtcNowAsync(CancellationToken cancellationToken = default);
}

public interface IHumanIdentityBindingStore
{
    Task<HumanIdentityBinding?> FindByIdentityAsync(
        ExactIssuerSubject identity,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListEligibleOrganizationIdsAsync(
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<(bool Exists, bool Disabled)> GetActorStateAsync(
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<string?> TryProvisionAsync(
        HumanIdentityBinding binding,
        CancellationToken cancellationToken = default);

    Task DisableByIdentityAsync(
        ExactIssuerSubject identity,
        DateTimeOffset disabledAt,
        CancellationToken cancellationToken = default);
}

public interface IApplicationSessionStore
{
    Task InsertAsync(ApplicationSessionRecord session, CancellationToken cancellationToken = default);

    Task<ApplicationSessionRecord?> FindLiveByCredentialDigestAsync(
        string credentialDigest,
        CancellationToken cancellationToken = default);

    Task<ApplicationSessionRecord?> GetByIdAsync(
        Guid applicationSessionId,
        CancellationToken cancellationToken = default);

    Task TerminateLiveAsync(
        Guid applicationSessionId,
        DateTimeOffset terminatedAt,
        string terminalReason,
        bool rotated,
        CancellationToken cancellationToken = default);

    Task TouchActivityAsync(
        Guid applicationSessionId,
        ApplicationSessionLifetime lifetime,
        CancellationToken cancellationToken = default);

    Task<int> RevokeLiveByIdentityAsync(
        ExactIssuerSubject identity,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default);

    Task<int> RevokeLiveByProviderSessionDigestAsync(
        string providerSessionDigest,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default);
}

public interface IOidcLoginTransactionStore
{
    Task CreateAsync(OidcLoginTransaction transaction, CancellationToken cancellationToken = default);

    Task<OidcLoginTransaction?> ConsumeAsync(
        string stateDigest,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public interface IAuthenticationSecurityEventWriter
{
    Task WriteAsync(AuthenticationSecurityEvent securityEvent, CancellationToken cancellationToken = default);
}

public interface ILookupDigestCalculator
{
    string Compute(string value);
}

public interface IHumanAuthenticationCoordinator
{
    Task<HumanAuthenticationResult> CompleteLoginAsync(
        ValidatedHumanLogin login,
        Guid? clientSuppliedOrganizationId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedApplicationSession?> AuthenticateAsync(
        string rawCredential,
        bool advanceActivity,
        CancellationToken cancellationToken = default);

    Task<HumanAuthenticationResult> RotateAsync(
        Guid applicationSessionId,
        string terminalReason,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(
        string rawCredential,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<int> ApplyProviderForcedLogoutAsync(
        string providerSessionId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<int> ApplyAccountDisablementAsync(
        ExactIssuerSubject identity,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}

public sealed class SystemDatabaseClock(TimeProvider timeProvider) : IDatabaseClock
{
    public Task<DateTimeOffset> GetUtcNowAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(timeProvider.GetUtcNow());
    }
}
