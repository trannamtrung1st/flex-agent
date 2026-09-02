using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionTimerSchedulePersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Insert_active_persists_the_default_pending_schedule()
    {
        var prepared = await InsertActiveSessionAsync();
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await prepared.Repository.LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);

        var pending = Assert.Single(loaded!.TimerSchedules);
        Assert.Equal(TimerLaneStates.Pending, pending.LaneState);
        Assert.Equal(1, pending.ScheduleRevision);
        Assert.Equal("PT5M", pending.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.DefaultCadence, pending.RequestedByCategory);
        Assert.Equal(300, pending.RemainingActiveSeconds);
        Assert.NotNull(pending.DueAt);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var row = await connection.QuerySingleAsync<(string State, string LaneState)>(
            """
            SELECT state, lane_state
            FROM session_timer_schedules
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            new
            {
                prepared.Binding.Ownership.OrganizationId,
                prepared.Binding.Ownership.SessionId,
            });
        Assert.Equal("pending", row.State);
        Assert.Equal(TimerLaneStates.Pending, row.LaneState);
    }

    [Fact]
    public async Task Accepted_replacement_persists_superseded_as_replaced_and_one_pending_successor()
    {
        var prepared = await InsertActiveSessionAsync();
        var actor = SessionPersistenceFixtures.Actor(prepared.Organization.ActorId);
        var admitCoordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            new AdmitTrustedTriggerHandler());
        var completeCoordinator = new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            new CompleteInvocationHandler());

        var admitted = await admitCoordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                prepared.Binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.timer.replace"),
                "idem.timer.replace",
                Guid.NewGuid(),
                "integration.test"),
            prepared.Binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                prepared.Binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation!.AgentInvocationId,
                new NoActionRecommendation(
                    Guid.NewGuid().ToString("N"),
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    NoActionReasonCategories.IntentionalSilence,
                    new NextTimerRecommendation("PT2M", "1")),
                null,
                Guid.NewGuid(),
                "integration.test"),
            prepared.Binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await prepared.Repository.LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);

        Assert.Equal(2, loaded!.TimerSchedules.Count);
        Assert.Equal(TimerLaneStates.Superseded, loaded.TimerSchedules[0].LaneState);
        Assert.Equal(TimerLaneStates.Pending, loaded.CurrentTimerLane!.LaneState);
        Assert.Equal("PT2M", loaded.CurrentTimerLane.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, loaded.CurrentTimerLane.RequestedByCategory);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var states = (await connection.QueryAsync<(string State, string LaneState)>(
            """
            SELECT state, lane_state
            FROM session_timer_schedules
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
            ORDER BY schedule_revision_ordinal;
            """,
            new
            {
                prepared.Binding.Ownership.OrganizationId,
                prepared.Binding.Ownership.SessionId,
            })).AsList();
        Assert.Equal("replaced", states[0].State);
        Assert.Equal(TimerLaneStates.Superseded, states[0].LaneState);
        Assert.Equal("pending", states[1].State);
        Assert.Equal(TimerLaneStates.Pending, states[1].LaneState);
    }

    [Fact]
    public async Task Pause_and_resume_persist_remaining_delay_and_recomputed_due_at()
    {
        var prepared = await InsertActiveSessionAsync();
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var session = await prepared.Repository.LoadForUpdateAsync(
                prepared.Binding.Ownership,
                prepared.Binding,
                scope.Transaction,
                CancellationToken);
            var expectedVersion = session!.SessionVersion;
            var utc = await prepared.Repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            session.Pause(utc);
            Assert.True(
                await prepared.Repository.TrySaveLifecycleAsync(
                    prepared.Binding.Ownership,
                    expectedVersion,
                    session,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using (var pausedScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var paused = await prepared.Repository.LoadForUpdateAsync(
                prepared.Binding.Ownership,
                prepared.Binding,
                pausedScope.Transaction,
                CancellationToken);
            Assert.Equal(SessionLifecycleState.Paused, paused!.LifecycleState);
            Assert.Null(paused.CurrentTimerLane!.DueAt);
            Assert.True(paused.CurrentTimerLane.RemainingActiveSeconds <= 300);
            var expectedVersion = paused.SessionVersion;
            var utc = await prepared.Repository.ReadAuthoritativeUtcAsync(pausedScope.Transaction, CancellationToken);
            paused.Resume(utc);
            Assert.True(
                await prepared.Repository.TrySaveLifecycleAsync(
                    prepared.Binding.Ownership,
                    expectedVersion,
                    paused,
                    pausedScope.Transaction,
                    CancellationToken));
            await pausedScope.CommitAsync(CancellationToken);
        }

        await using var loadScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var resumed = await prepared.Repository.LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);
        Assert.Equal(SessionLifecycleState.Active, resumed!.LifecycleState);
        Assert.NotNull(resumed.CurrentTimerLane!.DueAt);
        Assert.Equal(TimerLaneStates.Pending, resumed.CurrentTimerLane.LaneState);
    }

    [Fact]
    public async Task Due_timer_fire_persists_one_invocation_and_retries_reconcile()
    {
        var prepared = await InsertActiveSessionAsync();
        TimerFireResult first;
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var session = await prepared.Repository.LoadForUpdateAsync(
                prepared.Binding.Ownership,
                prepared.Binding,
                scope.Transaction,
                CancellationToken);
            var utc = await prepared.Repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var expectedVersion = session!.SessionVersion;
            first = session.FireDueTimer(1, utc);
            Assert.True(first.Succeeded, first.OutcomeCode);
            Assert.Equal(TimerFireOutcomeCodes.Succeeded, first.OutcomeCode);
            Assert.True(
                await prepared.Repository.TrySaveAdmissionAsync(
                    prepared.Binding.Ownership,
                    expectedVersion,
                    session,
                    first.Admission!.Invocation!,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using (var retryScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var session = await prepared.Repository.LoadForUpdateAsync(
                prepared.Binding.Ownership,
                prepared.Binding,
                retryScope.Transaction,
                CancellationToken);
            var utc = await prepared.Repository.ReadAuthoritativeUtcAsync(retryScope.Transaction, CancellationToken);
            var retry = session!.FireDueTimer(1, utc);
            await retryScope.CommitAsync(CancellationToken);
            Assert.True(retry.Succeeded, retry.OutcomeCode);
            Assert.Equal(TimerFireOutcomeCodes.Reconciled, retry.OutcomeCode);
            Assert.Equal(
                first.Admission!.Invocation!.AgentInvocationId,
                retry.Admission!.Invocation!.AgentInvocationId);
        }

        await using var loadScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await prepared.Repository.LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);
        Assert.Equal(TimerLaneStates.Fired, loaded!.CurrentTimerLane!.LaneState);
        Assert.Equal(loaded.Invocations[0].AgentInvocationId, loaded.CurrentTimerLane.FiredInvocationId);
        Assert.Equal(RuntimeTriggerIdentifiers.TimerLaneDefaultType, loaded.Invocations[0].Trigger.TriggerType);
    }

    [Fact]
    public async Task Concurrent_due_claims_yield_exactly_one_timer_invocation()
    {
        var prepared = await InsertActiveSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Binding.Ownership);
        await MarkScheduleDueAsync(prepared.Binding.Ownership);
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(prepared.Binding);
        var first =             new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            bindingSource,
            CommitKernel());
        var second =             new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.RuntimeRepository(),
            bindingSource,
            CommitKernel());
        var command = new FireDueTimerCommand(
            SessionPersistenceFixtures.Actor(prepared.Organization.ActorId),
            Guid.NewGuid(),
            "integration.test");

        var results = await Task.WhenAll(
            first.TryFireNextDueAsync(command, CancellationToken),
            second.TryFireNextDueAsync(command, CancellationToken));

        var winners = results.Where(item => item.Succeeded && item.OutcomeCode == TimerFireOutcomeCodes.Succeeded).ToArray();
        var reconciled = results.Where(item => item.Succeeded && item.OutcomeCode == TimerFireOutcomeCodes.Reconciled).ToArray();
        Assert.Single(winners);
        Assert.True(reconciled.Length <= 1);
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await prepared.Repository.LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);
        Assert.Equal(1, loaded!.Invocations.Count(item =>
            item.Trigger.TriggerType == RuntimeTriggerIdentifiers.TimerLaneDefaultType));
    }

    [Fact]
    public async Task Recovered_claimed_schedule_with_positive_remaining_still_fires()
    {
        var prepared = await InsertActiveSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Binding.Ownership);
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var updated = await connection.ExecuteAsync(
                """
                UPDATE session_timer_schedules
                SET
                    state = 'claimed',
                    lane_state = 'claimed',
                    remaining_active_seconds = 300,
                    fire_at = clock_timestamp() - INTERVAL '1 second'
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND state = 'pending';
                """,
                new
                {
                    prepared.Binding.Ownership.OrganizationId,
                    prepared.Binding.Ownership.SessionId,
                });
            Assert.Equal(1, updated);
        }

        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(prepared.Binding);
        var coordinator =             new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            bindingSource,
            CommitKernel());

        var result = await coordinator.TryFireNextDueAsync(
            new FireDueTimerCommand(
                SessionPersistenceFixtures.Actor(prepared.Organization.ActorId),
                Guid.NewGuid(),
                "integration.test"),
            CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerLaneStates.Fired, result.Revision!.LaneState);
    }

    [Fact]
    public async Task Future_pending_schedule_is_not_claimed()
    {
        var prepared = await InsertActiveSessionAsync(armFromDatabaseClock: true);
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Binding.Ownership);
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(prepared.Binding);
        var coordinator =             new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            bindingSource,
            CommitKernel());

        var result = await coordinator.TryFireNextDueAsync(
            new FireDueTimerCommand(
                SessionPersistenceFixtures.Actor(prepared.Organization.ActorId),
                Guid.NewGuid(),
                "integration.test"),
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.Idle, result.OutcomeCode);
    }

    [Fact]
    public async Task Missing_binding_does_not_cancel_the_due_schedule()
    {
        var prepared = await InsertActiveSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Binding.Ownership);
        await MarkScheduleDueAsync(prepared.Binding.Ownership);
        var coordinator = new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            new MemoryTrustedSessionBindingSource(),
            CommitKernel());
        var command = new FireDueTimerCommand(
            SessionPersistenceFixtures.Actor(prepared.Organization.ActorId),
            Guid.NewGuid(),
            "integration.test");

        var first = await coordinator.TryFireNextDueAsync(command, CancellationToken);
        var second = await coordinator.TryFireNextDueAsync(command, CancellationToken);

        Assert.False(first.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.StaleRevision, first.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.StaleRevision, second.OutcomeCode);
        await using var loadScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await prepared.Repository.LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);
        Assert.Equal(TimerLaneStates.Pending, loaded!.TimerSchedules[0].LaneState);
    }

    [Fact]
    public async Task Paused_session_is_not_claimed_even_when_fire_at_is_past()
    {
        var prepared = await InsertActiveSessionAsync();
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var session = await prepared.Repository.LoadForUpdateAsync(
                prepared.Binding.Ownership,
                prepared.Binding,
                scope.Transaction,
                CancellationToken);
            var expectedVersion = session!.SessionVersion;
            var utc = await prepared.Repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            session.Pause(utc);
            Assert.True(
                await prepared.Repository.TrySaveLifecycleAsync(
                    prepared.Binding.Ownership,
                    expectedVersion,
                    session,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var updated = await connection.ExecuteAsync(
                """
                UPDATE session_timer_schedules
                SET fire_at = clock_timestamp() - INTERVAL '1 second'
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId;
                """,
                new
                {
                    prepared.Binding.Ownership.OrganizationId,
                    prepared.Binding.Ownership.SessionId,
                });
            Assert.Equal(1, updated);
        }

        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Binding.Ownership);
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(prepared.Binding);
        var coordinator =             new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            prepared.Repository,
            bindingSource,
            CommitKernel());

        var result = await coordinator.TryFireNextDueAsync(
            new FireDueTimerCommand(
                SessionPersistenceFixtures.Actor(prepared.Organization.ActorId),
                Guid.NewGuid(),
                "integration.test"),
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.Idle, result.OutcomeCode);
    }

    [Fact]
    public async Task Budget_exhausted_due_row_expires_and_does_not_block_another_session()
    {
        var exhausted = await InsertActiveSessionAsync(maxTimerTriggeredInvocations: 1);
        TimerFireResult fired;
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var session = await exhausted.Repository.LoadForUpdateAsync(
                exhausted.Binding.Ownership,
                exhausted.Binding,
                scope.Transaction,
                CancellationToken);
            var utc = await exhausted.Repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var expectedVersion = session!.SessionVersion;
            fired = session.FireDueTimer(1, utc);
            Assert.True(fired.Succeeded, fired.OutcomeCode);
            var admitted = fired.Admission!.Invocation!;
            Assert.True(
                await exhausted.Repository.TrySaveAdmissionAsync(
                    exhausted.Binding.Ownership,
                    expectedVersion,
                    session,
                    admitted,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        var completed = await new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            exhausted.Repository,
            new CompleteInvocationHandler()).CompleteAsync(
            new CompleteInvocationCommand(
                SessionPersistenceFixtures.Actor(exhausted.Organization.ActorId),
                exhausted.Binding.Ownership,
                fired.Admission!.SessionVersion!.Value,
                fired.Admission.Invocation!.AgentInvocationId,
                SessionRuntimeTestFixturesNoAction(fired.Admission.Invocation.AgentInvocationId),
                null,
                Guid.NewGuid(),
                "integration.test"),
            exhausted.Binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var pending = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)::int
                FROM session_timer_schedules
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND state IN ('pending', 'claimed');
                """,
                new
                {
                    exhausted.Binding.Ownership.OrganizationId,
                    exhausted.Binding.Ownership.SessionId,
                });
            Assert.Equal(0, pending);

            var inserted = await connection.ExecuteAsync(
                """
                INSERT INTO session_timer_schedules (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    schedule_revision, schedule_revision_ordinal, state, lane_state, relative_delay,
                    remaining_active_seconds, remaining_since, fire_at, requested_by_category, created_at,
                    timer_lane_delegation_id)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'tsrev.poison', 2, 'pending', 'pending', 'PT5M',
                    0, clock_timestamp(), clock_timestamp() - INTERVAL '1 minute',
                    'successor_after_fire', clock_timestamp(),
                    (SELECT delegation_id
                     FROM service_delegations
                     WHERE organization_id = @OrganizationId
                       AND session_id = @SessionId
                       AND allowed_action = 'session.timer_lane.fire'
                       AND revoked_at IS NULL));
                """,
                new
                {
                    exhausted.Binding.Ownership.OrganizationId,
                    exhausted.Binding.Ownership.ActivityId,
                    exhausted.Binding.Ownership.ParticipantId,
                    exhausted.Binding.Ownership.AttemptId,
                    exhausted.Binding.Ownership.SessionId,
                });
            Assert.Equal(1, inserted);
        }

        var other = await InsertActiveSessionAsync(timerServiceActorId: exhausted.Organization.ActorId);
        await MarkScheduleDueAsync(other.Binding.Ownership);
        await using var otherDue = await HoldOtherDueSchedulesAsync(
            exhausted.Binding.Ownership,
            other.Binding.Ownership);
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(exhausted.Binding);
        bindingSource.Register(other.Binding);
        var coordinator = new PostgresFireDueTimerCoordinator(
            Fixture.Services.ConnectionAccessor,
            exhausted.Repository,
            bindingSource,
            CommitKernel());
        var command = new FireDueTimerCommand(
            SessionPersistenceFixtures.Actor(exhausted.Organization.ActorId),
            Guid.NewGuid(),
            "integration.test");

        var first = await coordinator.TryFireNextDueAsync(command, CancellationToken);
        var second = await coordinator.TryFireNextDueAsync(command, CancellationToken);
        var results = new[] { first, second };
        Assert.Contains(results, item => item.OutcomeCode == TimerFireOutcomeCodes.BudgetExhausted);
        Assert.Contains(
            results,
            item => item.Succeeded
                && item.OutcomeCode == TimerFireOutcomeCodes.Succeeded
                && item.Admission!.Invocation!.Ownership.SessionId == other.Binding.Ownership.SessionId);
    }

    private static NoActionRecommendation SessionRuntimeTestFixturesNoAction(string invocationId) =>
        new(
            Guid.NewGuid().ToString("N"),
            invocationId,
            new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
            NoActionReasonCategories.IntentionalSilence,
            null);

    private async Task<PreparedSession> InsertActiveSessionAsync(
        bool armFromDatabaseClock = false,
        int maxTimerTriggeredInvocations = 8,
        Guid? timerServiceActorId = null)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(
            organization.OrganizationId,
            cooldownSeconds: 0,
            maxTimerTriggeredInvocations);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        DateTimeOffset startedAt = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            if (armFromDatabaseClock)
            {
                startedAt = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            }

            var session = SessionRuntime.CreateActive(binding, startedAt);
            var delegationClock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var timerLaneDelegation = new AuthorizedServiceDelegationIssue(
                new ServiceDelegationIssue(
                    Guid.NewGuid(),
                    timerServiceActorId ?? organization.ActorId,
                    AuthorizationActions.FireSessionTimerLane,
                    "session.timer_lane.scheduler",
                    "system.session_runtime",
                    delegationClock.AddMinutes(-1),
                    delegationClock.AddDays(6)),
                new ServiceDelegationMutationContext(
                    new TrustedActor(organization.ActorId, "integration.test"),
                    Guid.NewGuid(),
                    "session.start",
                    "session.start.timer_lane"));
            await Fixture.GrantOrganizationActionAsync(
                organization.OrganizationId,
                organization.ActorId,
                AuthorizationActions.IssueServiceDelegation);
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken,
                timerLaneDelegation,
                CommitKernel(),
                InvocationExecuteDelegationSupport.CreateIssue(
                    organization,
                    timerServiceActorId ?? organization.ActorId,
                    delegationClock));
            await scope.CommitAsync(CancellationToken);
        }

        return new PreparedSession(organization, binding, repository);
    }

    private async Task MarkScheduleDueAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var updated = await connection.ExecuteAsync(
            """
            UPDATE session_timer_schedules
            SET fire_at = clock_timestamp() - INTERVAL '1 second'
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND state = 'pending';
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
            });
        Assert.Equal(1, updated);
    }

    private Task<IAsyncDisposable> HoldOtherDueSchedulesAsync(SessionOwnership ownership) =>
        HoldOtherDueSchedulesAsync(ownership, other: null);

    private async Task<IAsyncDisposable> HoldOtherDueSchedulesAsync(
        SessionOwnership ownership,
        SessionOwnership? other)
    {
        var connection = new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var transaction = await connection.BeginTransactionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                SELECT schedule_revision
                FROM session_timer_schedules
                WHERE NOT (
                        (organization_id = @OrganizationId AND session_id = @SessionId)
                        OR (
                            @OtherOrganizationId IS NOT NULL
                            AND organization_id = @OtherOrganizationId
                            AND session_id = @OtherSessionId
                        )
                      )
                  AND (
                        (state = 'pending' AND fire_at IS NOT NULL AND fire_at <= clock_timestamp())
                        OR state = 'claimed'
                      )
                FOR UPDATE;
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    OtherOrganizationId = other?.OrganizationId,
                    OtherSessionId = other?.SessionId,
                },
                transaction,
                cancellationToken: CancellationToken));
        return new HeldDueScope(connection, transaction);
    }

    private ICommitAuthorizationKernel CommitKernel() =>
        (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel;

    private sealed record PreparedSession(
        SeededOrganization Organization,
        TrustedSessionBinding Binding,
        PostgresSessionRuntimeRepository Repository);

    private sealed class HeldDueScope(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
