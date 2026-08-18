using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
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
    ICommitAuthorizationKernel authorizationKernel,
    IAuditEventWriter? auditEventWriter = null,
    IOutboxItemWriter? outboxItemWriter = null,
    ISessionRuntimeTelemetry? telemetry = null) : IDueTimerFirePort
{
    private const string ClaimDueSql = """
        WITH candidate AS MATERIALIZED (
            SELECT
                schedule.organization_id,
                schedule.activity_id,
                schedule.participant_id,
                schedule.attempt_id,
                schedule.session_id,
                schedule.schedule_revision_ordinal,
                schedule.timer_lane_delegation_id
            FROM session_timer_schedules AS schedule
            INNER JOIN session_runtimes AS runtime
                ON runtime.organization_id = schedule.organization_id
               AND runtime.activity_id = schedule.activity_id
               AND runtime.participant_id = schedule.participant_id
               AND runtime.attempt_id = schedule.attempt_id
               AND runtime.session_id = schedule.session_id
            INNER JOIN service_delegations AS delegation
                ON delegation.delegation_id = schedule.timer_lane_delegation_id
               AND delegation.organization_id = schedule.organization_id
               AND delegation.activity_id = schedule.activity_id
               AND delegation.participant_id = schedule.participant_id
               AND delegation.attempt_id = schedule.attempt_id
               AND delegation.session_id = schedule.session_id
               AND delegation.service_actor_id = @ServiceActorId
               AND delegation.allowed_action = @AllowedAction
               AND delegation.revoked_at IS NULL
               AND delegation.effective_at <= clock_timestamp()
               AND (delegation.expires_at IS NULL OR delegation.expires_at > clock_timestamp())
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
            schedule_revision_ordinal,
            timer_lane_delegation_id
        FROM candidate;
        """;

    private readonly IAuditEventWriter _auditEventWriter = auditEventWriter ?? new PostgresAuditEventWriter();
    private readonly IOutboxItemWriter _outboxItemWriter = outboxItemWriter ?? new PostgresOutboxItemWriter();
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    internal Func<Task>? AfterDueClaimedAsync { get; set; }

    internal Func<Task>? AfterAdmissionAuthorizedAsync { get; set; }

    public async Task<TimerFireResult> TryFireNextDueAsync(
        FireDueTimerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var due = await scope.Connection.QuerySingleOrDefaultAsync<DueScheduleRow>(
                new CommandDefinition(
                    ClaimDueSql,
                    new
                    {
                        ServiceActorId = command.Actor.ActorId,
                        AllowedAction = AuthorizationActions.FireSessionTimerLane,
                    },
                    transaction: scope.Transaction,
                    cancellationToken: cancellationToken));
            if (due is null)
            {
                await scope.CommitAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.Idle);
            }

            if (AfterDueClaimedAsync is not null)
            {
                await AfterDueClaimedAsync();
            }

            var ownership = new SessionOwnership(
                due.organization_id,
                due.activity_id,
                due.participant_id,
                due.attempt_id,
                due.session_id);
            if (due.timer_lane_delegation_id is null || due.timer_lane_delegation_id == Guid.Empty)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.AuthorityDenied);
            }

            var authorizationRequest = CreateAuthorizationRequest(command, ownership, due.timer_lane_delegation_id.Value);
            var admission = await authorizationKernel.AuthorizeInTransactionAsync(
                authorizationRequest,
                scope.Transaction,
                cancellationToken);
            if (!admission.IsPermitted)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.AuthorityDenied);
            }

            if (AfterAdmissionAuthorizedAsync is not null)
            {
                await AfterAdmissionAuthorizedAsync();
            }

            var binding = await bindingSource.GetAsync(ownership, cancellationToken);
            if (binding is null || binding.Ownership != ownership)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision);
            }

            var session = await runtimeRepository.LoadForUpdateAsync(
                ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var expectedVersion = session.SessionVersion;
            if (due.schedule_revision_ordinal is null or <= 0)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision, ObservedAt: authoritativeUtc);
            }

            var result = WithObservedAt(
                session.FireDueTimer(due.schedule_revision_ordinal.Value, authoritativeUtc),
                authoritativeUtc);
            var commitDecision = await authorizationKernel.ReauthorizeInTransactionAsync(
                authorizationRequest,
                scope.Transaction,
                cancellationToken);
            if (!commitDecision.IsPermitted)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TimerFireResult(false, TimerFireOutcomeCodes.AuthorityDenied, ObservedAt: authoritativeUtc);
            }

            if (result.OutcomeCode == TimerFireOutcomeCodes.BudgetExhausted)
            {
                var expired = await runtimeRepository.TrySaveLifecycleAsync(
                    ownership,
                    expectedVersion,
                    session,
                    scope.Transaction,
                    cancellationToken);
                if (!expired)
                {
                    await scope.RollbackAsync(cancellationToken);
                    return new TimerFireResult(
                        false,
                        TimerFireOutcomeCodes.StaleRevision,
                        result.Revision,
                        ObservedAt: result.ObservedAt);
                }

                await scope.CommitAsync(cancellationToken);
                return result;
            }

            if (result.OutcomeCode == TimerFireOutcomeCodes.LifecycleIneligible)
            {
                var targeted = session.TimerSchedules.FirstOrDefault(item =>
                    item.ScheduleRevision == due.schedule_revision_ordinal.Value);
                targeted?.Cancel();
                var cancelled = await runtimeRepository.TrySaveLifecycleAsync(
                    ownership,
                    expectedVersion,
                    session,
                    scope.Transaction,
                    cancellationToken);
                if (!cancelled)
                {
                    await scope.RollbackAsync(cancellationToken);
                    return new TimerFireResult(
                        false,
                        TimerFireOutcomeCodes.StaleRevision,
                        result.Revision,
                        ObservedAt: result.ObservedAt);
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
                return new TimerFireResult(
                    false,
                    TimerFireOutcomeCodes.LifecycleIneligible,
                    result.Revision,
                    ObservedAt: result.ObservedAt);
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
                return new TimerFireResult(
                    false,
                    TimerFireOutcomeCodes.StaleRevision,
                    result.Revision,
                    ObservedAt: result.ObservedAt);
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
                cancellationToken,
                _telemetry,
                commitDecision.RelationshipVersion);

            await scope.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static AuthorizationRequest CreateAuthorizationRequest(
        FireDueTimerCommand command,
        SessionOwnership ownership,
        Guid delegationId) =>
        new(
            new TrustedActor(command.Actor.ActorId, command.Actor.ActorType),
            new OrganizationScope(ownership.OrganizationId),
            AuthorizationActions.FireSessionTimerLane,
            new ResourceScope(
                new OrganizationScope(ownership.OrganizationId),
                AuthorizationResourceTypes.Session,
                ownership.SessionId),
            command.SourceChannel,
            command.CorrelationId,
            delegationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId);

    private sealed record DueScheduleRow(
        Guid organization_id,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id,
        long? schedule_revision_ordinal,
        Guid? timer_lane_delegation_id);

    private static TimerFireResult WithObservedAt(TimerFireResult result, DateTimeOffset observedAt) =>
        result with { ObservedAt = observedAt };
}
