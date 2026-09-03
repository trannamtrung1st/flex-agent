using Dapper;
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

        var sweep = new PostgresHostedSessionExpirySweep(
            Fixture.Services.ConnectionAccessor,
            new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor),
            repository,
            new PostgresHostedFrozenTimingDocumentSource(Fixture.Services.ConnectionAccessor),
            new PostgresSessionLifecycleCoordinator(
                Fixture.Services.ConnectionAccessor,
                repository,
                new ChangeSessionLifecycleHandler()),
            new HostedSessionExpirySettings(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                HostedSessionExpiryChannels.Service));
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
}
