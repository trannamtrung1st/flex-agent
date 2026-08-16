using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresFireDueTimerCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    ITrustedSessionBindingSource bindingSource,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null)
{
    private const string ClaimDueSql = """
        WITH candidate AS MATERIALIZED (
            SELECT
                schedule.organization_id,
                schedule.activity_id,
                schedule.participant_id,
                schedule.attempt_id,
                schedule.session_id,
                schedule.schedule_revision_ordinal
            FROM session_timer_schedules AS schedule
            INNER JOIN session_runtimes AS runtime
                ON runtime.organization_id = schedule.organization_id
               AND runtime.activity_id = schedule.activity_id
               AND runtime.participant_id = schedule.participant_id
               AND runtime.attempt_id = schedule.attempt_id
               AND runtime.session_id = schedule.session_id
            WHERE runtime.lifecycle_state = 'active'
              AND (
                    (
                        schedule.state = 'pending'
                        AND schedule.fire_at IS NOT NULL
                        AND schedule.fire_at <= clock_timestamp()
                    )
                    OR (
                        schedule.state = 'claimed'
                        AND (schedule.fire_at IS NULL OR schedule.fire_at <= clock_timestamp())
                    )
                  )
            ORDER BY COALESCE(schedule.fire_at, schedule.last_committed_at) ASC,
                     schedule.schedule_revision ASC
            FOR UPDATE OF schedule, runtime SKIP LOCKED
            LIMIT 1
        )
        SELECT
            organization_id,
            activity_id,
            participant_id,
            attempt_id,
            session_id,
            schedule_revision_ordinal
        FROM candidate;
        """;

    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();

    public async Task<TimerFireResult> TryFireNextDueAsync(
        FireDueTimerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var due = await scope.Connection.QuerySingleOrDefaultAsync<DueScheduleRow>(
                new CommandDefinition(ClaimDueSql, transaction: scope.Transaction, cancellationToken: cancellationToken));
            if (due is null)
            {
                await scope.CommitAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.Idle);
            }

            var ownership = new SessionOwnership(
                due.organization_id,
                due.activity_id,
                due.participant_id,
                due.attempt_id,
                due.session_id);
            var binding = await bindingSource.GetAsync(ownership, cancellationToken);
            if (binding is null || binding.Ownership != ownership)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.LifecycleIneligible);
            }

            var session = await runtimeRepository.LoadForUpdateAsync(
                ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.LifecycleIneligible);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var expectedVersion = session.SessionVersion;
            if (due.schedule_revision_ordinal is null or <= 0)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision);
            }

            var result = session.FireDueTimer(due.schedule_revision_ordinal.Value, authoritativeUtc);
            if (result.OutcomeCode == TimerFireOutcomeCodes.BudgetExhausted)
            {
                // Durable terminal mutation: persist Expired and commit. Hosts must
                // acknowledge this outcome and must not retry the same due row.
                var expired = await runtimeRepository.TrySaveLifecycleAsync(
                    ownership,
                    expectedVersion,
                    session,
                    scope.Transaction,
                    cancellationToken);
                if (!expired)
                {
                    await scope.RollbackAsync(cancellationToken);
                    return new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision, result.Revision);
                }

                await scope.CommitAsync(cancellationToken);
                return result;
            }

            if (!result.Succeeded)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            if (result.OutcomeCode == TimerFireOutcomeCodes.Reconciled)
            {
                await scope.CommitAsync(cancellationToken);
                return result;
            }

            if (result.Admission?.Invocation is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.LifecycleIneligible, result.Revision);
            }

            var saved = await runtimeRepository.TrySaveAdmissionAsync(
                ownership,
                expectedVersion,
                session,
                result.Admission.Invocation,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision, result.Revision);
            }

            await SessionRuntimePersistenceAudit.WriteAsync(
                _auditEventWriter,
                _outboxItemWriter,
                command.Actor,
                ownership,
                command.CorrelationId,
                command.SourceChannel,
                SessionRuntimeAuditActions.FireDueTimer,
                SessionRuntimeOutboxEventTypes.TimerLaneFired,
                result.Admission.Invocation.AgentInvocationId,
                authoritativeUtc,
                scope.Transaction,
                cancellationToken);

            await scope.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record DueScheduleRow(
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        long? schedule_revision_ordinal);
}
