using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

/// <summary>
/// Optional live probe against authenticated-browser Compose Postgres. Run via
/// <c>build/scripts/probe-compose-hosted-expiry-sweep.sh</c> after API/Worker
/// recreate. Inserts a due Session and waits for the <em>running</em> Worker
/// background loop to terminalize it; does not invoke
/// <see cref="PostgresHostedSessionExpirySweep"/> from the test process.
/// </summary>
public sealed class ComposeStackHostedExpiryProbeTests
{
    private const string ProbeEnabledVariable = "FLEXAGENT_COMPOSE_PROBE";
    private const string ProbeConnectionVariable = "FLEXAGENT_COMPOSE_PROBE_CONNECTION";
    private static readonly TimeSpan WorkerLoopWait = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Compose_worker_loop_expiry_sweep_completes_due_active_session()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(ProbeEnabledVariable),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Skip($"Set {ProbeEnabledVariable}=1 to probe the live Compose stack.");
        }

        var connectionString = Environment.GetEnvironmentVariable(ProbeConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip($"Set {ProbeConnectionVariable} to the Compose Postgres connection string.");
        }

        var services = ConfigurationServiceCollection.Create(connectionString);
        var probeContext = await ComposeProbeSubmissionSeed.SeedDueSessionAsync(
            services,
            TestContext.Current.CancellationToken);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var expiredStartedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var expiredSession = SessionRuntime.CreateActive(probeContext.Binding, expiredStartedAt);
        var expiredHardEnd = DateTimeOffset.UtcNow.AddMinutes(-1);
        var expiredPolicy = new HostedFrozenTimingPolicy(
            HostedTimingReconstruction.Unbounded,
            null,
            [],
            expiredHardEnd);

        await using (var scope = await PostgresTransactionScope.BeginAsync(services.ConnectionAccessor, TestContext.Current.CancellationToken))
        {
            await SessionPersistenceFixtures.InsertActiveAsync(
                repository,
                probeContext.Binding.Ownership,
                expiredSession,
                SessionPersistenceFixtures.Actor(probeContext.ParticipantActorId),
                scope.Transaction,
                TestContext.Current.CancellationToken,
                frozenTiming: expiredPolicy,
                seedDefaultFrozenTiming: false);
            await BackdateSessionStartAsync(scope.Transaction, probeContext.Binding.Ownership, expiredStartedAt);
            await scope.CommitAsync(TestContext.Current.CancellationToken);
        }

        var deadline = DateTimeOffset.UtcNow.Add(WorkerLoopWait);
        string lifecycle = "active";
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            await using var connection = await services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken);
            lifecycle = await connection.QuerySingleAsync<string>(
                """
                SELECT lifecycle_state
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                """,
                new
                {
                    probeContext.Binding.Ownership.OrganizationId,
                    probeContext.Binding.Ownership.SessionId,
                });
            if (string.Equals(lifecycle, "completed", StringComparison.Ordinal))
            {
                break;
            }
        }

        Assert.Equal("completed", lifecycle);

        await using (var connection = await services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken))
        {
            var terminalReason = await connection.QuerySingleOrDefaultAsync<string?>(
                """
                SELECT reason_category
                FROM session_terminal_records
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                ORDER BY committed_at DESC
                LIMIT 1
                """,
                new
                {
                    probeContext.Binding.Ownership.OrganizationId,
                    probeContext.Binding.Ownership.SessionId,
                });
            Assert.Equal(TerminalReasonCategories.TimeExpiry, terminalReason);

            var attemptStatus = await connection.QuerySingleAsync<string>(
                """
                SELECT status
                FROM submissions_attempts
                WHERE organization_id = @OrganizationId
                  AND attempt_id = @AttemptId
                """,
                new
                {
                    probeContext.Binding.Ownership.OrganizationId,
                    probeContext.Binding.Ownership.AttemptId,
                });
            Assert.Equal("completed", attemptStatus);
        }
    }

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

}
