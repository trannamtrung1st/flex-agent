using System.Data;
using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Microsoft.Extensions.Logging;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresHostedSessionExpirySweep(
    PostgresConnectionAccessor connectionAccessor,
    ITrustedSessionBindingSource bindings,
    PostgresSessionRuntimeRepository runtimeRepository,
    IHostedSessionFrozenTimingSource frozenTiming,
    PostgresSessionLifecycleCoordinator lifecycle,
    HostedSessionExpirySettings settings,
    ILogger<PostgresHostedSessionExpirySweep>? logger = null,
    IAuthenticatedWorkloadContextSource? workloadIdentity = null) : IHostedSessionExpirySweep
{
    private const int BatchSize = 32;

    internal Func<Task>? BeforeWarningCommitAsync { get; set; }

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await IssueDueWarningsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger?.LogError(
                exception,
                "Hosted Session warning sweep failed; expiry processing will continue.");
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var hardEndDue = (await connection.QueryAsync<DueRow>(
            new CommandDefinition(
                """
                SELECT runtime.organization_id AS OrganizationId,
                       runtime.session_id AS SessionId
                FROM session_runtimes AS runtime
                INNER JOIN session_frozen_timing AS timing
                    ON timing.organization_id = runtime.organization_id
                   AND timing.session_id = runtime.session_id
                WHERE runtime.lifecycle_state IN ('active', 'paused')
                  AND timing.document->>'reconstruction' IN ('timed', 'unbounded')
                  AND timing.document->>'hard_end_at_utc' IS NOT NULL
                  AND (timing.document->>'hard_end_at_utc')::timestamptz <= clock_timestamp()
                ORDER BY (timing.document->>'hard_end_at_utc')::timestamptz
                LIMIT @BatchSize
                """,
                new { BatchSize },
                cancellationToken: cancellationToken))).ToArray();
        var activeBudgetDue = (await connection.QueryAsync<DueRow>(
            new CommandDefinition(
                """
                SELECT runtime.organization_id AS OrganizationId,
                       runtime.session_id AS SessionId
                FROM session_runtimes AS runtime
                INNER JOIN session_frozen_timing AS timing
                    ON timing.organization_id = runtime.organization_id
                   AND timing.session_id = runtime.session_id
                LEFT JOIN LATERAL (
                    SELECT COALESCE(SUM(
                        EXTRACT(EPOCH FROM (interval.ended_at - interval.started_at)))::int,
                        0) AS accumulated_seconds
                    FROM session_pause_intervals AS interval
                    WHERE interval.organization_id = runtime.organization_id
                      AND interval.session_id = runtime.session_id
                      AND interval.ended_at IS NOT NULL
                ) AS pause_totals ON TRUE
                WHERE runtime.lifecycle_state = 'active'
                  AND timing.document->>'reconstruction' = 'timed'
                  AND jsonb_typeof(timing.document->'warnings') = 'array'
                  AND jsonb_array_length(timing.document->'warnings') > 0
                  AND runtime.created_at
                      + make_interval(secs => GREATEST(COALESCE((timing.document->>'budget_seconds')::int, 0), 0))
                      + make_interval(secs => GREATEST(COALESCE(pause_totals.accumulated_seconds, 0), 0))
                      <= clock_timestamp()
                ORDER BY runtime.created_at
                    + make_interval(secs => GREATEST(COALESCE((timing.document->>'budget_seconds')::int, 0), 0))
                    + make_interval(secs => GREATEST(COALESCE(pause_totals.accumulated_seconds, 0), 0))
                LIMIT @BatchSize
                """,
                new { BatchSize },
                cancellationToken: cancellationToken))).ToArray();

        var seen = new HashSet<(Guid OrganizationId, Guid SessionId)>();
        var expired = 0;
        foreach (var candidate in hardEndDue.Concat(activeBudgetDue))
        {
            var key = (candidate.OrganizationId, candidate.SessionId);
            if (!seen.Add(key))
            {
                continue;
            }

            if (await TryExpireAsync(candidate.OrganizationId, candidate.SessionId, cancellationToken))
            {
                expired++;
            }
        }

        return expired;
    }

    private async Task IssueDueWarningsAsync(CancellationToken cancellationToken)
    {
        List<DueWarningRow> candidates;
        await using (var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken))
        {
            candidates = (await connection.QueryAsync<DueWarningRow>(
                new CommandDefinition(
                """
                WITH pause_totals AS (
                    SELECT organization_id,
                           session_id,
                           LEAST(
                               COALESCE(SUM(GREATEST(FLOOR(EXTRACT(EPOCH FROM (ended_at - started_at))), 0)), 0),
                               2147483647)::int AS accumulated_seconds
                    FROM session_pause_intervals
                    WHERE ended_at IS NOT NULL
                    GROUP BY organization_id, session_id
                ),
                open_pauses AS (
                    SELECT organization_id,
                           session_id,
                           MAX(started_at) AS started_at
                    FROM session_pause_intervals
                    WHERE ended_at IS NULL
                    GROUP BY organization_id, session_id
                ),
                warning_candidates AS (
                    SELECT runtime.organization_id AS OrganizationId,
                           runtime.activity_id AS ActivityId,
                           runtime.participant_id AS ParticipantId,
                           runtime.attempt_id AS AttemptId,
                           runtime.session_id AS SessionId,
                           warning->>'code' AS WarningThresholdId,
                           warning->>'code' AS WarningCode,
                           (warning->>'remaining_seconds')::int AS RemainingSecondsThreshold,
                           CASE
                               WHEN runtime.lifecycle_state = 'active'
                                   AND timing.document->>'hard_end_at_utc' IS NOT NULL
                                   THEN LEAST(
                                       runtime.created_at
                                           + make_interval(secs =>
                                               (timing.document->>'budget_seconds')::int
                                               - (warning->>'remaining_seconds')::int
                                               + COALESCE(pause_totals.accumulated_seconds, 0)),
                                       (timing.document->>'hard_end_at_utc')::timestamptz
                                           - make_interval(secs => (warning->>'remaining_seconds')::int))
                               WHEN runtime.lifecycle_state = 'active'
                                   THEN runtime.created_at
                                       + make_interval(secs =>
                                           (timing.document->>'budget_seconds')::int
                                           - (warning->>'remaining_seconds')::int
                                           + COALESCE(pause_totals.accumulated_seconds, 0))
                               WHEN runtime.lifecycle_state = 'paused'
                                   AND runtime.created_at
                                       + make_interval(secs =>
                                           (timing.document->>'budget_seconds')::int
                                           - (warning->>'remaining_seconds')::int
                                           + COALESCE(pause_totals.accumulated_seconds, 0))
                                       <= open_pauses.started_at
                                   AND timing.document->>'hard_end_at_utc' IS NOT NULL
                                   THEN LEAST(
                                       runtime.created_at
                                           + make_interval(secs =>
                                               (timing.document->>'budget_seconds')::int
                                               - (warning->>'remaining_seconds')::int
                                               + COALESCE(pause_totals.accumulated_seconds, 0)),
                                       (timing.document->>'hard_end_at_utc')::timestamptz
                                           - make_interval(secs => (warning->>'remaining_seconds')::int))
                               WHEN runtime.lifecycle_state = 'paused'
                                   AND runtime.created_at
                                       + make_interval(secs =>
                                           (timing.document->>'budget_seconds')::int
                                           - (warning->>'remaining_seconds')::int
                                           + COALESCE(pause_totals.accumulated_seconds, 0))
                                       <= open_pauses.started_at
                                   THEN runtime.created_at
                                       + make_interval(secs =>
                                           (timing.document->>'budget_seconds')::int
                                           - (warning->>'remaining_seconds')::int
                                           + COALESCE(pause_totals.accumulated_seconds, 0))
                               WHEN timing.document->>'hard_end_at_utc' IS NOT NULL
                                   THEN (timing.document->>'hard_end_at_utc')::timestamptz
                                       - make_interval(secs => (warning->>'remaining_seconds')::int)
                               ELSE NULL
                           END AS DueAt
                    FROM session_runtimes AS runtime
                    INNER JOIN session_frozen_timing AS timing
                        ON timing.organization_id = runtime.organization_id
                       AND timing.session_id = runtime.session_id
                    CROSS JOIN LATERAL jsonb_array_elements(timing.document->'warnings') AS warning
                    LEFT JOIN pause_totals
                        ON pause_totals.organization_id = runtime.organization_id
                       AND pause_totals.session_id = runtime.session_id
                    LEFT JOIN open_pauses
                        ON open_pauses.organization_id = runtime.organization_id
                       AND open_pauses.session_id = runtime.session_id
                    LEFT JOIN session_warning_occurrences AS occurrence
                        ON occurrence.organization_id = runtime.organization_id
                       AND occurrence.session_id = runtime.session_id
                       AND occurrence.warning_threshold_id = warning->>'code'
                    WHERE runtime.lifecycle_state IN ('active', 'paused')
                      AND timing.document->>'reconstruction' = 'timed'
                      AND (timing.document->>'budget_seconds')::int > 0
                      AND jsonb_typeof(timing.document->'warnings') = 'array'
                      AND warning->>'code' IN ('approaching', 'imminent')
                      AND (warning->>'remaining_seconds')::int > 0
                      AND occurrence.warning_threshold_id IS NULL
                )
                SELECT *
                FROM warning_candidates
                WHERE DueAt IS NOT NULL
                  AND DueAt <= clock_timestamp()
                ORDER BY DueAt, OrganizationId, SessionId, WarningThresholdId
                LIMIT @BatchSize
                """,
                new { BatchSize },
                cancellationToken: cancellationToken))).AsList();
        }

        foreach (var candidate in candidates)
        {
            try
            {
                await TryIssueWarningAsync(candidate, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger?.LogError(
                    exception,
                    "Hosted Session warning issuance failed for one candidate; remaining candidates will continue.");
            }
        }
    }

    private async Task<bool> TryIssueWarningAsync(
        DueWarningRow candidate,
        CancellationToken cancellationToken)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            if (!await AuthenticatedWorkloadGuard.IsCurrentForActorAsync(
                    workloadIdentity,
                    settings.ServiceActor,
                    cancellationToken,
                    scope.Transaction))
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var row = await scope.Transaction.Connection!.QuerySingleOrDefaultAsync<WarningLockRow>(
                new CommandDefinition(
                    """
                    SELECT runtime.lifecycle_state AS LifecycleState,
                           runtime.created_at AS StartedAt,
                           runtime.last_committed_at AS LastCommittedAt,
                           runtime.session_version AS SessionVersion,
                           runtime.session_sequence AS SessionSequence,
                           timing.document::text AS TimingDocument,
                           COALESCE((
                               SELECT LEAST(
                                   SUM(GREATEST(FLOOR(EXTRACT(EPOCH FROM (interval.ended_at - interval.started_at))), 0)),
                                   2147483647)::int
                               FROM session_pause_intervals AS interval
                               WHERE interval.organization_id = runtime.organization_id
                                 AND interval.session_id = runtime.session_id
                                 AND interval.ended_at IS NOT NULL
                           ), 0) AS AccumulatedPausedSeconds,
                           (
                               SELECT interval.started_at
                               FROM session_pause_intervals AS interval
                               WHERE interval.organization_id = runtime.organization_id
                                 AND interval.session_id = runtime.session_id
                                 AND interval.ended_at IS NULL
                               ORDER BY interval.started_at DESC
                               LIMIT 1
                           ) AS OpenPauseStartedAt,
                           clock_timestamp() AS AuthoritativeUtc
                    FROM session_runtimes AS runtime
                    INNER JOIN session_frozen_timing AS timing
                        ON timing.organization_id = runtime.organization_id
                       AND timing.session_id = runtime.session_id
                    WHERE runtime.organization_id = @OrganizationId
                      AND runtime.session_id = @SessionId
                    FOR UPDATE OF runtime
                    """,
                    candidate,
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (row is null
                || row.LifecycleState is not ("active" or "paused")
                || await WarningAlreadyIssuedAsync(scope.Transaction, candidate, cancellationToken))
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var policy = HostedSessionFrozenTiming.FromDocumentJson(row.TimingDocument);
            var threshold = policy.WarningSchedule.SingleOrDefault(item =>
                string.Equals(item.Code, candidate.WarningThresholdId, StringComparison.Ordinal));
            if (threshold is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var lifecycle = row.LifecycleState == "paused"
                ? SessionLifecycleState.Paused
                : SessionLifecycleState.Active;
            var timing = HostedSessionTiming.Project(
                lifecycle,
                row.StartedAt,
                row.LastCommittedAt,
                row.AuthoritativeUtc,
                policy,
                row.AccumulatedPausedSeconds,
                row.OpenPauseStartedAt);
            if (timing.RemainingSeconds is not { } remaining
                || remaining > threshold.RemainingSecondsThreshold)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var dueAt = ResolveWarningDueAt(
                lifecycle,
                row.StartedAt,
                row.AccumulatedPausedSeconds,
                row.OpenPauseStartedAt,
                policy,
                threshold.RemainingSecondsThreshold);
            if (dueAt is null || dueAt > row.AuthoritativeUtc)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            if (BeforeWarningCommitAsync is not null)
            {
                await BeforeWarningCommitAsync();
            }

            var nextSequence = checked(row.SessionSequence + 1);
            await scope.Transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_runtimes
                    SET session_version = session_version + 1,
                        session_sequence = @NextSequence
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    """,
                    new
                    {
                        candidate.OrganizationId,
                        candidate.SessionId,
                        NextSequence = nextSequence,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));

            var deliveryStatus = row.AuthoritativeUtc > dueAt.Value
                ? "late"
                : "issued";
            var payloadDigest = ProtectedContentRef.DigestUtf8(
                $"{candidate.WarningThresholdId}:{threshold.RemainingSecondsThreshold}:{remaining}:{deliveryStatus}");
            await scope.Transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_events (
                        event_id,
                        organization_id,
                        activity_id,
                        participant_id,
                        attempt_id,
                        session_id,
                        session_sequence,
                        event_type,
                        payload_digest,
                        committed_at)
                    VALUES (
                        @EventId,
                        @OrganizationId,
                        @ActivityId,
                        @ParticipantId,
                        @AttemptId,
                        @SessionId,
                        @SessionSequence,
                        'session.warning.issued',
                        @PayloadDigest,
                        @CommittedAt)
                    """,
                    new
                    {
                        EventId = Guid.CreateVersion7(),
                        candidate.OrganizationId,
                        candidate.ActivityId,
                        candidate.ParticipantId,
                        candidate.AttemptId,
                        candidate.SessionId,
                        SessionSequence = nextSequence,
                        PayloadDigest = payloadDigest,
                        CommittedAt = row.AuthoritativeUtc,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            await scope.Transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_warning_occurrences (
                        organization_id,
                        activity_id,
                        participant_id,
                        attempt_id,
                        session_id,
                        warning_threshold_id,
                        warning_code,
                        remaining_seconds_threshold,
                        due_at,
                        committed_at,
                        session_sequence,
                        remaining_seconds_at_commit,
                        delivery_status)
                    VALUES (
                        @OrganizationId,
                        @ActivityId,
                        @ParticipantId,
                        @AttemptId,
                        @SessionId,
                        @WarningThresholdId,
                        @WarningCode,
                        @RemainingSecondsThreshold,
                        @DueAt,
                        @CommittedAt,
                        @SessionSequence,
                        @RemainingSecondsAtCommit,
                        @DeliveryStatus)
                    """,
                    new
                    {
                        candidate.OrganizationId,
                        candidate.ActivityId,
                        candidate.ParticipantId,
                        candidate.AttemptId,
                        candidate.SessionId,
                        candidate.WarningThresholdId,
                        candidate.WarningCode,
                        RemainingSecondsThreshold = threshold.RemainingSecondsThreshold,
                        DueAt = dueAt.Value,
                        CommittedAt = row.AuthoritativeUtc,
                        SessionSequence = nextSequence,
                        RemainingSecondsAtCommit = remaining,
                        DeliveryStatus = deliveryStatus,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            await scope.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static DateTimeOffset? ResolveWarningDueAt(
        SessionLifecycleState lifecycle,
        DateTimeOffset startedAt,
        int accumulatedPausedSeconds,
        DateTimeOffset? openPauseStartedAt,
        HostedFrozenTimingPolicy policy,
        int remainingSecondsThreshold)
    {
        DateTimeOffset? activeDueAt = policy.BudgetSeconds is { } budgetSeconds
            ? startedAt.AddSeconds(
                checked(budgetSeconds - remainingSecondsThreshold + accumulatedPausedSeconds))
            : null;
        if (lifecycle == SessionLifecycleState.Paused
            && (openPauseStartedAt is null || activeDueAt > openPauseStartedAt))
        {
            activeDueAt = null;
        }

        var hardDueAt = policy.HardEndAtUtc?.AddSeconds(-remainingSecondsThreshold);
        return (activeDueAt, hardDueAt) switch
        {
            ({ } active, { } hard) => active <= hard ? active : hard,
            ({ } active, null) => active,
            (null, { } hard) => hard,
            _ => null,
        };
    }

    private static async Task<bool> WarningAlreadyIssuedAsync(
        Npgsql.NpgsqlTransaction transaction,
        DueWarningRow candidate,
        CancellationToken cancellationToken) =>
        await transaction.Connection!.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM session_warning_occurrences
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                      AND warning_threshold_id = @WarningThresholdId)
                """,
                candidate,
                transaction,
                cancellationToken: cancellationToken));

    private async Task<bool> TryExpireAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var binding = await bindings.GetForOrganizationSessionAsync(organizationId, sessionId, cancellationToken);
        if (binding is null)
        {
            return false;
        }

        long version;
        await using (var readScope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            IsolationLevel.RepeatableRead,
            cancellationToken))
        {
            var session = await runtimeRepository.LoadSnapshotAsync(
                binding.Ownership,
                binding,
                readScope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await readScope.RollbackAsync(cancellationToken);
                return false;
            }

            var observedAt = await runtimeRepository.ReadAuthoritativeUtcAsync(readScope.Transaction, cancellationToken);
            var startedAt = await readScope.Transaction.Connection!.QuerySingleAsync<DateTimeOffset>(
                new CommandDefinition(
                    """
                    SELECT created_at
                    FROM session_runtimes
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    """,
                    new
                    {
                        session.Ownership.OrganizationId,
                        session.Ownership.SessionId,
                    },
                    readScope.Transaction,
                    cancellationToken: cancellationToken));
            await readScope.RollbackAsync(cancellationToken);

            var policy = await frozenTiming.LoadAsync(
                session.Ownership.OrganizationId,
                session.Ownership.SessionId,
                startedAt,
                cancellationToken);
            var timing = HostedSessionTiming.Project(
                session.LifecycleState,
                startedAt,
                session.LastCommittedAt,
                observedAt,
                policy,
                session.AccumulatedPausedSeconds,
                session.OpenPauseStartedAt);
            if (timing.RemainingSeconds != 0
                || session.LifecycleState is not (SessionLifecycleState.Active or SessionLifecycleState.Paused))
            {
                return false;
            }

            version = session.SessionVersion;
        }

        foreach (var transition in new[]
                 {
                     SessionLifecycleTransitions.BeginCompleting,
                     SessionLifecycleTransitions.Complete,
                 })
        {
            var step = await lifecycle.ChangeAsync(
                new ChangeSessionLifecycleCommand(
                    settings.ServiceActor,
                    binding.Ownership,
                    version,
                    transition,
                    HostedSessionCommandCorrelation.ForCommandId(
                        HostedSessionCommandCorrelation.ExpiryCommandId(
                            binding.Ownership.SessionId,
                            transition)),
                    settings.SourceChannel,
                    transition == SessionLifecycleTransitions.Complete
                        ? TerminalReasonCategories.TimeExpiry
                        : null),
                binding,
                cancellationToken);
            if (!step.Succeeded && step.OutcomeCode != SessionLifecycleOutcomeCodes.Reconciled)
            {
                return false;
            }

            version = step.SessionVersion;
        }

        return true;
    }

    private sealed class DueRow
    {
        public Guid OrganizationId { get; init; }

        public Guid SessionId { get; init; }
    }

    private sealed class DueWarningRow
    {
        public Guid OrganizationId { get; init; }

        public Guid ActivityId { get; init; }

        public Guid ParticipantId { get; init; }

        public Guid AttemptId { get; init; }

        public Guid SessionId { get; init; }

        public string WarningThresholdId { get; init; } = string.Empty;

        public string WarningCode { get; init; } = string.Empty;

        public int RemainingSecondsThreshold { get; init; }

        public DateTimeOffset DueAt { get; init; }
    }

    private sealed class WarningLockRow
    {
        public string LifecycleState { get; init; } = string.Empty;

        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset LastCommittedAt { get; init; }

        public long SessionVersion { get; init; }

        public long SessionSequence { get; init; }

        public string TimingDocument { get; init; } = string.Empty;

        public int AccumulatedPausedSeconds { get; init; }

        public DateTimeOffset? OpenPauseStartedAt { get; init; }

        public DateTimeOffset AuthoritativeUtc { get; init; }
    }
}
