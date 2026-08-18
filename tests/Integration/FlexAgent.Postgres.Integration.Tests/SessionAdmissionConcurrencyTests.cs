using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionAdmissionConcurrencyTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Waiting_admission_samples_clock_after_session_lock()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await using var holdingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await holdingConnection.OpenAsync(CancellationToken);
        await using var holdingTransaction = await holdingConnection.BeginTransactionAsync(CancellationToken);
        var lockedSession = await holdingConnection.ExecuteScalarAsync<Guid>(
            new CommandDefinition(
                """
                SELECT session_id
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId
                FOR UPDATE;
                """,
                binding.Ownership,
                holdingTransaction,
                cancellationToken: CancellationToken));
        Assert.Equal(binding.Ownership.SessionId, lockedSession);

        var waitingAdmission = Task.Run(
            () => coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
                    binding.Ownership,
                    ExpectedSessionVersion: 0,
                    SessionPersistenceFixtures.OpeningTrigger(),
                    "idem.opening.lock-wait",
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken),
            CancellationToken);

        await WaitUntilBlockedOnThisBackendAsync(holdingConnection, holdingTransaction, CancellationToken);
        Assert.False(waitingAdmission.IsCompleted);

        await holdingConnection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_runtimes
                SET lifecycle_state = lifecycle_state
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId;
                """,
                binding.Ownership,
                holdingTransaction,
                cancellationToken: CancellationToken));
        await holdingTransaction.CommitAsync(CancellationToken);

        var result = await waitingAdmission.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.True(result.Succeeded);
    }

    private static async Task WaitUntilBlockedOnThisBackendAsync(
        NpgsqlConnection holdingConnection,
        NpgsqlTransaction holdingTransaction,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var waiters = await holdingConnection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT COUNT(*)::int
                    FROM pg_locks blocked
                    JOIN pg_locks holder
                      ON holder.locktype = blocked.locktype
                     AND holder.database IS NOT DISTINCT FROM blocked.database
                     AND holder.relation IS NOT DISTINCT FROM blocked.relation
                     AND holder.page IS NOT DISTINCT FROM blocked.page
                     AND holder.tuple IS NOT DISTINCT FROM blocked.tuple
                     AND holder.virtualxid IS NOT DISTINCT FROM blocked.virtualxid
                     AND holder.transactionid IS NOT DISTINCT FROM blocked.transactionid
                     AND holder.classid IS NOT DISTINCT FROM blocked.classid
                     AND holder.objid IS NOT DISTINCT FROM blocked.objid
                     AND holder.objsubid IS NOT DISTINCT FROM blocked.objsubid
                     AND holder.pid <> blocked.pid
                    WHERE NOT blocked.granted
                      AND holder.granted
                      AND holder.pid = pg_backend_pid();
                    """,
                    transaction: holdingTransaction,
                    cancellationToken: cancellationToken));
            if (waiters > 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for a PostgreSQL lock waiter on this backend.");
    }
}
