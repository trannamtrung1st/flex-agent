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
    public async Task Concurrent_equivalent_requests_return_the_same_baseline()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var second = harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.Succeeded, result.OutcomeCode));
        Assert.Equal(results[0].BaselineId, results[1].BaselineId);
    }

    [Fact]
    public async Task Failed_activation_persists_an_attempt_and_audit_without_activating()
    {
        var harness = await SeedReadyHarnessAsync();
        var stale = harness.Command() with { ExpectedRevisionNumber = harness.RevisionNumber + 1 };
        stale = stale with { TrustedCommandDigest = harness.Digest.Compute(stale) };
        var outcome = await harness.Coordinator.ActivateAsync(stale, CancellationToken);

        Assert.False(outcome.Succeeded);
        Assert.Equal(AssessmentFailureCodes.StaleRevision, outcome.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var attemptOutcome = await connection.ExecuteScalarAsync<string>(
            """
            SELECT outcome_code
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND cohort_id = @CohortId AND idempotency_key = 'idem-1'
            """,
            new { harness.OrganizationId, harness.CohortId });
        var actorId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT actor_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND outcome = 'deny'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new { harness.OrganizationId, Action = AssessmentAuthorizationActions.ActivateCohort });
        var activated = await connection.ExecuteScalarAsync<string>(
            "SELECT state FROM assessment_cohorts WHERE organization_id = @OrganizationId AND cohort_id = @CohortId",
            new { harness.OrganizationId, harness.CohortId });

        Assert.Equal(AssessmentFailureCodes.StaleRevision, attemptOutcome);
        Assert.Equal(harness.Actor.Actor.ActorId, actorId);
        Assert.Equal(CohortStates.Draft, activated);
        var requestedNumber = await connection.ExecuteScalarAsync<long>(
            """
            SELECT requested_revision_number
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND cohort_id = @CohortId AND idempotency_key = 'idem-1'
            """,
            new { harness.OrganizationId, harness.CohortId });
        var authoritativeNumber = await connection.ExecuteScalarAsync<long>(
            """
            SELECT authoritative_revision_number
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND cohort_id = @CohortId AND idempotency_key = 'idem-1'
            """,
            new { harness.OrganizationId, harness.CohortId });
        Assert.Equal(harness.RevisionNumber + 1, requestedNumber);
        Assert.Equal(harness.RevisionNumber, authoritativeNumber);
    }

    [Fact]
    public async Task Early_mfa_failure_retry_returns_the_stored_attempt()
    {
        var harness = await SeedReadyHarnessAsync();
        harness = harness with
        {
            Actor = harness.Actor with { Strength = new AuthenticationStrength(null, []) },
        };
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var retry = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var attemptCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND cohort_id = @CohortId AND idempotency_key = 'idem-1'
            """,
            new { harness.OrganizationId, harness.CohortId });

        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, first.OutcomeCode);
        Assert.Equal(first.OutcomeCode, retry.OutcomeCode);
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task Concurrent_reconcile_does_not_report_denied_against_an_activated_cohort()
    {
        var harness = await SeedReadyHarnessAsync();
        var activate = harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        ActivationOutcome? inconsistent = null;
        while (!activate.IsCompleted)
        {
            var reconciled = await harness.Coordinator.ReconcileAsync(
                new ReconcileActivationQuery(harness.Actor, harness.ActivityId, harness.CohortId, "idem-1"),
                CancellationToken);
            if (!reconciled.Succeeded
                && string.Equals(reconciled.CohortState, CohortStates.Activated, StringComparison.Ordinal))
            {
                inconsistent = reconciled;
                break;
            }
        }

        var completed = await activate;
        var final = await harness.Coordinator.ReconcileAsync(
            new ReconcileActivationQuery(harness.Actor, harness.ActivityId, harness.CohortId, "idem-1"),
            CancellationToken);

        Assert.True(completed.Succeeded, completed.OutcomeCode);
        Assert.Null(inconsistent);
        Assert.True(final.Succeeded, final.OutcomeCode);
        Assert.Equal(completed.BaselineId, final.BaselineId);
    }

    [Fact]
    public async Task Successful_activation_audit_uses_the_trusted_actor()
    {
        var harness = await SeedReadyHarnessAsync();
        Assert.True((await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken)).Succeeded);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var actorId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT actor_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_type = @ResourceType
              AND outcome = 'permit'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new { harness.OrganizationId, ResourceType = AssessmentResourceTypes.Baseline });
        var correlation = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT correlation_id
            FROM assessment_activation_baselines
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { harness.OrganizationId, harness.ActivityId });

        Assert.Equal(harness.Actor.Actor.ActorId, actorId);
        Assert.Equal(harness.Actor.CorrelationId, correlation);
        var aggregateId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT aggregate_id
            FROM outbox_items
            WHERE organization_id = @OrganizationId
              AND event_type = 'assessment.cohort.activated'
            ORDER BY created_at DESC
            LIMIT 1
            """,
            new { harness.OrganizationId });
        Assert.Equal(harness.CohortId, aggregateId);
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
            AssessmentAuthorizationActions.ReadActivity);
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
            store,
            new PostgresAssessmentUnitOfWork(connections));
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
            new PostgresAssessmentAttemptStore(new PostgresAuditEventWriter()));

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
