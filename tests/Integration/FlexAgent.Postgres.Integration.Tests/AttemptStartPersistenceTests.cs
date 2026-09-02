using Dapper;
using FlexAgent.AssessmentConfiguration.Application;
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
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        await using var lookup = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var enrollment = await lookup.QuerySingleAsync<(Guid BaselineId, Guid TaskSourceId, string TaskDigest)>(
            """
            SELECT baseline_id, task_source_id, task_content_digest
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, harness.EnrollmentId });
        var connections = Fixture.Services.ConnectionAccessor;
        var unitOfWork = new PostgresEnrollmentUnitOfWork(
            connections,
            new AllowEnrollmentSessionPort());
        var actor = harness.ParticipantActor;
        var port = new FlexAgent.Api.GatedP0SessionStartPort(
            new DevelopmentHostEnvironment(),
            SessionPersistenceFixtures.RuntimeRepository());
        var scope = new SubmissionParentScope(
            harness.OrganizationId,
            harness.ActivityId,
            harness.CohortId,
            enrollment.BaselineId,
            harness.EnrollmentId,
            harness.ParticipantId,
            enrollment.TaskSourceId,
            Guid.CreateVersion7(),
            enrollment.TaskDigest);
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
            new { OrganizationId = harness.OrganizationId, SessionId = request.SessionId });
        var resolvedDigest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT configuration_digest
            FROM session_resolved_configurations
            WHERE organization_id = @OrganizationId AND configuration_id = @ConfigurationId
            """,
            new { OrganizationId = harness.OrganizationId, ConfigurationId = request.ConfigurationId });
        var manifestConfigurationId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT configuration_id
            FROM session_initial_manifests
            WHERE organization_id = @OrganizationId AND manifest_id = @ManifestId
            """,
            new { OrganizationId = harness.OrganizationId, ManifestId = request.ManifestId });
        var policyDigest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT policy_digest
            FROM session_frozen_policy_snapshots
            WHERE organization_id = @OrganizationId AND session_id = @SessionId
            """,
            new { OrganizationId = harness.OrganizationId, SessionId = request.SessionId });
        Assert.Equal(committed.ConfigurationDigest, runtimeDigest);
        Assert.Equal(committed.ConfigurationDigest, resolvedDigest);
        Assert.Equal(request.ConfigurationId, manifestConfigurationId);
        Assert.NotEqual(committed.ConfigurationDigest, policyDigest);
        var loaded = await new FlexAgent.Sessions.Infrastructure.PostgresTrustedSessionBindingSource(connections)
            .GetAsync(
                new FlexAgent.Sessions.Domain.SessionOwnership(
                    harness.OrganizationId,
                    scope.ActivityId,
                    harness.ParticipantId,
                    request.AttemptId,
                    request.SessionId),
                CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(committed.ConfigurationDigest, loaded!.ConfigurationDigest);
        Assert.Equal(policyDigest, loaded.Policy.PolicyDigest);
    }

    [Fact]
    public async Task Production_environment_refuses_session_start_commit()
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
            new DevelopmentHostEnvironment { EnvironmentName = Microsoft.Extensions.Hosting.Environments.Production },
            SessionPersistenceFixtures.RuntimeRepository());
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

        Assert.False(committed.Succeeded);
        Assert.Equal(AttemptFailureCodes.Unavailable, committed.OutcomeCode);
        Assert.False(port.CanCommit);
        await using var connection = await connections.OpenConnectionAsync(CancellationToken);
        var runtimes = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM session_runtimes WHERE organization_id = @OrganizationId",
            new { OrganizationId = seeded.OrganizationId });
        Assert.Equal(0, runtimes);
    }

    [Fact]
    public async Task Development_commit_without_frozen_baseline_sources_fails_closed()
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
            SessionPersistenceFixtures.RuntimeRepository());
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

        Assert.False(committed.Succeeded);
        Assert.Equal(AttemptFailureCodes.Unavailable, committed.OutcomeCode);
        await using var connection = await connections.OpenConnectionAsync(CancellationToken);
        var runtimes = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM session_runtimes WHERE organization_id = @OrganizationId",
            new { OrganizationId = seeded.OrganizationId });
        Assert.Equal(0, runtimes);
    }

    [Fact]
    public async Task Development_commit_writes_frozen_baseline_source_identities()
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var enrollment = await connection.QuerySingleAsync<(Guid BaselineId, Guid TaskSourceId, string TaskDigest)>(
            """
            SELECT baseline_id, task_source_id, task_content_digest
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, harness.EnrollmentId });
        var connections = Fixture.Services.ConnectionAccessor;
        var unitOfWork = new PostgresEnrollmentUnitOfWork(
            connections,
            new AllowEnrollmentSessionPort());
        var port = new FlexAgent.Api.GatedP0SessionStartPort(
            new DevelopmentHostEnvironment(),
            SessionPersistenceFixtures.RuntimeRepository());
        var scope = new SubmissionParentScope(
            harness.OrganizationId,
            harness.ActivityId,
            harness.CohortId,
            enrollment.BaselineId,
            harness.EnrollmentId,
            harness.ParticipantId,
            enrollment.TaskSourceId,
            Guid.CreateVersion7(),
            enrollment.TaskDigest);
        var request = new SessionStartCommitRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            scope,
            [new AttemptSubmissionBinding(Guid.CreateVersion7(), 1, 1, new string('b', 64))],
            DateTimeOffset.UtcNow);

        var committed = await unitOfWork.ExecuteAsync(
            harness.ParticipantActor,
            transaction => port.CommitActiveAsync(request, transaction.CommitHandle, CancellationToken),
            CancellationToken);

        Assert.True(committed.Succeeded, committed.OutcomeCode);
        var canonical = await connection.ExecuteScalarAsync<string>(
            """
            SELECT canonical_json
            FROM session_resolved_configurations
            WHERE organization_id = @OrganizationId AND configuration_id = @ConfigurationId
            """,
            new { harness.OrganizationId, request.ConfigurationId });
        Assert.Contains(AssessmentDevelopmentSources.OrganizationPolicy.SourceId.ToString("D"), canonical, StringComparison.Ordinal);
        Assert.Contains(AssessmentDevelopmentSources.Workflow.SourceId.ToString("D"), canonical, StringComparison.Ordinal);
        Assert.Contains(AssessmentDevelopmentSources.Agent.ContentDigest, canonical, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Development_commit_fails_when_registered_source_digest_drifts()
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var enrollment = await connection.QuerySingleAsync<(Guid BaselineId, Guid TaskSourceId, string TaskDigest)>(
            """
            SELECT baseline_id, task_source_id, task_content_digest
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, harness.EnrollmentId });
        var driftedBaselineId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO assessment_activation_baselines (
                organization_id, activity_id, baseline_id, content_digest, procedure_id,
                schema_version, canonicalization_version, document, created_at,
                actor_id, correlation_id)
            SELECT organization_id, activity_id, @DriftedBaselineId, content_digest, procedure_id,
                   schema_version, canonicalization_version,
                   replace(document::text, @WorkflowDigest, @DriftedDigest)::jsonb,
                   CLOCK_TIMESTAMP(), actor_id, correlation_id
            FROM assessment_activation_baselines
            WHERE organization_id = @OrganizationId AND baseline_id = @BaselineId
            """,
            new
            {
                harness.OrganizationId,
                enrollment.BaselineId,
                DriftedBaselineId = driftedBaselineId,
                WorkflowDigest = AssessmentDevelopmentSources.Workflow.ContentDigest,
                DriftedDigest = new string('f', 64),
            });
        var connections = Fixture.Services.ConnectionAccessor;
        var unitOfWork = new PostgresEnrollmentUnitOfWork(
            connections,
            new AllowEnrollmentSessionPort());
        var port = new FlexAgent.Api.GatedP0SessionStartPort(
            new DevelopmentHostEnvironment(),
            SessionPersistenceFixtures.RuntimeRepository());
        var scope = new SubmissionParentScope(
            harness.OrganizationId,
            harness.ActivityId,
            harness.CohortId,
            driftedBaselineId,
            harness.EnrollmentId,
            harness.ParticipantId,
            enrollment.TaskSourceId,
            Guid.CreateVersion7(),
            enrollment.TaskDigest);
        var request = new SessionStartCommitRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            scope,
            [new AttemptSubmissionBinding(Guid.CreateVersion7(), 1, 1, new string('b', 64))],
            DateTimeOffset.UtcNow);

        var committed = await unitOfWork.ExecuteAsync(
            harness.ParticipantActor,
            transaction => port.CommitActiveAsync(request, transaction.CommitHandle, CancellationToken),
            CancellationToken);

        Assert.False(committed.Succeeded);
        Assert.Equal(AttemptFailureCodes.Unavailable, committed.OutcomeCode);
        var runtimes = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM session_runtimes WHERE organization_id = @OrganizationId",
            new { harness.OrganizationId });
        Assert.Equal(0, runtimes);
    }

    [Fact]
    public async Task Same_start_key_advisory_lock_blocks_until_the_holder_commits()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var enrollmentId = Guid.CreateVersion7();
        const string key = "attempt-start-synthetic-0001";
        var store = new PostgresStartOperationStore();
        await using var holdingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await using var waitingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await holdingConnection.OpenAsync(CancellationToken);
        await waitingConnection.OpenAsync(CancellationToken);
        await using var holdingTransaction = await holdingConnection.BeginTransactionAsync(CancellationToken);
        await store.AcquireLockAsync(
            seeded.OrganizationId,
            enrollmentId,
            key,
            new AttachedPostgresEnrollmentTransaction(holdingTransaction),
            CancellationToken);

        var waitingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waitingAcquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = Task.Run(async () =>
        {
            await using var waitingTransaction = await waitingConnection.BeginTransactionAsync(CancellationToken);
            waitingStarted.TrySetResult();
            await store.AcquireLockAsync(
                seeded.OrganizationId,
                enrollmentId,
                key,
                new AttachedPostgresEnrollmentTransaction(waitingTransaction),
                CancellationToken);
            waitingAcquired.TrySetResult();
            await waitingTransaction.RollbackAsync(CancellationToken);
        }, CancellationToken);

        await waitingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(waitingAcquired.Task.IsCompleted);

        await holdingTransaction.CommitAsync(CancellationToken);
        await waiting.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        Assert.True(waitingAcquired.Task.IsCompletedSuccessfully);
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
