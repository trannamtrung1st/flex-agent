using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class HostedSessionTimingFairnessTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task AcceptAsync_rejects_messages_after_the_cutoff_crosses_under_session_lock()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var startedAt = DateTimeOffset.UtcNow.AddSeconds(-3598);
        var session = SessionRuntime.CreateActive(binding, startedAt);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await InsertFrozenTimingAsync(
                scope.Transaction,
                binding.Ownership,
                TimedPolicy(startedAt.AddHours(2), budgetSeconds: 3600));
            await BackdateSessionStartAsync(scope.Transaction, binding.Ownership, startedAt);
            await scope.CommitAsync(CancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken);

        var accept = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var result = await accept.AcceptAsync(
            new AcceptParticipantMessageCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                ExpectedSessionVersion: 0,
                "msg.cutoff.cross",
                "turn.cutoff.cross",
                "slot.cutoff.cross",
                "trig.cutoff.cross",
                "idem.cutoff.cross",
                Guid.NewGuid(),
                "integration.test",
                "late message"),
            binding,
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.CutoffPassed, result.OutcomeCode);
    }

    [Fact]
    public async Task Expiry_sweep_finds_due_active_session_when_many_older_paused_sessions_precede_it()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var expiredBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var expiredStartedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var expiredSession = SessionRuntime.CreateActive(expiredBinding, expiredStartedAt);
        var expiredPolicy = TimedPolicy(expiredStartedAt.AddHours(4), budgetSeconds: 120);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                expiredBinding.Ownership,
                expiredSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await InsertFrozenTimingAsync(scope.Transaction, expiredBinding.Ownership, expiredPolicy);
            await BackdateSessionStartAsync(scope.Transaction, expiredBinding.Ownership, expiredStartedAt);
            await scope.CommitAsync(CancellationToken);
        }

        for (var index = 0; index < 32; index++)
        {
            var pausedBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
            var pausedStartedAt = DateTimeOffset.UtcNow.AddDays(-30).AddMinutes(index);
            var pausedSession = SessionRuntime.CreateActive(pausedBinding, pausedStartedAt);
            await using var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
            await repository.InsertActiveAsync(
                pausedBinding.Ownership,
                pausedSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await scope.Transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_runtimes
                    SET lifecycle_state = 'paused',
                        last_committed_at = @PausedAt,
                        created_at = @PausedAt
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    """,
                    new
                    {
                        pausedBinding.Ownership.OrganizationId,
                        pausedBinding.Ownership.SessionId,
                        PausedAt = pausedStartedAt.AddMinutes(5),
                    },
                    scope.Transaction,
                    cancellationToken: CancellationToken));
            await scope.Transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_pause_intervals (
                        organization_id,
                        activity_id,
                        participant_id,
                        attempt_id,
                        session_id,
                        pause_id,
                        started_at,
                        ended_at,
                        last_committed_at)
                    VALUES (
                        @OrganizationId,
                        @ActivityId,
                        @ParticipantId,
                        @AttemptId,
                        @SessionId,
                        @PauseId,
                        @StartedAt,
                        NULL,
                        @StartedAt)
                    """,
                    new
                    {
                        pausedBinding.Ownership.OrganizationId,
                        pausedBinding.Ownership.ActivityId,
                        pausedBinding.Ownership.ParticipantId,
                        pausedBinding.Ownership.AttemptId,
                        pausedBinding.Ownership.SessionId,
                        PauseId = Guid.CreateVersion7(),
                        StartedAt = pausedStartedAt.AddMinutes(5),
                    },
                    scope.Transaction,
                    cancellationToken: CancellationToken));
            await InsertFrozenTimingAsync(
                scope.Transaction,
                pausedBinding.Ownership,
                TimedPolicy(DateTimeOffset.UtcNow.AddDays(10), budgetSeconds: 3600));
            await scope.CommitAsync(CancellationToken);
        }

        var sweep = CreateExpirySweep(
            organization.ActorId,
            repository,
            new SyntheticConfiguredActorWorkloadIdentitySource(organization.ActorId));
        sweep.BeforeWarningCommitAsync = () =>
            throw new InvalidOperationException("synthetic warning persistence failure");
        var expiredCount = await sweep.ExpireDueAsync(CancellationToken);
        Assert.True(expiredCount >= 1);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var lifecycle = await connection.QuerySingleAsync<string>(
            """
            SELECT lifecycle_state
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
            """,
            new
            {
                expiredBinding.Ownership.OrganizationId,
                expiredBinding.Ownership.SessionId,
            });
        Assert.Equal("completed", lifecycle);
    }

    [Fact]
    public async Task Expiry_sweep_finds_due_active_session_when_many_resumed_active_sessions_with_long_pause_history_precede_it()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var expiredBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var expiredStartedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var expiredSession = SessionRuntime.CreateActive(expiredBinding, expiredStartedAt);
        var expiredPolicy = TimedPolicy(expiredStartedAt.AddHours(4), budgetSeconds: 120);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                expiredBinding.Ownership,
                expiredSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await InsertFrozenTimingAsync(scope.Transaction, expiredBinding.Ownership, expiredPolicy);
            await BackdateSessionStartAsync(scope.Transaction, expiredBinding.Ownership, expiredStartedAt);
            await scope.CommitAsync(CancellationToken);
        }

        for (var index = 0; index < 32; index++)
        {
            var resumedBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
            var resumedStartedAt = DateTimeOffset.UtcNow.AddMinutes(-100).AddSeconds(index);
            var resumedSession = SessionRuntime.CreateActive(resumedBinding, resumedStartedAt);
            var pauseStartedAt = resumedStartedAt.AddMinutes(1);
            var pauseEndedAt = resumedStartedAt.AddMinutes(51);
            await using var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
            await repository.InsertActiveAsync(
                resumedBinding.Ownership,
                resumedSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await BackdateSessionStartAsync(scope.Transaction, resumedBinding.Ownership, resumedStartedAt);
            await InsertClosedPauseIntervalAsync(
                scope.Transaction,
                resumedBinding.Ownership,
                pauseStartedAt,
                pauseEndedAt);
            await InsertFrozenTimingAsync(
                scope.Transaction,
                resumedBinding.Ownership,
                TimedPolicy(DateTimeOffset.UtcNow.AddDays(10), budgetSeconds: 3600));
            await scope.CommitAsync(CancellationToken);
        }

        var sweep = CreateExpirySweep(organization.ActorId, repository);
        var expiredCount = await sweep.ExpireDueAsync(CancellationToken);
        Assert.True(expiredCount >= 1);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var lifecycle = await connection.QuerySingleAsync<string>(
            """
            SELECT lifecycle_state
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
            """,
            new
            {
                expiredBinding.Ownership.OrganizationId,
                expiredBinding.Ownership.SessionId,
            });
        Assert.Equal("completed", lifecycle);
    }

    [Fact]
    public async Task AcceptAsync_rejects_messages_when_frozen_timing_is_unavailable()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var session = SessionRuntime.CreateActive(binding, startedAt);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await BackdateSessionStartAsync(scope.Transaction, binding.Ownership, startedAt);
            await scope.CommitAsync(CancellationToken);
        }

        var accept = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var result = await accept.AcceptAsync(
            new AcceptParticipantMessageCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                ExpectedSessionVersion: 0,
                "msg.timing.unavailable",
                "turn.timing.unavailable",
                "slot.timing.unavailable",
                "trig.timing.unavailable",
                "idem.timing.unavailable",
                Guid.NewGuid(),
                "integration.test",
                "blocked message"),
            binding,
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.TimingUnavailable, result.OutcomeCode);
    }

    [Fact]
    public async Task Expiry_sweep_finds_due_session_when_many_unavailable_hard_end_rows_precede_it()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var expiredBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var expiredStartedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var expiredSession = SessionRuntime.CreateActive(expiredBinding, expiredStartedAt);
        var expiredHardEnd = DateTimeOffset.UtcNow.AddMinutes(-1);
        var expiredPolicy = new HostedFrozenTimingPolicy(
            HostedTimingReconstruction.Unbounded,
            null,
            [],
            expiredHardEnd);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                expiredBinding.Ownership,
                expiredSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await InsertFrozenTimingAsync(scope.Transaction, expiredBinding.Ownership, expiredPolicy);
            await BackdateSessionStartAsync(scope.Transaction, expiredBinding.Ownership, expiredStartedAt);
            await scope.CommitAsync(CancellationToken);
        }

        for (var index = 0; index < 32; index++)
        {
            var corruptBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
            var corruptStartedAt = DateTimeOffset.UtcNow.AddDays(-30).AddMinutes(index);
            var corruptSession = SessionRuntime.CreateActive(corruptBinding, corruptStartedAt);
            var corruptPolicy = new HostedFrozenTimingPolicy(
                HostedTimingReconstruction.Unavailable,
                null,
                [],
                DateTimeOffset.UtcNow.AddDays(-1));
            await using var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
            await repository.InsertActiveAsync(
                corruptBinding.Ownership,
                corruptSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await InsertFrozenTimingAsync(scope.Transaction, corruptBinding.Ownership, corruptPolicy);
            await BackdateSessionStartAsync(scope.Transaction, corruptBinding.Ownership, corruptStartedAt);
            await scope.CommitAsync(CancellationToken);
        }

        var sweep = CreateExpirySweep(organization.ActorId, repository);
        var expiredCount = await sweep.ExpireDueAsync(CancellationToken);
        Assert.True(expiredCount >= 1);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var lifecycle = await connection.QuerySingleAsync<string>(
            """
            SELECT lifecycle_state
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
            """,
            new
            {
                expiredBinding.Ownership.OrganizationId,
                expiredBinding.Ownership.SessionId,
            });
        Assert.Equal("completed", lifecycle);
    }

    [Fact]
    public async Task Expiry_sweep_records_each_due_warning_once_with_reconstructable_history()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-50);
        var session = SessionRuntime.CreateActive(binding, startedAt);
        var policy = new HostedFrozenTimingPolicy(
            HostedTimingReconstruction.Timed,
            3600,
            [
                new HostedTimingWarningThreshold("approaching", 900),
                new HostedTimingWarningThreshold("imminent", 300),
            ],
            DateTimeOffset.UtcNow.AddHours(2));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await InsertFrozenTimingAsync(scope.Transaction, binding.Ownership, policy);
            await BackdateSessionStartAsync(scope.Transaction, binding.Ownership, startedAt);
            var pausedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
            await scope.Transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_runtimes
                    SET lifecycle_state = 'paused',
                        last_committed_at = @PausedAt
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    """,
                    new
                    {
                        binding.Ownership.OrganizationId,
                        binding.Ownership.SessionId,
                        PausedAt = pausedAt,
                    },
                    scope.Transaction,
                    cancellationToken: CancellationToken));
            await InsertOpenPauseIntervalAsync(
                scope.Transaction,
                binding.Ownership,
                pausedAt);
            await InsertClosedPauseIntervalAsync(
                scope.Transaction,
                binding.Ownership,
                startedAt.AddMinutes(1),
                startedAt.AddMinutes(1).AddMilliseconds(800));
            await scope.CommitAsync(CancellationToken);
        }

        var deniedSweep = CreateExpirySweep(
            organization.ActorId,
            repository,
            new SyntheticConfiguredActorWorkloadIdentitySource(Guid.NewGuid()));
        await deniedSweep.ExpireDueAsync(CancellationToken);
        await using (var deniedConnection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            Assert.Equal(
                0,
                await deniedConnection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM session_warning_occurrences
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    """,
                    new
                    {
                        binding.Ownership.OrganizationId,
                        binding.Ownership.SessionId,
                    }));
        }

        var sweep = CreateExpirySweep(
            organization.ActorId,
            repository,
            new SyntheticConfiguredActorWorkloadIdentitySource(organization.ActorId));
        await Task.WhenAll(
            sweep.ExpireDueAsync(CancellationToken),
            sweep.ExpireDueAsync(CancellationToken));
        await sweep.ExpireDueAsync(CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var warnings = (await connection.QueryAsync<WarningOccurrenceRow>(
            """
            SELECT warning_threshold_id AS WarningThresholdId,
                   warning_code AS WarningCode,
                   remaining_seconds_threshold AS RemainingSecondsThreshold,
                   due_at AS DueAt,
                   committed_at AS CommittedAt,
                   session_sequence AS SessionSequence,
                   remaining_seconds_at_commit AS RemainingSecondsAtCommit,
                   delivery_status AS DeliveryStatus
            FROM session_warning_occurrences
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
            ORDER BY session_sequence
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
            })).AsList();

        var warning = Assert.Single(warnings);
        Assert.Equal("approaching", warning.WarningThresholdId);
        Assert.Equal("approaching", warning.WarningCode);
        Assert.Equal(900, warning.RemainingSecondsThreshold);
        Assert.True(warning.DueAt <= warning.CommittedAt);
        Assert.InRange(
            warning.DueAt,
            startedAt.AddSeconds(2700).AddSeconds(-1),
            startedAt.AddSeconds(2700).AddSeconds(1));
        Assert.Equal(1, warning.SessionSequence);
        Assert.InRange(warning.RemainingSecondsAtCommit, 0, 900);
        Assert.Equal("late", warning.DeliveryStatus);

        var replay = await new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new ReplayAuthorizedSessionEventsHandler(),
            new PostgresHostedFrozenTimingDocumentSource(Fixture.Services.ConnectionAccessor)).ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                null,
                UseHostedProjection: true),
            binding,
            CancellationToken);
        var warningEvent = Assert.Single(
            replay.Events,
            item => item.EventType == HostedSessionEventTypes.WarningIssued);
        Assert.Equal("approaching", warningEvent.WarningCode);
        Assert.Equal(warning.RemainingSecondsAtCommit, warningEvent.RemainingSeconds);

        var resumed = await new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new ReplayAuthorizedSessionEventsHandler(),
            new PostgresHostedFrozenTimingDocumentSource(Fixture.Services.ConnectionAccessor)).ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                warningEvent.StreamCursor,
                UseHostedProjection: true),
            binding,
            CancellationToken);
        Assert.True(resumed.Succeeded);
        Assert.DoesNotContain(
            resumed.Events,
            item => item.EventType == HostedSessionEventTypes.WarningIssued);
    }

    private PostgresHostedSessionExpirySweep CreateExpirySweep(
        Guid actorId,
        PostgresSessionRuntimeRepository repository,
        IAuthenticatedWorkloadContextSource? workloadIdentity = null) =>
        new(
            Fixture.Services.ConnectionAccessor,
            new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor),
            repository,
            new PostgresHostedFrozenTimingDocumentSource(Fixture.Services.ConnectionAccessor),
            new PostgresSessionLifecycleCoordinator(
                Fixture.Services.ConnectionAccessor,
                repository,
                new ChangeSessionLifecycleHandler()),
            new HostedSessionExpirySettings(
                SessionPersistenceFixtures.Actor(actorId),
                HostedSessionExpiryChannels.Service),
            workloadIdentity: workloadIdentity);

    private static Task InsertClosedPauseIntervalAsync(
        NpgsqlTransaction transaction,
        SessionOwnership ownership,
        DateTimeOffset pauseStartedAt,
        DateTimeOffset pauseEndedAt) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_pause_intervals (
                    organization_id,
                    activity_id,
                    participant_id,
                    attempt_id,
                    session_id,
                    pause_id,
                    started_at,
                    ended_at,
                    last_committed_at)
                VALUES (
                    @OrganizationId,
                    @ActivityId,
                    @ParticipantId,
                    @AttemptId,
                    @SessionId,
                    @PauseId,
                    @StartedAt,
                    @EndedAt,
                    @EndedAt)
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    PauseId = Guid.CreateVersion7(),
                    StartedAt = pauseStartedAt,
                    EndedAt = pauseEndedAt,
                },
                transaction));

    private static Task InsertOpenPauseIntervalAsync(
        NpgsqlTransaction transaction,
        SessionOwnership ownership,
        DateTimeOffset pauseStartedAt) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_pause_intervals (
                    organization_id,
                    activity_id,
                    participant_id,
                    attempt_id,
                    session_id,
                    pause_id,
                    started_at,
                    ended_at,
                    last_committed_at)
                VALUES (
                    @OrganizationId,
                    @ActivityId,
                    @ParticipantId,
                    @AttemptId,
                    @SessionId,
                    @PauseId,
                    @StartedAt,
                    NULL,
                    @StartedAt)
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    PauseId = Guid.CreateVersion7(),
                    StartedAt = pauseStartedAt,
                },
                transaction));

    private static HostedFrozenTimingPolicy TimedPolicy(DateTimeOffset hardEndAtUtc, int budgetSeconds) =>
        new(
            HostedTimingReconstruction.Timed,
            budgetSeconds,
            [
                new HostedTimingWarningThreshold("approaching", Math.Max(60, budgetSeconds / 4)),
                new HostedTimingWarningThreshold("imminent", Math.Max(30, budgetSeconds / 8)),
            ],
            hardEndAtUtc);

    private static Task BackdateSessionStartAsync(
        NpgsqlTransaction transaction,
        SessionOwnership ownership,
        DateTimeOffset startedAt) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_runtimes
                SET created_at = @StartedAt,
                    last_committed_at = @StartedAt
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    StartedAt = startedAt,
                },
                transaction));

    private static Task InsertFrozenTimingAsync(
        NpgsqlTransaction transaction,
        SessionOwnership ownership,
        HostedFrozenTimingPolicy policy) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_frozen_timing (
                    organization_id, session_id, document, created_at)
                VALUES (
                    @OrganizationId,
                    @SessionId,
                    CAST(@Document AS jsonb),
                    NOW())
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    Document = HostedSessionFrozenTiming.ToDocumentJson(policy),
                },
                transaction));

    private sealed record WarningOccurrenceRow(
        string WarningThresholdId,
        string WarningCode,
        int RemainingSecondsThreshold,
        DateTimeOffset DueAt,
        DateTimeOffset CommittedAt,
        long SessionSequence,
        int RemainingSecondsAtCommit,
        string DeliveryStatus);
}
