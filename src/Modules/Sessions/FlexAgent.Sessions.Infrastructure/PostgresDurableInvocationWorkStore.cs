using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresDurableInvocationWorkStore(PostgresConnectionAccessor connectionAccessor)
    : IDurableInvocationWorkStore
{
    private const string ClaimSql = """
        WITH candidate AS (
            SELECT organization_id, activity_id, participant_id, attempt_id, session_id, work_id
            FROM session_durable_work
            WHERE work_type = @WorkType
              AND (
                    state = @Pending
                    OR (
                        state = @Claimed
                        AND claim_lease_until IS NOT NULL
                        AND claim_lease_until < clock_timestamp()
                    )
                  )
            ORDER BY last_committed_at ASC, work_id ASC
            FOR UPDATE SKIP LOCKED
            LIMIT 1
        )
        UPDATE session_durable_work AS work
        SET
            state = @Claimed,
            claim_lease_until = clock_timestamp() + (@LeaseSeconds * INTERVAL '1 second')
        FROM candidate
        WHERE work.organization_id = candidate.organization_id
          AND work.session_id = candidate.session_id
          AND work.work_id = candidate.work_id
        RETURNING
            work.work_id,
            work.organization_id,
            work.activity_id,
            work.participant_id,
            work.attempt_id,
            work.session_id,
            work.business_key,
            work.state,
            work.claim_lease_until;
        """;

    private const string ReleaseSql = """
        UPDATE session_durable_work
        SET
            state = @Pending,
            claim_lease_until = NULL
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND work_id = @WorkId
          AND state = @Claimed
          AND claim_lease_until IS NOT DISTINCT FROM @ClaimLeaseUntil;
        """;

    private const string CompleteSql = """
        UPDATE session_durable_work
        SET
            state = @Completed,
            claim_lease_until = NULL
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND work_id = @WorkId
          AND state = @Claimed
          AND claim_lease_until IS NOT DISTINCT FROM @ClaimLeaseUntil;
        """;

    public async Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        if (lease <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lease));
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var row = await scope.Connection.QuerySingleOrDefaultAsync<ClaimedWorkRow>(
                new CommandDefinition(
                    ClaimSql,
                    new
                    {
                        WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                        Pending = DurableSessionWorkStates.Pending,
                        Claimed = DurableSessionWorkStates.Claimed,
                        LeaseSeconds = lease.TotalSeconds,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (row is null)
            {
                await scope.CommitAsync(cancellationToken);
                return null;
            }

            await scope.CommitAsync(cancellationToken);
            return new DurableInvocationWorkItem(
                row.work_id,
                new SessionOwnership(
                    row.organization_id,
                    row.activity_id,
                    row.participant_id,
                    row.attempt_id,
                    row.session_id),
                row.business_key,
                row.state,
                ToUtc(row.claim_lease_until));
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task ReleaseToPendingAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken) =>
        UpdateOwnedClaimAsync(ReleaseSql, work, cancellationToken);

    public Task MarkCompletedAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken) =>
        UpdateOwnedClaimAsync(CompleteSql, work, cancellationToken);

    private async Task UpdateOwnedClaimAsync(
        string sql,
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.Ownership.SessionId,
                        work.WorkId,
                        Claimed = DurableSessionWorkStates.Claimed,
                        Pending = DurableSessionWorkStates.Pending,
                        Completed = DurableSessionWorkStates.Completed,
                        ClaimLeaseUntil = work.ClaimLeaseUntil?.UtcDateTime,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            await scope.CommitAsync(cancellationToken);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private sealed record ClaimedWorkRow(
        Guid work_id,
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string business_key,
        string state,
        DateTime claim_lease_until);
}
