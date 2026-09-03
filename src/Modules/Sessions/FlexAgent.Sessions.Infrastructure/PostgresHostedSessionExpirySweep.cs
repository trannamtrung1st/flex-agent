using System.Data;
using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresHostedSessionExpirySweep(
    PostgresConnectionAccessor connectionAccessor,
    ITrustedSessionBindingSource bindings,
    PostgresSessionRuntimeRepository runtimeRepository,
    IHostedSessionFrozenTimingSource frozenTiming,
    PostgresSessionLifecycleCoordinator lifecycle,
    HostedSessionExpirySettings settings) : IHostedSessionExpirySweep
{
    private const int BatchSize = 32;

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken = default)
    {
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

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            IsolationLevel.RepeatableRead,
            cancellationToken);
        try
        {
            var session = await runtimeRepository.LoadSnapshotAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            var observedAt = await runtimeRepository.ReadAuthoritativeUtcAsync(scope.Transaction, cancellationToken);
            var startedAt = await scope.Transaction.Connection!.QuerySingleAsync<DateTimeOffset>(
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
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            await scope.CommitAsync(cancellationToken);
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

            var commandId = $"sessioncommand.expiry.{session.Ownership.SessionId:N}";
            var version = session.SessionVersion;
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
                        HostedSessionCommandCorrelation.ForCommandId(commandId),
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
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed class DueRow
    {
        public Guid OrganizationId { get; init; }

        public Guid SessionId { get; init; }
    }
}
