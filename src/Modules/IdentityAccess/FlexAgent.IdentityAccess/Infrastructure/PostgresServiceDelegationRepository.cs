using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class PostgresServiceDelegationRepository(PostgresConnectionAccessor connectionAccessor)
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

    private const string RevokeSql = """
        UPDATE service_delegations
        SET
            revoked_at = clock_timestamp(),
            delegation_version = delegation_version + 1
        WHERE delegation_id = @DelegationId
          AND organization_id = @OrganizationId
          AND session_id = @SessionId
          AND revoked_at IS NULL;
        """;

    private const string NarrowActionSql = """
        UPDATE service_delegations
        SET
            allowed_action = @AllowedAction,
            delegation_version = delegation_version + 1
        WHERE delegation_id = @DelegationId
          AND organization_id = @OrganizationId
          AND session_id = @SessionId
          AND revoked_at IS NULL;
        """;

    public static Task InsertInTransactionAsync(
        SessionScopedDelegationTarget target,
        ServiceDelegationIssue issue,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentNullException.ThrowIfNull(transaction);
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

    public async Task<bool> RevokeAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                RevokeSql,
                new { OrganizationId = organizationId, SessionId = sessionId, DelegationId = delegationId },
                cancellationToken: cancellationToken));
        return updated == 1;
    }

    public async Task<bool> NarrowAllowedActionAsync(
        Guid organizationId,
        Guid sessionId,
        Guid delegationId,
        string allowedAction,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                NarrowActionSql,
                new
                {
                    OrganizationId = organizationId,
                    SessionId = sessionId,
                    DelegationId = delegationId,
                    AllowedAction = allowedAction,
                },
                cancellationToken: cancellationToken));
        return updated == 1;
    }

    private static NpgsqlConnection RequireConnection(NpgsqlTransaction transaction) =>
        transaction.Connection
        ?? throw new InvalidOperationException("Delegation writes require an open transaction connection.");
}

public sealed record SessionScopedDelegationTarget(
    Guid OrganizationId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId);
