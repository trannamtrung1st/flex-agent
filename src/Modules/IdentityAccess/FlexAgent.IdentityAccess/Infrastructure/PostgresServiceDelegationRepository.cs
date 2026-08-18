using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

internal static class PostgresServiceDelegationRepository
{
    private const string InsertSql = """
        INSERT INTO service_delegations (
            delegation_id, organization_id, activity_id, participant_id, attempt_id, session_id,
            service_actor_id, allowed_action, system_purpose, initiating_authority,
            effective_at, expires_at, revoked_at, delegation_version, created_at)
        VALUES (
            @DelegationId, @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ServiceActorId, @AllowedAction, @SystemPurpose, @InitiatingAuthority,
            @EffectiveAt, @ExpiresAt, NULL, 1, clock_timestamp());
        """;

    private const string LoadForUpdateSql = """
        SELECT
            allowed_action,
            revoked_at,
            expires_at,
            delegation_version
        FROM service_delegations
        WHERE delegation_id = @DelegationId
          AND organization_id = @OrganizationId
          AND session_id = @SessionId
        FOR UPDATE;
        """;

    private const string RevokeSql = """
        UPDATE service_delegations
        SET
            revoked_at = clock_timestamp(),
            delegation_version = delegation_version + 1
        WHERE delegation_id = @DelegationId
          AND organization_id = @OrganizationId
          AND session_id = @SessionId
          AND revoked_at IS NULL
        RETURNING allowed_action, revoked_at, expires_at, delegation_version;
        """;

    private const string InsertTransitionSql = """
        INSERT INTO service_delegation_transitions (
            transition_id, delegation_id, organization_id, session_id, mutation_kind,
            previous_allowed_action, new_allowed_action, previous_revoked_at, new_revoked_at,
            previous_expires_at, new_expires_at, delegation_version, actor_id, actor_type,
            reason, correlation_id, occurred_at)
        VALUES (
            @TransitionId, @DelegationId, @OrganizationId, @SessionId, @MutationKind,
            @PreviousAllowedAction, @NewAllowedAction, @PreviousRevokedAt, @NewRevokedAt,
            @PreviousExpiresAt, @NewExpiresAt, @DelegationVersion, @ActorId, @ActorType,
            @Reason, @CorrelationId, clock_timestamp());
        """;

    public static Task InsertInTransactionAsync(
        SessionScopedDelegationTarget target,
        ServiceDelegationIssue issue,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(issue);
        return RequireConnection(transaction).ExecuteAsync(
            new CommandDefinition(
                InsertSql,
                new
                {
                    issue.DelegationId,
                    target.OrganizationId,
                    target.ActivityId,
                    target.ParticipantId,
                    target.AttemptId,
                    target.SessionId,
                    issue.ServiceActorId,
                    issue.AllowedAction,
                    issue.SystemPurpose,
                    issue.InitiatingAuthority,
                    EffectiveAt = issue.EffectiveAt.UtcDateTime,
                    ExpiresAt = issue.ExpiresAt?.UtcDateTime,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    public static Task<DelegationStateRow?> LoadForUpdateAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        RequireConnection(transaction).QuerySingleOrDefaultAsync<DelegationStateRow>(
            new CommandDefinition(
                LoadForUpdateSql,
                new { OrganizationId = organizationId, SessionId = sessionId, DelegationId = delegationId },
                transaction,
                cancellationToken: cancellationToken));

    public static Task<DelegationStateRow?> RevokeInTransactionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        RequireConnection(transaction).QuerySingleOrDefaultAsync<DelegationStateRow>(
            new CommandDefinition(
                RevokeSql,
                new { OrganizationId = organizationId, SessionId = sessionId, DelegationId = delegationId },
                transaction,
                cancellationToken: cancellationToken));

    public static Task InsertTransitionAsync(
        object parameters,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        RequireConnection(transaction).ExecuteAsync(
            new CommandDefinition(InsertTransitionSql, parameters, transaction, cancellationToken: cancellationToken));

    private static NpgsqlConnection RequireConnection(NpgsqlTransaction transaction) =>
        transaction.Connection
        ?? throw new InvalidOperationException("Delegation writes require an open transaction connection.");
}

internal sealed record DelegationStateRow(
    string allowed_action,
    DateTime? revoked_at,
    DateTime? expires_at,
    long delegation_version);

public sealed record SessionScopedDelegationTarget(
    Guid OrganizationId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId);
