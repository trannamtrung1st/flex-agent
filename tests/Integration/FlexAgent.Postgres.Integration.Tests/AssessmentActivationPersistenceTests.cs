using Dapper;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AssessmentActivationPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Activation_marks_the_activity_head_without_inserting_a_revision()
    {
        var harness = await SeedReadyHarnessAsync();
        var outcome = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);

        Assert.True(outcome.Succeeded, outcome.OutcomeCode);
        Assert.Equal("assessment.activated", outcome.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var revisionCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM assessment_activity_revisions
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { harness.OrganizationId, harness.ActivityId });
        var activated = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT has_activated_cohort
            FROM assessment_activities
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { harness.OrganizationId, harness.ActivityId });

        Assert.Equal(1, revisionCount);
        Assert.True(activated);
    }

    [Fact]
    public async Task Equivalent_retry_returns_the_stored_attempt_and_competing_key_conflicts()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var retry = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var competing = await harness.Coordinator.ActivateAsync(harness.Command("idem-2"), CancellationToken);
        var reconciled = await harness.Coordinator.ReconcileAsync(
            new ReconcileActivationQuery(harness.Actor, harness.ActivityId, harness.CohortId, "idem-1"),
            CancellationToken);

        Assert.True(first.Succeeded);
        Assert.True(retry.Succeeded);
        Assert.Equal(first.BaselineId, retry.BaselineId);
        Assert.Equal(AssessmentFailureCodes.ConcurrentActivation, competing.OutcomeCode);
        Assert.Equal(first.BaselineId, reconciled.BaselineId);
    }

    [Fact]
    public async Task Parent_traversal_rejects_a_cohort_bound_to_another_activity_revision()
    {
        var first = await SeedReadyHarnessAsync();
        var second = await SeedReadyHarnessAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                """
                UPDATE assessment_cohorts
                SET bound_revision_id = @ForeignRevisionId
                WHERE organization_id = @OrganizationId AND cohort_id = @CohortId
                """,
                new
                {
                    ForeignRevisionId = first.RevisionId,
                    second.OrganizationId,
                    second.CohortId,
                }));

        Assert.Equal("23503", exception.SqlState);
    }

    private async Task<Harness> SeedReadyHarnessAsync()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            seeded.ActorId,
            AssessmentAuthorizationActions.CreateActivity);
        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            seeded.ActorId,
            AssessmentAuthorizationActions.ActivateCohort);
        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            seeded.ActorId,
            AssessmentAuthorizationActions.ReconcileActivation);
        await SeedDescriptorsAsync(seeded.OrganizationId);

        var connections = Fixture.Services.ConnectionAccessor;
        var kernel = new PostgresAuthorizationKernel(connections);
        var store = new PostgresAssessmentDraftStore(connections);
        var catalog = new PostgresAssessmentSourceCatalog(connections);
        var drafts = new AssessmentDraftHandler(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            catalog,
            store);
        var actor = new AssessmentActorContext(
            seeded.Actor,
            seeded.Scope,
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https");
        var created = await drafts.CreateAsync(
            new CreateAssessmentDraftCommand(
                actor,
                "P0 Assessment",
                new TaskBinding(
                    Guid.CreateVersion7(),
                    "Task 1",
                    "Submit one written response",
                    AssessmentDevelopmentSources.TaskRequirement),
                new TimingRules(
                    new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
                    "UTC",
                    2,
                    3600),
                AssessmentDevelopmentSources.OrganizationPolicy,
                AssessmentDevelopmentSources.Agent,
                AssessmentDevelopmentSources.Harness,
                AssessmentDevelopmentSources.Workflow,
                AssessmentDevelopmentSources.AdaptiveFollowUp,
                AssessmentDevelopmentSources.Rubric,
                AssessmentDevelopmentSources.ModelDeployment,
                [AssessmentDevelopmentSources.Knowledge],
                AssessmentDevelopmentSources.Capability,
                AssessmentDevelopmentSources.ReviewRelease),
            CancellationToken);
        Assert.True(created.Succeeded, created.OutcomeCode);
        var cohort = await store.FindCohortForActivityAsync(seeded.OrganizationId, created.Value!.ActivityId, CancellationToken);
        Assert.NotNull(cohort);

        var coordinator = new AssessmentActivationCoordinator(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            catalog,
            store,
            new PostgresAssessmentUnitOfWork(connections),
            new ActivationBaselineDigester(),
            new AssessmentCommandDigest(),
            new PostgresAssessmentBaselineStore(new PostgresAuditEventWriter(), new PostgresOutboxItemWriter()),
            new PostgresAssessmentAttemptStore());

        return new Harness(
            coordinator,
            new AssessmentCommandDigest(),
            actor,
            seeded.OrganizationId,
            created.Value.ActivityId,
            cohort.CohortId,
            created.Value.RevisionId,
            created.Value.RevisionNumber);
    }

    private async Task SeedDescriptorsAsync(Guid organizationId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        foreach (var source in AssessmentDevelopmentSources.ForOrganization(organizationId))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO configuration_sources (id, organization_id, source_kind, created_at)
                VALUES (@SourceId, @OrganizationId, @SourceKind, CLOCK_TIMESTAMP())
                ON CONFLICT DO NOTHING;
                INSERT INTO configuration_source_versions (
                    id, organization_id, configuration_source_id, schema_version, procedure_id,
                    content_digest, idempotency_key, created_at)
                VALUES (
                    @VersionId, @OrganizationId, @SourceId, 'v1', 'activation-baseline-jcs-sha256-v1',
                    @ContentDigest, @IdempotencyKey, CLOCK_TIMESTAMP());
                INSERT INTO configuration_source_readiness_descriptors (
                    organization_id, configuration_source_id, version_id, source_kind, category,
                    lifecycle_state, compatibility_key, capability_text_enabled, capability_voice_enabled,
                    capability_tools_enabled, capability_dynamic_memory_writes_enabled,
                    capability_shared_session_enabled, capability_direct_deployment_enabled,
                    production_eligible, transactionally_revalidatable, effective_values, created_at)
                VALUES (
                    @OrganizationId, @SourceId, @VersionId, @SourceKind, @Category, @Lifecycle,
                    @Compatibility, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, TRUE,
                    @EffectiveValues::jsonb, CLOCK_TIMESTAMP());
                """,
                new
                {
                    OrganizationId = organizationId,
                    source.SourceId,
                    source.VersionId,
                    source.SourceKind,
                    source.Category,
                    source.ContentDigest,
                    IdempotencyKey = source.VersionId.ToString("D"),
                    Lifecycle = source.LifecycleState,
                    Compatibility = source.CompatibilityKey,
                    EffectiveValues = """{"ref":"seeded"}""",
                });
        }
    }

    private sealed record Harness(
        AssessmentActivationCoordinator Coordinator,
        AssessmentCommandDigest Digest,
        AssessmentActorContext Actor,
        Guid OrganizationId,
        Guid ActivityId,
        Guid CohortId,
        Guid RevisionId,
        long RevisionNumber)
    {
        public ActivateCohortCommand Command(string idempotencyKey = "idem-1")
        {
            var command = new ActivateCohortCommand(
                Actor,
                ActivityId,
                CohortId,
                RevisionId,
                RevisionNumber,
                idempotencyKey,
                "pending",
                DeploymentEnvironments.Development);
            return command with { TrustedCommandDigest = Digest.Compute(command) };
        }
    }
}
