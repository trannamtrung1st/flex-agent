using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresDurableInvocationWorkStore(
    PostgresConnectionAccessor connectionAccessor,
    TrustedRuntimeActor? serviceActor = null,
    ICommitAuthorizationKernel? authorizationKernel = null,
    IAuthenticatedWorkloadContextSource? workloadIdentity = null)
    : IDurableInvocationWorkStore
{
    public const string ClaimableIndexName = "ix_session_durable_work_claimable";

    internal const string ClaimCandidateSql = """
        SELECT work.organization_id, work.activity_id, work.participant_id, work.attempt_id,
               work.session_id, work.work_id, work.invocation_execute_delegation_id
        FROM session_durable_work AS work
        INNER JOIN service_delegations AS delegation
          ON delegation.delegation_id = work.invocation_execute_delegation_id
         AND delegation.organization_id = work.organization_id
         AND delegation.activity_id = work.activity_id
         AND delegation.participant_id = work.participant_id
         AND delegation.attempt_id = work.attempt_id
         AND delegation.session_id = work.session_id
         AND delegation.service_actor_id = @ServiceActorId
         AND delegation.allowed_action = @AllowedAction
         AND delegation.revoked_at IS NULL
         AND delegation.effective_at <= clock_timestamp()
         AND (delegation.expires_at IS NULL OR delegation.expires_at > clock_timestamp())
        LEFT JOIN session_durable_work_claim_partitions AS served
          ON served.organization_id = work.organization_id
         AND served.activity_id = work.activity_id
        WHERE work.work_type = @WorkType
          AND (
                work.state = @Pending
                OR (
                    work.state = @Claimed
                    AND work.claim_lease_until IS NOT NULL
                    AND work.claim_lease_until < clock_timestamp()
                )
              )
          AND NOT EXISTS (
                SELECT 1
                FROM session_durable_work AS older
                INNER JOIN service_delegations AS older_delegation
                  ON older_delegation.delegation_id = older.invocation_execute_delegation_id
                 AND older_delegation.organization_id = older.organization_id
                 AND older_delegation.activity_id = older.activity_id
                 AND older_delegation.participant_id = older.participant_id
                 AND older_delegation.attempt_id = older.attempt_id
                 AND older_delegation.session_id = older.session_id
                 AND older_delegation.service_actor_id = @ServiceActorId
                 AND older_delegation.allowed_action = @AllowedAction
                 AND older_delegation.revoked_at IS NULL
                 AND older_delegation.effective_at <= clock_timestamp()
                 AND (older_delegation.expires_at IS NULL OR older_delegation.expires_at > clock_timestamp())
                WHERE older.work_type = work.work_type
                  AND older.organization_id = work.organization_id
                  AND older.activity_id = work.activity_id
                  AND (
                        older.state = @Pending
                        OR (
                            older.state = @Claimed
                            AND older.claim_lease_until IS NOT NULL
                            AND older.claim_lease_until < clock_timestamp()
                        )
                      )
                  AND (older.last_committed_at, older.work_id) < (work.last_committed_at, work.work_id)
          )
        ORDER BY COALESCE(served.last_claimed_at, TIMESTAMPTZ '-infinity') ASC,
                 work.last_committed_at ASC,
                 work.work_id ASC
        FOR UPDATE OF work SKIP LOCKED
        LIMIT 1
        """;

    private const string ClaimSql = """
        WITH candidate AS MATERIALIZED (
        """ + ClaimCandidateSql + """
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
            work.claim_lease_until,
            work.invocation_execute_delegation_id;
        """;

    private const string BacklogSql = """
        SELECT COUNT(*)::INT AS claimable_count,
               COUNT(DISTINCT (organization_id, activity_id))::INT AS partition_count
        FROM session_durable_work
        WHERE work_type = @WorkType
          AND (
                state = @Pending
                OR (
                    state = @Claimed
                    AND claim_lease_until IS NOT NULL
                    AND claim_lease_until < clock_timestamp()
                )
              );
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

    private const string RenewLeaseSql = """
        UPDATE session_durable_work
        SET
            claim_lease_until = clock_timestamp() + (@LeaseSeconds * INTERVAL '1 second')
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND work_id = @WorkId
          AND state = @Claimed
          AND claim_lease_until IS NOT DISTINCT FROM @ClaimLeaseUntil
        RETURNING claim_lease_until;
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
            var actor = serviceActor;
            if (actor is null || actor.ActorId == Guid.Empty)
            {
                await scope.RollbackAsync(cancellationToken);
                return null;
            }

            if (!await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                workloadIdentity,
                actor,
                cancellationToken,
                scope.Transaction))
            {
                await scope.RollbackAsync(cancellationToken);
                return null;
            }

            var row = await scope.Connection.QuerySingleOrDefaultAsync<ClaimedWorkRow>(
                new CommandDefinition(
                    ClaimSql,
                    new
                    {
                        WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                        Pending = DurableSessionWorkStates.Pending,
                        Claimed = DurableSessionWorkStates.Claimed,
                        LeaseSeconds = lease.TotalSeconds,
                        ServiceActorId = actor.ActorId,
                        AllowedAction = AuthorizationActions.ExecuteSessionInvocation,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (row is null)
            {
                await scope.CommitAsync(cancellationToken);
                return null;
            }

            if (authorizationKernel is not null)
            {
                var ownership = new SessionOwnership(
                    row.organization_id,
                    row.activity_id,
                    row.participant_id,
                    row.attempt_id,
                    row.session_id);
                var decision = await authorizationKernel.AuthorizeInTransactionAsync(
                    new AuthorizationRequest(
                        new TrustedActor(actor.ActorId, actor.ActorType),
                        new OrganizationScope(ownership.OrganizationId),
                        AuthorizationActions.ExecuteSessionInvocation,
                        new ResourceScope(
                            new OrganizationScope(ownership.OrganizationId),
                            AuthorizationResourceTypes.Session,
                            ownership.SessionId),
                        "worker.session_runtime",
                        Guid.NewGuid(),
                        row.invocation_execute_delegation_id,
                        ownership.ActivityId,
                        ownership.ParticipantId,
                        ownership.AttemptId),
                    scope.Transaction,
                    cancellationToken);
                if (!decision.IsPermitted)
                {
                    await scope.RollbackAsync(CancellationToken.None);
                    return null;
                }
            }

            if (!await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                workloadIdentity,
                actor,
                cancellationToken,
                scope.Transaction))
            {
                await scope.RollbackAsync(CancellationToken.None);
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
                ToUtc(row.claim_lease_until),
                row.invocation_execute_delegation_id);
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
        UpdateOwnedClaimAsync(ReleaseSql, work, requireAuthorizedCommit: false, cancellationToken);

    public Task MarkCompletedAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken) =>
        UpdateOwnedClaimAsync(CompleteSql, work, requireAuthorizedCommit: true, cancellationToken);

    public async Task<DateTimeOffset?> TryRenewClaimLeaseAsync(
        DurableInvocationWorkItem work,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (lease <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lease));
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var renewed = await scope.Connection.QuerySingleOrDefaultAsync<DateTime?>(
                new CommandDefinition(
                    RenewLeaseSql,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.Ownership.SessionId,
                        work.WorkId,
                        Claimed = DurableSessionWorkStates.Claimed,
                        ClaimLeaseUntil = work.ClaimLeaseUntil?.UtcDateTime,
                        LeaseSeconds = lease.TotalSeconds,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (renewed is not null && serviceActor is not null)
            {
                if (authorizationKernel is not null)
                {
                    var commitDecision = await SessionInvocationExecuteCommitAuthorization.ReauthorizeAsync(
                        authorizationKernel,
                        serviceActor,
                        work.Ownership,
                        Guid.NewGuid(),
                        "worker.session_runtime",
                        scope.Transaction,
                        cancellationToken,
                        workloadIdentity);
                    if (!commitDecision.IsPermitted)
                    {
                        await scope.RollbackAsync(CancellationToken.None);
                        return null;
                    }
                }
                else if (!await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                    workloadIdentity,
                    serviceActor,
                    cancellationToken,
                    scope.Transaction))
                {
                    await scope.RollbackAsync(CancellationToken.None);
                    return null;
                }
            }

            await scope.CommitAsync(cancellationToken);
            return renewed is null ? null : ToUtc(renewed);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(CancellationToken cancellationToken)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var row = await scope.Connection.QuerySingleAsync<BacklogRow>(
                new CommandDefinition(
                    BacklogSql,
                    new
                    {
                        WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                        Pending = DurableSessionWorkStates.Pending,
                        Claimed = DurableSessionWorkStates.Claimed,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            await scope.CommitAsync(cancellationToken);
            return new DurableWorkBacklogSnapshot(row.claimable_count, row.partition_count);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task UpdateOwnedClaimAsync(
        string sql,
        DurableInvocationWorkItem work,
        bool requireAuthorizedCommit,
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
            if (authorizationKernel is not null && serviceActor is not null)
            {
                var commitDecision = await SessionInvocationExecuteCommitAuthorization.ReauthorizeAsync(
                    authorizationKernel,
                    serviceActor,
                    work.Ownership,
                    Guid.NewGuid(),
                    "worker.session_runtime",
                    scope.Transaction,
                    cancellationToken,
                    workloadIdentity);
                if (!commitDecision.IsPermitted)
                {
                    if (requireAuthorizedCommit)
                    {
                        throw new AuthorizationDeniedException(commitDecision.ReasonCode);
                    }

                    await scope.RollbackAsync(CancellationToken.None);
                    return;
                }
            }

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

    private sealed record BacklogRow(int claimable_count, int partition_count);

    private sealed record ClaimedWorkRow(
        Guid work_id,
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        string business_key,
        string state,
        DateTime claim_lease_until,
        Guid? invocation_execute_delegation_id);
}
