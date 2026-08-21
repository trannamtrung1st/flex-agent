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
        Assert.Equal(CohortStates.Activated, competing.CohortState);
        Assert.Equal(first.BaselineId, competing.BaselineId);
        Assert.Equal(first.BaselineId, reconciled.BaselineId);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var outcomes = (await connection.QueryAsync<string>(
            """
            SELECT outcome
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @CohortId
            ORDER BY sequence_number
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
                harness.CohortId,
            })).ToArray();
        Assert.Contains("permit", outcomes);
        Assert.Contains("deduplicated", outcomes);
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
        var actorType = await connection.ExecuteScalarAsync<string>(
            """
            SELECT actor_type
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND outcome = 'deny'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new { harness.OrganizationId, Action = AssessmentAuthorizationActions.ActivateCohort });
        var sourceChannel = await connection.ExecuteScalarAsync<string>(
            """
            SELECT source_channel
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND outcome = 'deny'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new { harness.OrganizationId, Action = AssessmentAuthorizationActions.ActivateCohort });
        Assert.Equal(harness.Actor.Actor.ActorType, actorType);
        Assert.Equal(harness.Actor.SourceChannel, sourceChannel);
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
    public async Task Early_mfa_failure_retry_revalidates_and_can_succeed()
    {
        var harness = await SeedReadyHarnessAsync();
        var denied = await harness.Coordinator.ActivateAsync(
            harness.Command() with { Actor = harness.Actor with { Strength = new AuthenticationStrength(null, []) } },
            CancellationToken);
        var retried = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var attemptCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND requested_cohort_id = @CohortId AND idempotency_key = 'idem-1'
            """,
            new { harness.OrganizationId, harness.CohortId });

        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, denied.OutcomeCode);
        Assert.True(retried.Succeeded, retried.OutcomeCode);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task Later_denied_request_cannot_rebind_an_idempotency_key()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = harness.Command();
        await harness.Coordinator.ActivateAsync(
            first with { Actor = harness.Actor with { Strength = new AuthenticationStrength(null, []) } },
            CancellationToken);

        var retargeted = harness.Command() with { ExpectedRevisionNumber = harness.RevisionNumber + 1 };
        retargeted = retargeted with { TrustedCommandDigest = harness.Digest.Compute(retargeted) };
        var poisoned = await harness.Coordinator.ActivateAsync(
            retargeted with { Actor = harness.Actor with { Strength = new AuthenticationStrength(null, []) } },
            CancellationToken);
        var conflicting = await harness.Coordinator.ActivateAsync(retargeted, CancellationToken);
        var recovered = await harness.Coordinator.ActivateAsync(first, CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var boundDigest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT command_digest
            FROM assessment_activation_operations
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId
              AND requested_cohort_id = @CohortId
              AND idempotency_key = 'idem-1'
            """,
            new { harness.OrganizationId, harness.ActivityId, harness.CohortId });

        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, poisoned.OutcomeCode);
        Assert.Equal(AssessmentFailureCodes.IdempotencyConflict, conflicting.OutcomeCode);
        Assert.True(recovered.Succeeded, recovered.OutcomeCode);
        Assert.Equal(first.TrustedCommandDigest, boundDigest);
    }

    [Fact]
    public async Task Guessed_cohort_denies_without_aborting_the_transaction()
    {
        var harness = await SeedReadyHarnessAsync();
        var guessed = harness.Command() with { CohortId = Guid.CreateVersion7() };
        guessed = guessed with { TrustedCommandDigest = harness.Digest.Compute(guessed) };

        var outcome = await harness.Coordinator.ActivateAsync(guessed, CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var attemptCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND requested_cohort_id = @CohortId
            """,
            new { harness.OrganizationId, CohortId = guessed.CohortId });
        var boundCohort = await connection.ExecuteScalarAsync<Guid?>(
            """
            SELECT cohort_id
            FROM assessment_activation_attempts
            WHERE organization_id = @OrganizationId AND requested_cohort_id = @CohortId
            """,
            new { harness.OrganizationId, CohortId = guessed.CohortId });
        var auditCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @CohortId
              AND outcome = 'deny'
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
                CohortId = guessed.CohortId,
            });

        Assert.Equal(AssessmentFailureCodes.Denied, outcome.OutcomeCode);
        Assert.Equal(1, attemptCount);
        Assert.Null(boundCohort);
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task Success_then_lost_mfa_does_not_replay_the_activation()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var denied = await harness.Coordinator.ActivateAsync(
            harness.Command() with { Actor = harness.Actor with { Strength = new AuthenticationStrength(null, []) } },
            CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(denied.Succeeded);
        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Success_then_revoked_activation_grant_does_not_disclose_the_baseline()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        await new PostgresGrantRepository(Fixture.Services.ConnectionAccessor).RevokeAsync(
            harness.OrganizationId,
            harness.Actor.Actor.ActorId,
            AssessmentAuthorizationActions.ActivateCohort,
            CancellationToken);
        var denied = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Success_then_revoked_grant_after_admission_does_not_disclose_the_baseline()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var connections = Fixture.Services.ConnectionAccessor;
        var kernel = new PostgresAuthorizationKernel(connections);
        var store = new PostgresAssessmentDraftStore(connections, new PostgresAuditEventWriter());
        var catalog = new PostgresAssessmentSourceCatalog(connections);
        var grants = new PostgresGrantRepository(connections);
        var coordinator = new AssessmentActivationCoordinator(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            catalog,
            store,
            new PostgresAssessmentUnitOfWork(connections),
            new ActivationBaselineDigester(),
            new AssessmentCommandDigest(),
            new PostgresAssessmentBaselineStore(new PostgresAuditEventWriter(), new PostgresOutboxItemWriter()),
            new RevokeActivateGrantAfterLockStore(
                new PostgresAssessmentAttemptStore(new PostgresAuditEventWriter()),
                () => grants.RevokeAsync(
                    harness.OrganizationId,
                    harness.Actor.Actor.ActorId,
                    AssessmentAuthorizationActions.ActivateCohort,
                    CancellationToken)));
        var denied = await coordinator.ActivateAsync(harness.Command(), CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
    }

    [Fact]
    public async Task Success_then_participant_relationship_does_not_disclose_the_baseline()
    {
        var harness = await SeedReadyHarnessAsync();
        var first = await harness.Coordinator.ActivateAsync(harness.Command(), CancellationToken);
        var denied = await harness.Coordinator.ActivateAsync(
            harness.Command() with
            {
                Actor = harness.Actor with { Relationship = "participant" },
            },
            CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentFailureCodes.Denied, denied.OutcomeCode);
        Assert.Null(denied.BaselineId);
        Assert.Null(denied.BaselineDigest);
        Assert.Equal(CohortStates.Draft, denied.CohortState);
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
        await using var grantConnection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await grantConnection.ExecuteAsync(
            """
            UPDATE actor_organization_grants
            SET relationship_version = 7
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action = @Action
            """,
            new
            {
                harness.OrganizationId,
                ActorId = harness.Actor.Actor.ActorId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
            });
        var grant = await grantConnection.QuerySingleAsync<(Guid GrantId, long Version)>(
            """
            SELECT grant_id, relationship_version
            FROM actor_organization_grants
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action = @Action
            """,
            new
            {
                harness.OrganizationId,
                ActorId = harness.Actor.Actor.ActorId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
            });
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
        var baselineAudit = await connection.QuerySingleAsync<(long? Version, string? ReferenceType, Guid? ReferenceId)>(
            """
            SELECT relationship_version, authorization_reference_type, authorization_reference_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_type = @ResourceType
              AND outcome = 'permit'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new { harness.OrganizationId, ResourceType = AssessmentResourceTypes.Baseline });
        var attemptAudit = await connection.QuerySingleAsync<(long? Version, string? ReferenceType, Guid? ReferenceId)>(
            """
            SELECT relationship_version, authorization_reference_type, authorization_reference_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_type = @ResourceType
              AND resource_id = @CohortId
              AND outcome = 'permit'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
                ResourceType = AssessmentResourceTypes.Cohort,
                harness.CohortId,
            });
        Assert.Equal(7, baselineAudit.Version);
        Assert.Equal(AuthorizationReferenceTypes.ActorOrganizationGrant, baselineAudit.ReferenceType);
        Assert.Equal(grant.GrantId, baselineAudit.ReferenceId);
        Assert.Equal(7, attemptAudit.Version);
        Assert.Equal(AuthorizationReferenceTypes.ActorOrganizationGrant, attemptAudit.ReferenceType);
        Assert.Equal(grant.GrantId, attemptAudit.ReferenceId);
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

    [Fact]
    public async Task Parent_traversal_rejects_a_previous_revision_from_another_activity()
    {
        var first = await SeedReadyHarnessAsync();
        var connections = Fixture.Services.ConnectionAccessor;
        var kernel = new PostgresAuthorizationKernel(connections);
        var store = new PostgresAssessmentDraftStore(connections, new PostgresAuditEventWriter());
        var drafts = new AssessmentDraftHandler(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            new PostgresAssessmentSourceCatalog(connections),
            store,
            new PostgresAssessmentUnitOfWork(connections));
        var second = await drafts.CreateAsync(
            new CreateAssessmentDraftCommand(
                first.Actor,
                "Other Activity",
                new TaskBinding(
                    Guid.CreateVersion7(),
                    "Task 2",
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
                AssessmentDevelopmentSources.ReviewRelease,
                DeploymentEnvironments.Development),
            CancellationToken);
        Assert.True(second.Succeeded, second.OutcomeCode);

        await using var connection = await connections.OpenConnectionAsync(CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await connection.ExecuteAsync(
                """
                INSERT INTO assessment_activity_revisions (
                    organization_id, activity_id, revision_id, revision_number, title, content, created_at,
                    previous_revision_id, actor_id, actor_type, correlation_id, change_category, saved_at)
                SELECT
                    organization_id,
                    activity_id,
                    @NewRevisionId,
                    revision_number + 1,
                    title,
                    content,
                    CLOCK_TIMESTAMP(),
                    @ForeignRevisionId,
                    actor_id,
                    actor_type,
                    correlation_id,
                    'saved',
                    CLOCK_TIMESTAMP()
                FROM assessment_activity_revisions
                WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
                """,
                new
                {
                    NewRevisionId = Guid.CreateVersion7(),
                    ForeignRevisionId = first.RevisionId,
                    first.OrganizationId,
                    ActivityId = second.Value!.ActivityId,
                }));

        Assert.Equal("23503", exception.SqlState);
    }

    [Fact]
    public async Task Save_denies_when_the_grant_is_revoked_after_source_locks()
    {
        var harness = await SeedReadyHarnessAsync();
        var connections = Fixture.Services.ConnectionAccessor;
        var kernel = new PostgresAuthorizationKernel(connections);
        var store = new PostgresAssessmentDraftStore(connections, new PostgresAuditEventWriter());
        var catalog = new PostgresAssessmentSourceCatalog(connections);
        var grants = new PostgresGrantRepository(connections);
        var drafts = new AssessmentDraftHandler(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            new RevokeAfterSelectableLockCatalog(
                catalog,
                () => grants.RevokeAsync(
                    harness.OrganizationId,
                    harness.Actor.Actor.ActorId,
                    AssessmentAuthorizationActions.SaveActivity,
                    CancellationToken)),
            store,
            new PostgresAssessmentUnitOfWork(connections));
        var current = await store.GetDraftAsync(harness.OrganizationId, harness.ActivityId, CancellationToken);
        Assert.NotNull(current);

        var saved = await drafts.SaveAsync(
            new SaveAssessmentDraftCommand(
                harness.Actor,
                harness.ActivityId,
                current.RevisionNumber,
                current.Content with { Title = "After revoke" },
                DeploymentEnvironments.Development),
            CancellationToken);
        var after = await store.GetDraftAsync(harness.OrganizationId, harness.ActivityId, CancellationToken);

        Assert.Equal(AssessmentFailureCodes.Denied, saved.OutcomeCode);
        Assert.Equal(current.RevisionNumber, after!.RevisionNumber);
        Assert.Equal(current.Content.Title, after.Content.Title);
    }

    [Fact]
    public async Task Invalid_idempotency_key_does_not_touch_activation_tables()
    {
        var harness = await SeedReadyHarnessAsync();
        var outcome = await harness.Coordinator.ActivateAsync(harness.Command("   "), CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var operations = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM assessment_activation_operations
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { harness.OrganizationId, harness.ActivityId });

        Assert.Equal(AssessmentFailureCodes.InvalidField, outcome.OutcomeCode);
        Assert.Equal(0, operations);
        var auditCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND reason_code = @Reason
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
                Reason = AssessmentFailureCodes.InvalidField,
            });
        Assert.Equal(1, auditCount);
        var relationshipVersion = await connection.ExecuteScalarAsync<long?>(
            """
            SELECT relationship_version
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND reason_code = @Reason
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.ActivateCohort,
                Reason = AssessmentFailureCodes.InvalidField,
            });
        Assert.Null(relationshipVersion);
    }

    [Fact]
    public async Task Create_and_save_persist_revision_provenance_and_audit()
    {
        var harness = await SeedReadyHarnessAsync();
        var connections = Fixture.Services.ConnectionAccessor;
        var store = new PostgresAssessmentDraftStore(connections, new PostgresAuditEventWriter());
        var current = await store.GetDraftAsync(harness.OrganizationId, harness.ActivityId, CancellationToken);
        Assert.NotNull(current);
        var drafts = new AssessmentDraftHandler(
            new KernelAssessmentAuthorizationPort(
                new PostgresAuthorizationKernel(connections),
                new PostgresAuthorizationKernel(connections)),
            new PostgresAssessmentSourceCatalog(connections),
            store,
            new PostgresAssessmentUnitOfWork(connections));
        await using var grantConnection = await connections.OpenConnectionAsync(CancellationToken);
        await grantConnection.ExecuteAsync(
            """
            UPDATE actor_organization_grants
            SET relationship_version = 7
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action IN (@SaveAction, @SelectAction)
            """,
            new
            {
                harness.OrganizationId,
                ActorId = harness.Actor.Actor.ActorId,
                SaveAction = AssessmentAuthorizationActions.SaveActivity,
                SelectAction = AssessmentAuthorizationActions.SelectSources,
            });
        var saveGrant = await grantConnection.QuerySingleAsync<(Guid GrantId, long Version)>(
            """
            SELECT grant_id, relationship_version
            FROM actor_organization_grants
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action = @Action
            """,
            new
            {
                harness.OrganizationId,
                ActorId = harness.Actor.Actor.ActorId,
                Action = AssessmentAuthorizationActions.SaveActivity,
            });

        var saved = await drafts.SaveAsync(
            new SaveAssessmentDraftCommand(
                harness.Actor,
                harness.ActivityId,
                current.RevisionNumber,
                current.Content with { Title = "Provenance" },
                DeploymentEnvironments.Development),
            CancellationToken);

        await using var connection = await connections.OpenConnectionAsync(CancellationToken);
        var created = await connection.QuerySingleAsync<(Guid? ActorId, string? Category, Guid? Previous)>(
            """
            SELECT actor_id, change_category, previous_revision_id
            FROM assessment_activity_revisions
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId AND revision_number = 1
            """,
            new { harness.OrganizationId, harness.ActivityId });
        var next = await connection.QuerySingleAsync<(Guid? ActorId, string? Category, Guid? Previous)>(
            """
            SELECT actor_id, change_category, previous_revision_id
            FROM assessment_activity_revisions
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId AND revision_number = 2
            """,
            new { harness.OrganizationId, harness.ActivityId });
        var createAudit = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @ActivityId
              AND outcome = 'permit'
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.CreateActivity,
                harness.ActivityId,
            });
        var saveAudit = await connection.QuerySingleAsync<(long Version, string? ReferenceType, Guid? ReferenceId)>(
            """
            SELECT relationship_version, authorization_reference_type, authorization_reference_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @ActivityId
              AND outcome = 'permit'
            ORDER BY sequence_number DESC
            LIMIT 1
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.SaveActivity,
                harness.ActivityId,
            });
        var selectSourceAudits = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*)
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @ActivityId
              AND outcome = 'permit'
            """,
            new
            {
                harness.OrganizationId,
                Action = AssessmentAuthorizationActions.SelectSources,
                harness.ActivityId,
            });

        Assert.True(saved.Succeeded, saved.OutcomeCode);
        Assert.Equal(harness.Actor.Actor.ActorId, created.ActorId);
        Assert.Equal(AssessmentRevisionChangeCategories.Created, created.Category);
        Assert.Null(created.Previous);
        Assert.Equal(harness.Actor.Actor.ActorId, next.ActorId);
        Assert.Equal(AssessmentRevisionChangeCategories.Saved, next.Category);
        Assert.Equal(current.RevisionId, next.Previous);
        Assert.Equal(1, createAudit);
        Assert.Equal(7, saveAudit.Version);
        Assert.Equal(AuthorizationReferenceTypes.ActorOrganizationGrant, saveAudit.ReferenceType);
        Assert.Equal(saveGrant.GrantId, saveAudit.ReferenceId);
        Assert.Equal(1, selectSourceAudits);
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
            AssessmentAuthorizationActions.SelectSources);
        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            seeded.ActorId,
            AssessmentAuthorizationActions.ReadActivity);
        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            seeded.ActorId,
            AssessmentAuthorizationActions.SaveActivity);
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
        var store = new PostgresAssessmentDraftStore(connections, new PostgresAuditEventWriter());
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
                AssessmentDevelopmentSources.ReviewRelease,
                DeploymentEnvironments.Development),
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

    private sealed class RevokeActivateGrantAfterLockStore(
        IAssessmentActivationAttemptStore inner,
        Func<Task> revoke) : IAssessmentActivationAttemptStore
    {
        public async Task AcquireIdempotencyLockAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            string idempotencyKey,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken)
        {
            await inner.AcquireIdempotencyLockAsync(
                organizationId,
                activityId,
                cohortId,
                idempotencyKey,
                transaction,
                cancellationToken);
            await revoke();
        }

        public Task<AssessmentActivationAttempt?> FindAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            string idempotencyKey,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken) =>
            inner.FindAsync(organizationId, activityId, cohortId, idempotencyKey, transaction, cancellationToken);

        public Task<AssessmentActivationAttempt?> FindSuccessfulAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            string idempotencyKey,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken) =>
            inner.FindSuccessfulAsync(organizationId, activityId, cohortId, idempotencyKey, transaction, cancellationToken);

        public Task InsertAsync(
            AssessmentActivationAttempt attempt,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken) =>
            inner.InsertAsync(attempt, transaction, cancellationToken);

        public Task InsertRequestAuditAsync(
            AssessmentActorContext actor,
            string action,
            Guid resourceId,
            string resourceType,
            string outcome,
            string? reasonCode,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken,
            AuthorizationDecision? authorization = null) =>
            inner.InsertRequestAuditAsync(
                actor,
                action,
                resourceId,
                resourceType,
                outcome,
                reasonCode,
                transaction,
                cancellationToken,
                authorization);

        public Task<string> BindCommandDigestAsync(
            Guid organizationId,
            Guid activityId,
            Guid requestedCohortId,
            string idempotencyKey,
            string commandDigest,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken) =>
            inner.BindCommandDigestAsync(
                organizationId,
                activityId,
                requestedCohortId,
                idempotencyKey,
                commandDigest,
                transaction,
                cancellationToken);
    }

    private sealed class RevokeAfterSelectableLockCatalog(
        PostgresAssessmentSourceCatalog inner,
        Func<Task> revoke) : IAssessmentSourceCatalog, IAssessmentSourceTransactionPort
    {
        public Task<IReadOnlyList<TrustedSourceDescriptor>> LoadExactAsync(
            Guid organizationId,
            IReadOnlyList<ExactSourceRef> references,
            CancellationToken cancellationToken) =>
            inner.LoadExactAsync(organizationId, references, cancellationToken);

        public Task<IReadOnlyList<TrustedSourceDescriptor>> ListSelectableAsync(
            Guid organizationId,
            string environment,
            CancellationToken cancellationToken) =>
            inner.ListSelectableAsync(organizationId, environment, cancellationToken);

        public Task<IReadOnlyList<TrustedSourceDescriptor>> LoadSelectableExactAsync(
            Guid organizationId,
            IReadOnlyList<ExactSourceRef> references,
            string environment,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken) =>
            LoadAndRevokeAsync(organizationId, references, environment, transaction, cancellationToken);

        public Task<IReadOnlyList<TrustedSourceDescriptor>> RevalidateExactAsync(
            Guid organizationId,
            IReadOnlyList<ExactSourceRef> references,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken) =>
            inner.RevalidateExactAsync(organizationId, references, transaction, cancellationToken);

        private async Task<IReadOnlyList<TrustedSourceDescriptor>> LoadAndRevokeAsync(
            Guid organizationId,
            IReadOnlyList<ExactSourceRef> references,
            string environment,
            IAssessmentActivationTransaction transaction,
            CancellationToken cancellationToken)
        {
            var rows = await inner.LoadSelectableExactAsync(
                organizationId,
                references,
                environment,
                transaction,
                cancellationToken);
            await revoke();
            return rows;
        }
    }
}
