using System.Security.Cryptography;
using System.Text;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Audit;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public static class PostgresServiceDelegationCoordinator
{
    public static readonly TimeSpan TimerLaneFireMaxLifetime = TimeSpan.FromDays(7);

    public static Task IssueInTransactionAsync(
        SessionScopedDelegationTarget target,
        AuthorizedServiceDelegationIssue authorizedIssue,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null,
        bool reauthorizeBeforeReturn = true) =>
        MutateInTransactionAsync(
            target.OrganizationId,
            target.SessionId,
            authorizedIssue.Issue.DelegationId,
            authorizedIssue.Mutation,
            AuthorizationActions.IssueServiceDelegation,
            authorizationKernel,
            transaction,
            async (decision, writer) =>
            {
                ArgumentNullException.ThrowIfNull(target);
                ValidateTimerLaneFireLifetime(authorizedIssue.Issue);
                await PostgresServiceDelegationRepository.InsertInTransactionAsync(
                    target,
                    authorizedIssue.Issue,
                    transaction,
                    cancellationToken);
                await WriteHistoryAndAuditAsync(
                    writer,
                    target.OrganizationId,
                    target.SessionId,
                    authorizedIssue.Issue.DelegationId,
                    authorizedIssue.Mutation,
                    decision,
                    AuthorizationActions.IssueServiceDelegation,
                    "issue",
                    previousAllowedAction: null,
                    newAllowedAction: authorizedIssue.Issue.AllowedAction,
                    previousRevokedAt: null,
                    newRevokedAt: null,
                    previousExpiresAt: null,
                    newExpiresAt: authorizedIssue.Issue.ExpiresAt?.UtcDateTime,
                    delegationVersion: 1,
                    transaction,
                    cancellationToken);
                return true;
            },
            cancellationToken,
            auditEventWriter,
            reauthorizeBeforeReturn);

    public static Task<bool> RevokeInTransactionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        ServiceDelegationMutationContext mutation,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null,
        bool reauthorizeBeforeReturn = true) =>
        MutateInTransactionAsync(
            organizationId,
            sessionId,
            delegationId,
            mutation,
            AuthorizationActions.RevokeServiceDelegation,
            authorizationKernel,
            transaction,
            async (decision, writer) =>
            {
                var current = await PostgresServiceDelegationRepository.LoadForUpdateAsync(
                    organizationId,
                    sessionId,
                    delegationId,
                    transaction,
                    cancellationToken);
                if (current is null || current.revoked_at is not null)
                {
                    return false;
                }

                var updated = await PostgresServiceDelegationRepository.RevokeInTransactionAsync(
                    organizationId,
                    sessionId,
                    delegationId,
                    transaction,
                    cancellationToken);
                if (updated is null)
                {
                    return false;
                }

                await WriteHistoryAndAuditAsync(
                    writer,
                    organizationId,
                    sessionId,
                    delegationId,
                    mutation,
                    decision,
                    AuthorizationActions.RevokeServiceDelegation,
                    "revoke",
                    current.allowed_action,
                    updated.allowed_action,
                    current.revoked_at,
                    updated.revoked_at,
                    current.expires_at,
                    updated.expires_at,
                    updated.delegation_version,
                    transaction,
                    cancellationToken);
                return true;
            },
            cancellationToken,
            auditEventWriter,
            reauthorizeBeforeReturn);

    public static async Task ReauthorizeMutationInTransactionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        ServiceDelegationMutationContext mutation,
        string action,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(authorizationKernel);
        ArgumentNullException.ThrowIfNull(transaction);
        var decision = await authorizationKernel.ReauthorizeInTransactionAsync(
            CreateMutationRequest(organizationId, delegationId, mutation, action),
            transaction,
            cancellationToken);
        if (!decision.IsPermitted)
        {
            AfterFinalAuthorizationDeniedBeforeAbort?.Invoke();
            await AbortCallerTransactionAsync(transaction);
            throw new AuthorizationDeniedException(decision.ReasonCode);
        }
    }

    public static Action? AfterFinalAuthorizationDeniedBeforeAbort { get; set; }

    public static async Task AbortCallerTransactionAsync(NpgsqlTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is InvalidOperationException or OperationCanceledException)
        {
        }
    }

    public static void ValidateTimerLaneFireLifetime(ServiceDelegationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        if (!string.Equals(issue.AllowedAction, AuthorizationActions.FireSessionTimerLane, StringComparison.Ordinal))
        {
            return;
        }

        if (issue.ExpiresAt is not { } expiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(issue),
                "session.timer_lane.fire delegations require a UTC expiry.");
        }

        if (expiresAt <= issue.EffectiveAt)
        {
            throw new ArgumentOutOfRangeException(nameof(issue), "Expiry must be after the effective time.");
        }

        if (expiresAt - issue.EffectiveAt > TimerLaneFireMaxLifetime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(issue),
                "session.timer_lane.fire delegations cannot exceed a 7-day lifetime.");
        }
    }

    private static async Task<bool> MutateInTransactionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        ServiceDelegationMutationContext mutation,
        string action,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        Func<AuthorizationDecision, IAuditEventWriter, Task<bool>> mutateAsync,
        CancellationToken cancellationToken,
        IAuditEventWriter? auditEventWriter,
        bool reauthorizeBeforeReturn)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(authorizationKernel);
        ArgumentNullException.ThrowIfNull(transaction);
        ValidateActor(mutation.Initiator);
        ValidateReason(mutation.Reason);
        var request = CreateMutationRequest(organizationId, delegationId, mutation, action);
        var admission = await authorizationKernel.AuthorizeInTransactionAsync(
            request,
            transaction,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            throw new AuthorizationDeniedException(admission.ReasonCode);
        }

        var mutated = await mutateAsync(admission, auditEventWriter ?? new PostgresAuditEventWriter());
        if (!mutated || !reauthorizeBeforeReturn)
        {
            return mutated;
        }

        await ReauthorizeMutationInTransactionAsync(
            organizationId,
            sessionId,
            delegationId,
            mutation,
            action,
            authorizationKernel,
            transaction,
            cancellationToken);
        return true;
    }

    private static AuthorizationRequest CreateMutationRequest(
        Guid organizationId,
        Guid delegationId,
        ServiceDelegationMutationContext mutation,
        string action) =>
        new(
            mutation.Initiator,
            new OrganizationScope(organizationId),
            action,
            new ResourceScope(
                new OrganizationScope(organizationId),
                AuthorizationResourceTypes.ServiceDelegation,
                delegationId),
            mutation.SourceChannel,
            mutation.CorrelationId);

    private static async Task WriteHistoryAndAuditAsync(
        IAuditEventWriter auditEventWriter,
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        ServiceDelegationMutationContext mutation,
        AuthorizationDecision decision,
        string auditAction,
        string mutationKind,
        string? previousAllowedAction,
        string newAllowedAction,
        DateTime? previousRevokedAt,
        DateTime? newRevokedAt,
        DateTime? previousExpiresAt,
        DateTime? newExpiresAt,
        long delegationVersion,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        await PostgresServiceDelegationRepository.InsertTransitionAsync(
            new
            {
                TransitionId = Guid.NewGuid(),
                DelegationId = delegationId,
                OrganizationId = organizationId,
                SessionId = sessionId,
                MutationKind = mutationKind,
                PreviousAllowedAction = previousAllowedAction,
                NewAllowedAction = newAllowedAction,
                PreviousRevokedAt = previousRevokedAt,
                NewRevokedAt = newRevokedAt,
                PreviousExpiresAt = previousExpiresAt,
                NewExpiresAt = newExpiresAt,
                DelegationVersion = delegationVersion,
                ActorId = mutation.Initiator.ActorId,
                ActorType = mutation.Initiator.ActorType,
                Reason = mutation.Reason,
                CorrelationId = mutation.CorrelationId,
            },
            transaction,
            cancellationToken);

        var digestSource =
            $"{mutationKind}|{delegationId:N}|{previousAllowedAction ?? ""}|{newAllowedAction}|{delegationVersion}|{mutation.Reason}";
        await auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                EventId: Guid.NewGuid(),
                OrganizationId: organizationId,
                EventSchemaVersion: "audit-event.v1",
                OccurredAt: occurredAt,
                CorrelationId: mutation.CorrelationId,
                ActorType: mutation.Initiator.ActorType,
                ActorId: mutation.Initiator.ActorId,
                Action: auditAction,
                ResourceType: AuthorizationResourceTypes.ServiceDelegation,
                ResourceId: delegationId,
                Outcome: "succeeded",
                ReasonCode: null,
                RelationshipVersion: decision.RelationshipVersion,
                SourceChannel: mutation.SourceChannel,
                PayloadDigest: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestSource)))
                    .ToLowerInvariant(),
                AuthorizationReferenceType: decision.AuthorizationReferenceType,
                AuthorizationReferenceId: decision.AuthorizationReferenceId),
            transaction,
            cancellationToken);
    }

    private static void ValidateActor(TrustedActor actor)
    {
        if (actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(actor.ActorType))
        {
            throw new ArgumentOutOfRangeException(nameof(actor));
        }
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }
    }
}
