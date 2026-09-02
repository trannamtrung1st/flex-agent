using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AttemptStartPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Session_insert_then_abort_leaves_no_runtime_or_configuration_row()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var connections = Fixture.Services.ConnectionAccessor;
        var unitOfWork = new PostgresEnrollmentUnitOfWork(
            connections,
            new AllowEnrollmentSessionPort());
        var actor = new EnrollmentActorContext(
            seeded.Actor,
            seeded.Scope,
            string.Empty,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https",
            [EnrollmentAuthorizationActions.Discover],
            Guid.CreateVersion7());
        var configurationId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var digest = new string('a', 64);

        var aborted = await unitOfWork.ExecuteAsync(
            actor,
            async transaction =>
            {
                var npgsql = (NpgsqlTransaction)transaction.CommitHandle;
                var connection = npgsql.Connection ?? throw new InvalidOperationException("commit.transaction.required");
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO session_runtimes (
                            organization_id, activity_id, participant_id, attempt_id, session_id,
                            configuration_id, configuration_digest, manifest_id, lifecycle_state,
                            session_version, session_sequence, cutoff_sequence)
                        VALUES (
                            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                            @ConfigurationId, @ConfigurationDigest, @ManifestId, 'active',
                            0, 0, NULL)
                        """,
                        new
                        {
                            OrganizationId = seeded.OrganizationId,
                            ActivityId = Guid.CreateVersion7(),
                            ParticipantId = Guid.CreateVersion7(),
                            AttemptId = Guid.CreateVersion7(),
                            SessionId = sessionId,
                            ConfigurationId = configurationId.ToString("D"),
                            ConfigurationDigest = digest,
                            ManifestId = Guid.CreateVersion7().ToString("D"),
                        },
                        npgsql,
                        cancellationToken: CancellationToken));
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO session_resolved_configurations (
                            organization_id, configuration_id, configuration_digest, canonical_json, created_at)
                        VALUES (@OrganizationId, @ConfigurationId, @ConfigurationDigest, '{}', CLOCK_TIMESTAMP())
                        """,
                        new
                        {
                            OrganizationId = seeded.OrganizationId,
                            ConfigurationId = configurationId,
                            ConfigurationDigest = digest,
                        },
                        npgsql,
                        cancellationToken: CancellationToken));
                transaction.AbortCommit();
                return true;
            },
            CancellationToken);

        Assert.True(aborted);
        await using var connection = await connections.OpenConnectionAsync(CancellationToken);
        var runtimes = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM session_runtimes WHERE organization_id = @OrganizationId",
            new { OrganizationId = seeded.OrganizationId });
        var configurations = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM session_resolved_configurations WHERE organization_id = @OrganizationId",
            new { OrganizationId = seeded.OrganizationId });
        Assert.Equal(0, runtimes);
        Assert.Equal(0, configurations);
    }

    [Fact]
    public async Task Successful_gated_start_shares_one_configuration_digest()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var connections = Fixture.Services.ConnectionAccessor;
        var unitOfWork = new PostgresEnrollmentUnitOfWork(
            connections,
            new AllowEnrollmentSessionPort());
        var actor = new EnrollmentActorContext(
            seeded.Actor,
            seeded.Scope,
            string.Empty,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https",
            [EnrollmentAuthorizationActions.Discover],
            Guid.CreateVersion7());
        var port = new FlexAgent.Api.GatedP0SessionStartPort(
            new DevelopmentHostEnvironment(),
            new FlexAgent.Sessions.Infrastructure.PostgresSessionRuntimeRepository());
        var scope = new SubmissionParentScope(
            seeded.OrganizationId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            seeded.ActorId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new string('a', 64));
        var request = new SessionStartCommitRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            scope,
            [new AttemptSubmissionBinding(Guid.CreateVersion7(), 1, 1, new string('b', 64))],
            DateTimeOffset.UtcNow);

        var committed = await unitOfWork.ExecuteAsync(
            actor,
            transaction => port.CommitActiveAsync(request, transaction.CommitHandle, CancellationToken),
            CancellationToken);

        Assert.True(committed.Succeeded, committed.OutcomeCode);
        await using var connection = await connections.OpenConnectionAsync(CancellationToken);
        var runtimeDigest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT configuration_digest
            FROM session_runtimes
            WHERE organization_id = @OrganizationId AND session_id = @SessionId
            """,
            new { OrganizationId = seeded.OrganizationId, SessionId = request.SessionId });
        var resolvedDigest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT configuration_digest
            FROM session_resolved_configurations
            WHERE organization_id = @OrganizationId AND configuration_id = @ConfigurationId
            """,
            new { OrganizationId = seeded.OrganizationId, ConfigurationId = request.ConfigurationId });
        var manifestConfigurationId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT configuration_id
            FROM session_initial_manifests
            WHERE organization_id = @OrganizationId AND manifest_id = @ManifestId
            """,
            new { OrganizationId = seeded.OrganizationId, ManifestId = request.ManifestId });
        var policyDigest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT policy_digest
            FROM session_frozen_policy_snapshots
            WHERE organization_id = @OrganizationId AND session_id = @SessionId
            """,
            new { OrganizationId = seeded.OrganizationId, SessionId = request.SessionId });
        Assert.Equal(committed.ConfigurationDigest, runtimeDigest);
        Assert.Equal(committed.ConfigurationDigest, resolvedDigest);
        Assert.Equal(request.ConfigurationId, manifestConfigurationId);
        Assert.NotEqual(committed.ConfigurationDigest, policyDigest);
        var loaded = await new FlexAgent.Sessions.Infrastructure.PostgresTrustedSessionBindingSource(connections)
            .GetAsync(
                new FlexAgent.Sessions.Domain.SessionOwnership(
                    seeded.OrganizationId,
                    scope.ActivityId,
                    seeded.ActorId,
                    request.AttemptId,
                    request.SessionId),
                CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(committed.ConfigurationDigest, loaded!.ConfigurationDigest);
        Assert.Equal(policyDigest, loaded.Policy.PolicyDigest);
    }

    private sealed class DevelopmentHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Microsoft.Extensions.Hosting.Environments.Development;

        public string ApplicationName { get; set; } = "flex-agent-tests";

        public string ContentRootPath { get; set; } = ".";

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
