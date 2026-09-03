using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
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
/// recreate. Uses the Compose Worker service actor and the same
/// <see cref="PostgresHostedSessionExpirySweep"/> path the Worker loop invokes.
/// </summary>
public sealed class ComposeStackHostedExpiryProbeTests
{
    private const string ProbeEnabledVariable = "FLEXAGENT_COMPOSE_PROBE";
    private const string ProbeConnectionVariable = "FLEXAGENT_COMPOSE_PROBE_CONNECTION";
    private static readonly Guid ComposeWorkerServiceActorId =
        Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaae");

    [Fact]
    public async Task Compose_hosted_expiry_sweep_completes_due_active_session()
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
        var organization = await SeedOrganizationAsync(services);
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

        await using (var scope = await PostgresTransactionScope.BeginAsync(services.ConnectionAccessor, TestContext.Current.CancellationToken))
        {
            await SessionPersistenceFixtures.InsertActiveAsync(
                repository,
                expiredBinding.Ownership,
                expiredSession,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                TestContext.Current.CancellationToken,
                frozenTiming: expiredPolicy,
                seedDefaultFrozenTiming: false);
            await BackdateSessionStartAsync(scope.Transaction, expiredBinding.Ownership, expiredStartedAt);
            await scope.CommitAsync(TestContext.Current.CancellationToken);
        }

        var sweep = CreateComposeExpirySweep(services, repository);
        var expiredCount = await sweep.ExpireDueAsync(TestContext.Current.CancellationToken);
        Assert.True(expiredCount >= 1);

        await using (var connection = await services.ConnectionAccessor.OpenConnectionAsync(TestContext.Current.CancellationToken))
        {
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
                    expiredBinding.Ownership.OrganizationId,
                    expiredBinding.Ownership.SessionId,
                });
            Assert.Equal(TerminalReasonCategories.TimeExpiry, terminalReason);
        }
    }

    private static PostgresHostedSessionExpirySweep CreateComposeExpirySweep(
        ConfigurationServiceCollection.ServiceBundle services,
        PostgresSessionRuntimeRepository repository) =>
        new(
            services.ConnectionAccessor,
            new PostgresTrustedSessionBindingSource(services.ConnectionAccessor),
            repository,
            new PostgresHostedFrozenTimingDocumentSource(services.ConnectionAccessor),
            new PostgresSessionLifecycleCoordinator(
                services.ConnectionAccessor,
                repository,
                new ChangeSessionLifecycleHandler()),
            new HostedSessionExpirySettings(
                new TrustedRuntimeActor(ComposeWorkerServiceActorId, "worker.session_runtime"),
                HostedSessionExpiryChannels.Service));

    private static async Task<SeededOrganization> SeedOrganizationAsync(ConfigurationServiceCollection.ServiceBundle services)
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = await services.ConnectionAccessor.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO organizations (id, created_at) VALUES (@OrganizationId, @CreatedAt);
            INSERT INTO actors (id, created_at) VALUES (@ActorId, @CreatedAt);
            INSERT INTO actor_organization_grants (
                organization_id, actor_id, relationship_version, granted_action, created_at)
            VALUES (
                @OrganizationId, @ActorId, 1, @GrantedAction, @CreatedAt);
            INSERT INTO configuration_sources (id, organization_id, source_kind, created_at)
            VALUES (@SourceId, @OrganizationId, @SourceKind, @CreatedAt);
            """,
            new
            {
                OrganizationId = organizationId,
                ActorId = actorId,
                GrantedAction = AuthorizationActions.RegisterConfigurationSourceVersion,
                SourceId = sourceId,
                SourceKind = ConfigurationSourceKinds.SyntheticV1,
                CreatedAt = now,
            });

        return new SeededOrganization(
            organizationId,
            actorId,
            sourceId,
            new TrustedActor(actorId, "synthetic.compose_probe"),
            new OrganizationScope(organizationId));
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
