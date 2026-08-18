using System.Security.Cryptography;
using System.Text;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Audit;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public static class PostgresServiceDelegationCoordinator
{
    public static readonly TimeSpan TimerLaneFireMaxLifetime = TimeSpan.FromDays(7);

    public static async Task IssueInTransactionAsync(
        SessionScopedDelegationTarget target,
        ServiceDelegationIssue issue,
        TrustedActor actor,
        Guid correlationId,
        string sourceChannel,
        string reason,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(transaction);
        ValidateActor(actor);
        ValidateReason(reason);
        ValidateTimerLaneFireLifetime(issue);
        var writer = auditEventWriter ?? new PostgresAuditEventWriter();
        await PostgresServiceDelegationRepository.InsertInTransactionAsync(
            target,
            issue,
            transaction,
            cancellationToken);
        await WriteHistoryAndAuditAsync(
            writer,
            target.OrganizationId,
            target.SessionId,
            issue.DelegationId,
            actor,
            correlationId,
            sourceChannel,
            reason,
            AuthorizationActions.IssueServiceDelegation,
            "issue",
            previousAllowedAction: null,
            newAllowedAction: issue.AllowedAction,
            previousRevokedAt: null,
            newRevokedAt: null,
            previousExpiresAt: null,
            newExpiresAt: issue.ExpiresAt?.UtcDateTime,
            delegationVersion: 1,
            transaction,
            cancellationToken);
    }

    public static async Task<bool> RevokeInTransactionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        TrustedActor actor,
        Guid correlationId,
        string sourceChannel,
        string reason,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(transaction);
        ValidateActor(actor);
        ValidateReason(reason);
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
            auditEventWriter ?? new PostgresAuditEventWriter(),
            organizationId,
            sessionId,
            delegationId,
            actor,
            correlationId,
            sourceChannel,
            reason,
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
    }

    public static async Task<bool> NarrowAllowedActionInTransactionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        string allowedAction,
        TrustedActor actor,
        Guid correlationId,
        string sourceChannel,
        string reason,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(transaction);
        ValidateActor(actor);
        ValidateReason(reason);
        if (string.IsNullOrWhiteSpace(allowedAction))
        {
            throw new ArgumentOutOfRangeException(nameof(allowedAction));
        }

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

        var updated = await PostgresServiceDelegationRepository.NarrowAllowedActionInTransactionAsync(
            organizationId,
            sessionId,
            delegationId,
            allowedAction,
            transaction,
            cancellationToken);
        if (updated is null)
        {
            return false;
        }

        await WriteHistoryAndAuditAsync(
            auditEventWriter ?? new PostgresAuditEventWriter(),
            organizationId,
            sessionId,
            delegationId,
            actor,
            correlationId,
            sourceChannel,
            reason,
            AuthorizationActions.NarrowServiceDelegation,
            "narrow",
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

    private static async Task WriteHistoryAndAuditAsync(
        IAuditEventWriter auditEventWriter,
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        TrustedActor actor,
        Guid correlationId,
        string sourceChannel,
        string reason,
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
                ActorId = actor.ActorId,
                ActorType = actor.ActorType,
                Reason = reason,
                CorrelationId = correlationId,
            },
            transaction,
            cancellationToken);

        var digestSource =
            $"{mutationKind}|{delegationId:N}|{previousAllowedAction ?? ""}|{newAllowedAction}|{delegationVersion}|{reason}";
        await auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                EventId: Guid.NewGuid(),
                OrganizationId: organizationId,
                EventSchemaVersion: "audit-event.v1",
                OccurredAt: occurredAt,
                CorrelationId: correlationId,
                ActorType: actor.ActorType,
                ActorId: actor.ActorId,
                Action: auditAction,
                ResourceType: AuthorizationResourceTypes.Session,
                ResourceId: sessionId,
                Outcome: "succeeded",
                ReasonCode: null,
                RelationshipVersion: delegationVersion,
                SourceChannel: sourceChannel,
                PayloadDigest: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestSource)))
                    .ToLowerInvariant(),
                AuthorizationReferenceType: AuthorizationReferenceTypes.ServiceDelegation,
                AuthorizationReferenceId: delegationId),
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
