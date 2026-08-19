using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionLifecyclePersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Begin_completing_persists_cancelled_timer_and_incomplete_seal_wakeup()
    {
        var ready = await PrepareOpenFragmentAsync("cutok");
        var correlationId = Guid.NewGuid();

        var result = await ready.Lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                SessionLifecycleTransitions.BeginCompleting,
                correlationId,
                "integration.test"),
            ready.Binding,
            CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleOutcomeCodes.Succeeded, result.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var lifecycle = await connection.QuerySingleAsync<(string Lifecycle, string TimerState, string LaneState, string Completion)>(
            """
            SELECT
                runtime.lifecycle_state,
                schedule.state,
                schedule.lane_state,
                message.completion_state
            FROM session_runtimes AS runtime
            INNER JOIN session_timer_schedules AS schedule
                ON schedule.organization_id = runtime.organization_id
               AND schedule.session_id = runtime.session_id
               AND schedule.lane_state = 'cancelled'
            INNER JOIN session_messages AS message
                ON message.organization_id = runtime.organization_id
               AND message.session_id = runtime.session_id
               AND message.author_type = 'agent'
            WHERE runtime.organization_id = @OrganizationId
              AND runtime.session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        var sealEvent = await connection.ExecuteScalarAsync<string>(
            """
            SELECT event_type
            FROM outbox_items
            WHERE correlation_id = @CorrelationId
              AND event_type = @EventType;
            """,
            new
            {
                CorrelationId = correlationId,
                EventType = SessionRuntimeOutboxEventTypes.AgentMessageSealed,
            });
        var auditAction = await connection.ExecuteScalarAsync<string>(
            "SELECT action FROM audit_events WHERE correlation_id = @CorrelationId AND action = @Action;",
            new { CorrelationId = correlationId, Action = SessionRuntimeAuditActions.SealAgentResponse });

        Assert.Equal("completing", lifecycle.Lifecycle);
        Assert.Equal("cancelled", lifecycle.TimerState);
        Assert.Equal(TimerLaneStates.Cancelled, lifecycle.LaneState);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, lifecycle.Completion);
        Assert.Equal(SessionRuntimeOutboxEventTypes.AgentMessageSealed, sealEvent);
        Assert.Equal(SessionRuntimeAuditActions.SealAgentResponse, auditAction);
    }

    [Fact]
    public async Task Outbox_failure_during_cutoff_leaves_the_message_open_and_the_timer_pending()
    {
        var ready = await PrepareOpenFragmentAsync(
            "cutfail",
            new PostgresAuditEventWriter(),
            new FaultInjectingOutboxItemWriter());
        var correlationId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ready.Lifecycle.ChangeAsync(
                new ChangeSessionLifecycleCommand(
                    ready.Actor,
                    ready.Binding.Ownership,
                    ready.SessionVersion,
                    SessionLifecycleTransitions.BeginCompleting,
                    correlationId,
                    "integration.test"),
                ready.Binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var row = await connection.QuerySingleAsync<(string Lifecycle, string TimerState, string Completion)>(
            """
            SELECT
                runtime.lifecycle_state,
                schedule.state,
                message.completion_state
            FROM session_runtimes AS runtime
            INNER JOIN session_timer_schedules AS schedule
                ON schedule.organization_id = runtime.organization_id
               AND schedule.session_id = runtime.session_id
               AND schedule.lane_state = 'pending'
            INNER JOIN session_messages AS message
                ON message.organization_id = runtime.organization_id
               AND message.session_id = runtime.session_id
               AND message.author_type = 'agent'
            WHERE runtime.organization_id = @OrganizationId
              AND runtime.session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        Assert.Equal("active", row.Lifecycle);
        Assert.Equal("pending", row.TimerState);
        Assert.Equal(AgentMessageCompletionStates.Open, row.Completion);
        Assert.Equal(0, await CountOutboxAsync(connection, correlationId, eventType: null));
    }

    [Fact]
    public async Task Duplicate_begin_completing_reconciles_without_a_second_seal_outbox()
    {
        var ready = await PrepareOpenFragmentAsync("cutdup");
        var correlationId = Guid.NewGuid();
        var command = new ChangeSessionLifecycleCommand(
            ready.Actor,
            ready.Binding.Ownership,
            ready.SessionVersion,
            SessionLifecycleTransitions.BeginCompleting,
            correlationId,
            "integration.test");

        Assert.True((await ready.Lifecycle.ChangeAsync(command, ready.Binding, CancellationToken)).Succeeded);
        var retry = await ready.Lifecycle.ChangeAsync(command, ready.Binding, CancellationToken);

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(SessionLifecycleOutcomeCodes.Reconciled, retry.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await CountOutboxAsync(
            connection,
            correlationId,
            SessionRuntimeOutboxEventTypes.AgentMessageSealed));
    }

    [Fact]
    public async Task Abort_persists_cancelled_timer_and_incomplete_seal_wakeup()
    {
        var ready = await PrepareOpenFragmentAsync("cutabort");
        var correlationId = Guid.NewGuid();

        var result = await ready.Lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                SessionLifecycleTransitions.Abort,
                correlationId,
                "integration.test"),
            ready.Binding,
            CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Aborted, result.LifecycleState);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var row = await connection.QuerySingleAsync<(string Lifecycle, string TimerState, string Completion)>(
            """
            SELECT
                runtime.lifecycle_state,
                schedule.state,
                message.completion_state
            FROM session_runtimes AS runtime
            INNER JOIN session_timer_schedules AS schedule
                ON schedule.organization_id = runtime.organization_id
               AND schedule.session_id = runtime.session_id
               AND schedule.lane_state = 'cancelled'
            INNER JOIN session_messages AS message
                ON message.organization_id = runtime.organization_id
               AND message.session_id = runtime.session_id
               AND message.author_type = 'agent'
            WHERE runtime.organization_id = @OrganizationId
              AND runtime.session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        Assert.Equal("aborted", row.Lifecycle);
        Assert.Equal("cancelled", row.TimerState);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, row.Completion);
        Assert.Equal(1, await CountOutboxAsync(
            connection,
            correlationId,
            SessionRuntimeOutboxEventTypes.AgentMessageSealed));
    }

    private async Task<ReadyLifecycle> PrepareOpenFragmentAsync(
        string key,
        IAuditEventWriter? auditEventWriter = null,
        IOutboxItemWriter? outboxItemWriter = null)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var completeCoordinator = new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new CompleteInvocationHandler());
        var publisher = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler());
        var lifecycle = new PostgresSessionLifecycleCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new ChangeSessionLifecycleHandler(),
            auditEventWriter,
            outboxItemWriter);
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                ExpectedSessionVersion: 0,
                $"msg.p.{key}",
                $"turn.{key}",
                $"slot.{key}",
                $"trig.participant.{key}",
                $"idem.p.{key}",
                Guid.NewGuid(),
                "integration.test",
                "synthetic.participant.message"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation!.AgentInvocationId,
                new EnvelopeRecommendation(
                    $"adec.{key}.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            $"turn.{key}",
                            $"slot.{key}"),
                    ],
                    []),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        long sessionVersion;
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            sessionVersion = loaded!.SessionVersion;
            await scope.CommitAsync(CancellationToken);
        }

        Assert.True((await publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                actor,
                binding.Ownership,
                sessionVersion,
                admitted.Invocation.AgentInvocationId,
                1,
                "Hel",
                $"agen.{key}.1",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken)).Succeeded);

        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            sessionVersion = await connection.ExecuteScalarAsync<long>(
                """
                SELECT session_version
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId;
                """,
                binding.Ownership);
        }

        return new ReadyLifecycle(binding, actor, sessionVersion, lifecycle);
    }

    private static async Task<int> CountOutboxAsync(
        NpgsqlConnection connection,
        Guid correlationId,
        string? eventType) =>
        await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM outbox_items
            WHERE correlation_id = @CorrelationId
              AND (@EventType IS NULL OR event_type = @EventType);
            """,
            new { CorrelationId = correlationId, EventType = eventType });

    private sealed record ReadyLifecycle(
        TrustedSessionBinding Binding,
        TrustedRuntimeActor Actor,
        long SessionVersion,
        PostgresSessionLifecycleCoordinator Lifecycle);

    private sealed class FaultInjectingOutboxItemWriter : IOutboxItemWriter
    {
        public Task InsertAsync(
            OutboxItemWriteModel outboxItem,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected outbox failure.");
    }
}
