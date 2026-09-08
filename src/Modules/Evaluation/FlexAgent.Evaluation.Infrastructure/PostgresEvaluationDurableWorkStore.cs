using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationDurableWorkStore(PostgresConnectionAccessor connectionAccessor)
    : IEvaluationDurableWorkStore
{
    public async Task<EvaluationDurableWorkItem?> TryClaimAsync(
        Guid claimOwner,
        TimeSpan lease,
        int perOrganizationConcurrency,
        CancellationToken cancellationToken)
    {
        ValidateClaim(claimOwner, lease);
        if (perOrganizationConcurrency is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(perOrganizationConcurrency));
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            var candidateOrganizationId = await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(
                    ClaimOrganizationSql,
                    new { ClaimOwner = claimOwner },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (candidateOrganizationId is null)
            {
                await scope.CommitAsync(cancellationToken);
                return null;
            }

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    WITH reconciled AS (
                        UPDATE evaluation_durable_work AS work
                        SET
                            state = 'completed',
                            claim_owner = NULL,
                            claim_lease_until = NULL,
                            failure_category = NULL,
                            last_committed_at = clock_timestamp()
                        FROM evaluation_requests AS request
                        WHERE work.organization_id = @CandidateOrganizationId
                          AND work.organization_id = request.organization_id
                          AND work.request_id = request.request_id
                          AND work.state = 'claimed'
                          AND work.claim_lease_until < clock_timestamp()
                          AND request.state = 'completed'
                          AND EXISTS (
                                SELECT 1
                                FROM evaluations
                                WHERE organization_id = request.organization_id
                                  AND request_id = request.request_id)
                        RETURNING work.organization_id, work.request_id
                    )
                    UPDATE evaluation_invocation_attempts AS attempt
                    SET state = 'completed', finished_at = clock_timestamp(), failure_category = NULL
                    FROM reconciled
                    WHERE attempt.organization_id = reconciled.organization_id
                      AND attempt.request_id = reconciled.request_id
                      AND attempt.state IN ('running', 'validating', 'completing');
                    """,
                    new { CandidateOrganizationId = candidateOrganizationId.Value },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            var invocationAttemptId = Guid.CreateVersion7();
            var row = await scope.Connection.QuerySingleOrDefaultAsync<ClaimedRow>(
                new CommandDefinition(
                    ClaimSql,
                    new
                    {
                        ClaimOwner = claimOwner,
                        CandidateOrganizationId = candidateOrganizationId.Value,
                        InvocationAttemptId = invocationAttemptId,
                        LeaseSeconds = lease.TotalSeconds,
                        PerOrganizationConcurrency = perOrganizationConcurrency,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (row is null)
            {
                await scope.CommitAsync(cancellationToken);
                return null;
            }

            if (row.attempt_count > 1)
            {
                await scope.Connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE evaluation_invocation_attempts
                        SET
                            state = 'failed_retryable',
                            finished_at = clock_timestamp(),
                            failure_category = 'worker.lease_expired'
                        WHERE organization_id = @organization_id
                          AND request_id = @request_id
                          AND attempt_ordinal = @PreviousAttemptOrdinal
                          AND state IN ('running', 'validating', 'completing');
                        """,
                        new
                        {
                            row.organization_id,
                            row.request_id,
                            PreviousAttemptOrdinal = row.attempt_count - 1,
                        },
                        scope.Transaction,
                        cancellationToken: cancellationToken));
            }

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO evaluation_invocation_attempts (
                        organization_id, request_id, invocation_attempt_id,
                        activity_id, participant_id, attempt_id, session_id,
                        attempt_ordinal, state, started_at)
                    VALUES (
                        @organization_id, @request_id, @invocation_attempt_id,
                        @activity_id, @participant_id, @attempt_id, @session_id,
                        @attempt_count, 'running', clock_timestamp());
                    """,
                    row,
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            var requestUpdated = await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_requests
                    SET state = 'running', failure_category = NULL
                    WHERE organization_id = @organization_id
                      AND request_id = @request_id
                      AND state IN ('queued', 'running', 'failed_retryable');
                    """,
                    row,
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (requestUpdated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return null;
            }

            await scope.CommitAsync(cancellationToken);
            return row.ToDomain();
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<DateTimeOffset?> TryRenewAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        ValidateClaim(claimOwner, lease);

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            if (!await IsAuthorizedClaimAsync(scope, work, claimOwner, cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return null;
            }

            var renewed = await scope.Connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(
                new CommandDefinition(
                    """
                    UPDATE evaluation_durable_work
                    SET
                        claim_lease_until = clock_timestamp() + (@LeaseSeconds * INTERVAL '1 second'),
                        last_committed_at = clock_timestamp()
                    WHERE organization_id = @OrganizationId
                      AND work_id = @WorkId
                      AND request_id = @RequestId
                      AND state = 'claimed'
                      AND claim_owner = @ClaimOwner
                      AND claim_lease_until = @ClaimLeaseUntil
                      AND claim_lease_until >= clock_timestamp()
                    RETURNING claim_lease_until;
                    """,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.WorkId,
                        work.RequestId,
                        ClaimOwner = claimOwner,
                        work.ClaimLeaseUntil,
                        LeaseSeconds = lease.TotalSeconds,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            await scope.CommitAsync(cancellationToken);
            return renewed;
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public Task<bool> ReleaseForRetryAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        string failureCategory,
        CancellationToken cancellationToken) =>
        FinishClaimAsync(
            work,
            claimOwner,
            failureCategory,
            workState: "pending",
            requestState: EvaluationRequestStates.FailedRetryable,
            availableDelaySeconds: work.BackoffSeconds,
            cancellationToken);

    public Task<bool> MarkExhaustedAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        string failureCategory,
        CancellationToken cancellationToken) =>
        FinishClaimAsync(
            work,
            claimOwner,
            failureCategory,
            workState: "failed",
            requestState: EvaluationRequestStates.FailedReviewRequired,
            availableDelaySeconds: 0,
            cancellationToken);

    public async Task<bool> MarkCompletedAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (claimOwner == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(claimOwner));
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            if (!await IsAuthorizedClaimAsync(scope, work, claimOwner, cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var updated = await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_durable_work
                    SET
                        state = 'completed',
                        claim_owner = NULL,
                        claim_lease_until = NULL,
                        failure_category = NULL,
                        last_committed_at = clock_timestamp()
                    WHERE organization_id = @OrganizationId
                      AND work_id = @WorkId
                      AND request_id = @RequestId
                      AND state = 'claimed'
                      AND claim_owner = @ClaimOwner
                      AND claim_lease_until = @ClaimLeaseUntil
                      AND EXISTS (
                            SELECT 1
                            FROM evaluations
                            WHERE organization_id = evaluation_durable_work.organization_id
                              AND request_id = evaluation_durable_work.request_id);
                    """,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.WorkId,
                        work.RequestId,
                        ClaimOwner = claimOwner,
                        work.ClaimLeaseUntil,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (updated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var attemptUpdated = await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_invocation_attempts
                    SET state = 'completed', finished_at = clock_timestamp(), failure_category = NULL
                    WHERE organization_id = @OrganizationId
                      AND request_id = @RequestId
                      AND invocation_attempt_id = @InvocationAttemptId
                      AND state IN ('running', 'validating', 'completing');
                    """,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.RequestId,
                        work.InvocationAttemptId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (attemptUpdated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            await scope.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<bool> FinishClaimAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        string failureCategory,
        string workState,
        string requestState,
        int availableDelaySeconds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (claimOwner == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(claimOwner));
        }

        if (!EvaluationIdentity.IsStableId(failureCategory))
        {
            throw new ArgumentException("Failure category must be a stable non-content identifier.", nameof(failureCategory));
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            cancellationToken);
        try
        {
            if (!await IsAuthorizedClaimAsync(scope, work, claimOwner, cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var updated = await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_durable_work
                    SET
                        state = @WorkState,
                        available_at = clock_timestamp() + (@AvailableDelaySeconds * INTERVAL '1 second'),
                        claim_owner = NULL,
                        claim_lease_until = NULL,
                        failure_category = @FailureCategory,
                        last_committed_at = clock_timestamp()
                    WHERE organization_id = @OrganizationId
                      AND work_id = @WorkId
                      AND request_id = @RequestId
                      AND state = 'claimed'
                      AND claim_owner = @ClaimOwner
                      AND claim_lease_until = @ClaimLeaseUntil
                      AND (
                            (@RequireExhausted AND attempt_count >= max_attempts)
                            OR (NOT @RequireExhausted AND attempt_count < max_attempts)
                          );
                    """,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.WorkId,
                        work.RequestId,
                        ClaimOwner = claimOwner,
                        work.ClaimLeaseUntil,
                        WorkState = workState,
                        AvailableDelaySeconds = availableDelaySeconds,
                        FailureCategory = failureCategory,
                        RequireExhausted = workState == "failed",
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (updated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var attemptUpdated = await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_invocation_attempts
                    SET
                        state = @AttemptState,
                        finished_at = clock_timestamp(),
                        failure_category = @FailureCategory
                    WHERE organization_id = @OrganizationId
                      AND request_id = @RequestId
                      AND invocation_attempt_id = @InvocationAttemptId
                      AND state IN ('running', 'validating', 'completing');
                    """,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.RequestId,
                        work.InvocationAttemptId,
                        AttemptState = workState == "failed"
                            ? EvaluationRequestStates.FailedReviewRequired
                            : EvaluationRequestStates.FailedRetryable,
                        FailureCategory = failureCategory,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (attemptUpdated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var requestUpdated = await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE evaluation_requests
                    SET state = @RequestState, failure_category = @FailureCategory
                    WHERE organization_id = @OrganizationId
                      AND request_id = @RequestId
                      AND state IN ('running', 'validating', 'completing');
                    """,
                    new
                    {
                        work.Ownership.OrganizationId,
                        work.RequestId,
                        RequestState = requestState,
                        FailureCategory = failureCategory,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (requestUpdated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            await scope.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void ValidateClaim(Guid claimOwner, TimeSpan lease)
    {
        if (claimOwner == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(claimOwner));
        }

        if (lease <= TimeSpan.Zero || lease > TimeSpan.FromHours(1))
        {
            throw new ArgumentOutOfRangeException(nameof(lease));
        }
    }

    private static async Task<bool> IsAuthorizedClaimAsync(
        PostgresTransactionScope scope,
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        CancellationToken cancellationToken)
    {
        var delegationId = await scope.Connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                SELECT delegation.delegation_id
                FROM evaluation_durable_work AS durable
                INNER JOIN evaluation_requests AS request
                  ON request.organization_id = durable.organization_id
                 AND request.request_id = durable.request_id
                INNER JOIN service_delegations AS delegation
                  ON delegation.delegation_id = request.delegation_id
                 AND delegation.organization_id = durable.organization_id
                 AND delegation.activity_id = durable.activity_id
                 AND delegation.participant_id = durable.participant_id
                 AND delegation.attempt_id = durable.attempt_id
                 AND delegation.session_id = durable.session_id
                WHERE durable.organization_id = @OrganizationId
                  AND durable.work_id = @WorkId
                  AND durable.request_id = @RequestId
                  AND durable.state = 'claimed'
                  AND durable.claim_owner = @ClaimOwner
                  AND durable.claim_lease_until = @ClaimLeaseUntil
                  AND durable.claim_lease_until >= clock_timestamp()
                  AND delegation.service_actor_id = @ClaimOwner
                  AND delegation.allowed_action = 'evaluation.execute'
                  AND delegation.revoked_at IS NULL
                  AND delegation.effective_at <= clock_timestamp()
                  AND (delegation.expires_at IS NULL OR delegation.expires_at > clock_timestamp())
                FOR UPDATE OF durable, delegation;
                """,
                new
                {
                    work.Ownership.OrganizationId,
                    work.WorkId,
                    work.RequestId,
                    ClaimOwner = claimOwner,
                    work.ClaimLeaseUntil,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));
        return delegationId is not null;
    }

    private const string ClaimOrganizationSql = """
        SELECT organization.id
        FROM evaluation_durable_work AS work
        INNER JOIN organizations AS organization
          ON organization.id = work.organization_id
        INNER JOIN evaluation_requests AS request
          ON request.organization_id = work.organization_id
         AND request.request_id = work.request_id
        INNER JOIN service_delegations AS delegation
          ON delegation.delegation_id = request.delegation_id
         AND delegation.organization_id = work.organization_id
         AND delegation.activity_id = work.activity_id
         AND delegation.participant_id = work.participant_id
         AND delegation.attempt_id = work.attempt_id
         AND delegation.session_id = work.session_id
         AND delegation.service_actor_id = @ClaimOwner
         AND delegation.allowed_action = 'evaluation.execute'
         AND delegation.revoked_at IS NULL
         AND delegation.effective_at <= clock_timestamp()
         AND (delegation.expires_at IS NULL OR delegation.expires_at > clock_timestamp())
        LEFT JOIN evaluation_work_claim_partitions AS served
          ON served.organization_id = work.organization_id
         AND served.activity_id = work.activity_id
        WHERE (
                (
                    work.attempt_count < work.max_attempts
                    AND (
                        (work.state = 'pending' AND work.available_at <= clock_timestamp())
                        OR (
                            work.state = 'claimed'
                            AND work.claim_lease_until IS NOT NULL
                            AND work.claim_lease_until < clock_timestamp())
                    )
                )
                OR (
                    work.state = 'claimed'
                    AND work.claim_lease_until < clock_timestamp()
                    AND request.state = 'completed'
                    AND EXISTS (
                        SELECT 1
                        FROM evaluations
                        WHERE organization_id = request.organization_id
                          AND request_id = request.request_id)
                )
              )
        ORDER BY COALESCE(served.last_claimed_at, TIMESTAMPTZ '-infinity'),
                 work.available_at,
                 work.work_id
        FOR UPDATE OF organization SKIP LOCKED
        LIMIT 1;
        """;

    private const string ClaimSql = """
        WITH candidate AS MATERIALIZED (
            SELECT work.organization_id, work.activity_id, work.participant_id,
                   work.attempt_id, work.session_id, work.work_id, work.request_id,
                   request.frozen_input_digest, request.delegation_id,
                   work.attempt_count, work.max_attempts, work.attempt_timeout_seconds,
                   work.backoff_seconds
            FROM evaluation_durable_work AS work
            INNER JOIN evaluation_requests AS request
              ON request.organization_id = work.organization_id
             AND request.request_id = work.request_id
            INNER JOIN service_delegations AS delegation
              ON delegation.delegation_id = request.delegation_id
             AND delegation.organization_id = work.organization_id
             AND delegation.activity_id = work.activity_id
             AND delegation.participant_id = work.participant_id
             AND delegation.attempt_id = work.attempt_id
             AND delegation.session_id = work.session_id
             AND delegation.service_actor_id = @ClaimOwner
             AND delegation.allowed_action = 'evaluation.execute'
             AND delegation.revoked_at IS NULL
             AND delegation.effective_at <= clock_timestamp()
             AND (delegation.expires_at IS NULL OR delegation.expires_at > clock_timestamp())
            LEFT JOIN evaluation_work_claim_partitions AS served
              ON served.organization_id = work.organization_id
             AND served.activity_id = work.activity_id
            WHERE work.attempt_count < work.max_attempts
              AND work.organization_id = @CandidateOrganizationId
              AND (
                    (work.state = 'pending' AND work.available_at <= clock_timestamp())
                    OR (
                        work.state = 'claimed'
                        AND work.claim_lease_until IS NOT NULL
                        AND work.claim_lease_until < clock_timestamp())
                  )
              AND (
                    SELECT COUNT(*)
                    FROM evaluation_durable_work AS active
                    WHERE active.organization_id = work.organization_id
                      AND active.state = 'claimed'
                      AND active.claim_lease_until >= clock_timestamp()
                  ) < @PerOrganizationConcurrency
            ORDER BY COALESCE(served.last_claimed_at, TIMESTAMPTZ '-infinity'),
                     work.available_at,
                     work.work_id
            FOR UPDATE OF work, delegation SKIP LOCKED
            LIMIT 1
        )
        UPDATE evaluation_durable_work AS work
        SET
            state = 'claimed',
            attempt_count = work.attempt_count + 1,
            claim_owner = @ClaimOwner,
            claim_lease_until = clock_timestamp() + (@LeaseSeconds * INTERVAL '1 second'),
            failure_category = NULL,
            last_committed_at = clock_timestamp()
        FROM candidate
        WHERE work.organization_id = candidate.organization_id
          AND work.work_id = candidate.work_id
        RETURNING
            work.organization_id,
            work.activity_id,
            work.participant_id,
            work.attempt_id,
            work.session_id,
            work.work_id,
            work.request_id,
            @InvocationAttemptId AS invocation_attempt_id,
            candidate.frozen_input_digest,
            candidate.delegation_id,
            work.attempt_count,
            work.max_attempts,
            work.attempt_timeout_seconds,
            work.backoff_seconds,
            work.claim_lease_until;
        """;

    private sealed record ClaimedRow(
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        Guid work_id,
        Guid request_id,
        Guid invocation_attempt_id,
        string frozen_input_digest,
        Guid delegation_id,
        int attempt_count,
        int max_attempts,
        int attempt_timeout_seconds,
        int backoff_seconds,
        DateTimeOffset claim_lease_until)
    {
        public EvaluationDurableWorkItem ToDomain() => new(
            work_id,
            request_id,
            invocation_attempt_id,
            new EvaluationOwnership(
                organization_id,
                activity_id,
                participant_id,
                attempt_id,
                session_id),
            frozen_input_digest,
            delegation_id,
            attempt_count,
            max_attempts,
            attempt_timeout_seconds,
            backoff_seconds,
            claim_lease_until);
    }
}
