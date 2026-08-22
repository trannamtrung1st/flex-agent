using Dapper;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EnrollmentPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Assignment_binds_the_activated_cohort_without_mutating_the_baseline()
    {
        var harness = await SeedActivatedAsync();
        var assigned = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-1"), CancellationToken);
        Assert.True(assigned.Succeeded, assigned.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var digest = await connection.ExecuteScalarAsync<string>(
            """
            SELECT content_digest
            FROM assessment_activation_baselines
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { OrganizationId = harness.OrganizationId, ActivityId = harness.ActivityId });
        var enrollment = await connection.QuerySingleAsync<(Guid BaselineId, string Status)>(
            """
            SELECT baseline_id, status
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { OrganizationId = harness.OrganizationId, EnrollmentId = assigned.EnrollmentId });
        Assert.Equal(digest, harness.BaselineDigest);
        Assert.Equal(EnrollmentStates.Active, enrollment.Status);
        Assert.Equal(assigned.EnrollmentId, await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT enrollment_id
            FROM submissions_enrollment_events
            WHERE organization_id = @OrganizationId
            LIMIT 1
            """,
            new { OrganizationId = harness.OrganizationId }));
    }

    [Fact]
    public async Task Terminal_assignment_can_create_a_new_enrollment_identity()
    {
        var harness = await SeedActivatedAsync();
        var first = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-1"), CancellationToken);
        var closed = await harness.Coordinator.MutateAsync(
            harness.LifecycleCommand(
                EnrollmentOperationKinds.Close,
                EnrollmentReasonCodes.ActivityOrEnrollmentEnd,
                first.EnrollmentId!.Value,
                first.Revision!.Value,
                "close-1"),
            CancellationToken);
        Assert.True(closed.Succeeded, closed.OutcomeCode);
        var second = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-2"), CancellationToken);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.NotEqual(first.EnrollmentId, second.EnrollmentId);
    }

    [Fact]
    public async Task Current_participant_list_is_empty_without_an_enrollment()
    {
        var harness = await SeedActivatedAsync();
        var store = new PostgresEnrollmentStore(Fixture.Services.ConnectionAccessor);
        var page = await store.ListCurrentForParticipantAsync(
            harness.OrganizationId,
            harness.ParticipantId,
            cursor: null,
            limit: 20,
            CancellationToken);
        Assert.Empty(page.Items);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Concurrent_same_cohort_assignments_deduplicate()
    {
        var harness = await SeedActivatedAsync();
        var results = await Task.WhenAll(
            harness.Coordinator.AssignAsync(harness.AssignCommand("assign-race-a"), CancellationToken),
            harness.Coordinator.AssignAsync(harness.AssignCommand("assign-race-b"), CancellationToken));

        Assert.All(results, result => Assert.True(result.Succeeded, result.OutcomeCode));
        Assert.Equal(results[0].EnrollmentId, results[1].EnrollmentId);
        Assert.Contains(results, result => result.OutcomeCode == EnrollmentOutcomes.Assigned);
        Assert.Contains(results, result => result.OutcomeCode is EnrollmentOutcomes.Assigned or EnrollmentOutcomes.Deduplicated);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { harness.OrganizationId, harness.ActivityId });
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_different_cohort_assignments_conflict_without_a_500()
    {
        var harness = await SeedActivatedAsync();
        var otherCohortId = await CloneActivatedCohortAsync(harness);
        var first = harness.Coordinator.AssignAsync(harness.AssignCommand("assign-a"), CancellationToken);
        var second = harness.Coordinator.AssignAsync(
            harness.AssignCommand("assign-b", otherCohortId),
            CancellationToken);
        var results = await Task.WhenAll(first, second);

        Assert.Contains(results, result => result.Succeeded && result.OutcomeCode == EnrollmentOutcomes.Assigned);
        Assert.Contains(results, result => !result.Succeeded && result.OutcomeCode == EnrollmentFailureCodes.Conflict);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId
              AND status IN ('active', 'suspended')
            """,
            new { harness.OrganizationId, harness.ActivityId });
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_lifecycle_commands_return_stale_revision_instead_of_500()
    {
        var harness = await SeedActivatedAsync();
        var assigned = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-1"), CancellationToken);
        Assert.True(assigned.Succeeded, assigned.OutcomeCode);
        var results = await Task.WhenAll(
            harness.Coordinator.MutateAsync(
                harness.LifecycleCommand(
                    EnrollmentOperationKinds.Suspend,
                    EnrollmentReasonCodes.TemporaryRestriction,
                    assigned.EnrollmentId!.Value,
                    assigned.Revision!.Value,
                    "suspend-a"),
                CancellationToken),
            harness.Coordinator.MutateAsync(
                harness.LifecycleCommand(
                    EnrollmentOperationKinds.Suspend,
                    EnrollmentReasonCodes.TemporaryRestriction,
                    assigned.EnrollmentId.Value,
                    assigned.Revision.Value,
                    "suspend-b"),
                CancellationToken));

        Assert.Contains(results, result => result.Succeeded && result.OutcomeCode == EnrollmentOutcomes.Suspended);
        Assert.Contains(results, result => !result.Succeeded && result.OutcomeCode == EnrollmentFailureCodes.StaleRevision);
    }

    [Fact]
    public async Task Degraded_baseline_verification_fails_assignment()
    {
        var harness = await SeedActivatedAsync();
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                UPDATE configuration_source_readiness_descriptors
                SET lifecycle_state = 'revoked'
                WHERE organization_id = @OrganizationId
                """,
                new { harness.OrganizationId });
        }

        var assigned = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-degraded"), CancellationToken);
        Assert.False(assigned.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Unavailable, assigned.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_source_revocation_fails_uncommitted_assignment()
    {
        var harness = await SeedActivatedAsync();
        await using var revokeConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await revokeConnection.OpenAsync(CancellationToken);
        await using var revokeTransaction = await revokeConnection.BeginTransactionAsync(CancellationToken);
        await revokeConnection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE configuration_source_readiness_descriptors
                SET lifecycle_state = 'revoked'
                WHERE organization_id = @OrganizationId
                """,
                new { harness.OrganizationId },
                revokeTransaction,
                cancellationToken: CancellationToken));

        var assignTask = harness.Coordinator.AssignAsync(harness.AssignCommand("assign-revoke"), CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(assignTask.IsCompleted);

        await revokeTransaction.CommitAsync(CancellationToken);
        var assigned = await assignTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        Assert.False(assigned.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Unavailable, assigned.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_eligibility_revocation_fails_uncommitted_assignment()
    {
        var harness = await SeedActivatedAsync();
        await using var revokeConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await revokeConnection.OpenAsync(CancellationToken);
        await using var revokeTransaction = await revokeConnection.BeginTransactionAsync(CancellationToken);
        await revokeConnection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE actor_organization_grants
                SET revoked_at = CLOCK_TIMESTAMP()
                WHERE organization_id = @OrganizationId
                  AND actor_id = @ParticipantId
                  AND granted_action = @Action
                  AND revoked_at IS NULL
                """,
                new
                {
                    harness.OrganizationId,
                    harness.ParticipantId,
                    Action = EnrollmentAuthorizationActions.Receive,
                },
                revokeTransaction,
                cancellationToken: CancellationToken));

        var assignTask = harness.Coordinator.AssignAsync(harness.AssignCommand("assign-ineligible"), CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(assignTask.IsCompleted);

        await revokeTransaction.CommitAsync(CancellationToken);
        var assigned = await assignTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        Assert.False(assigned.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Ineligible, assigned.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_session_revocation_fails_uncommitted_assignment()
    {
        var harness = await SeedActivatedAsync();
        await using var revokeConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await revokeConnection.OpenAsync(CancellationToken);
        await using var revokeTransaction = await revokeConnection.BeginTransactionAsync(CancellationToken);
        await revokeConnection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE application_sessions
                SET revoked_at = CLOCK_TIMESTAMP(), terminal_reason = 'test.revoke'
                WHERE application_session_id = @ApplicationSessionId
                """,
                new { harness.ApplicationSessionId },
                revokeTransaction,
                cancellationToken: CancellationToken));

        var assignTask = harness.Coordinator.AssignAsync(harness.AssignCommand("assign-session"), CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(assignTask.IsCompleted);

        await revokeTransaction.CommitAsync(CancellationToken);
        var assigned = await assignTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        Assert.False(assigned.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Denied, assigned.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_profile_deletion_fails_uncommitted_assignment()
    {
        var harness = await SeedActivatedAsync();
        await using var deleteConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await deleteConnection.OpenAsync(CancellationToken);
        await using var deleteTransaction = await deleteConnection.BeginTransactionAsync(CancellationToken);
        await deleteConnection.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE FROM identity_human_display_profiles
                WHERE organization_id = @OrganizationId
                  AND actor_id = @ParticipantId
                """,
                new
                {
                    harness.OrganizationId,
                    harness.ParticipantId,
                },
                deleteTransaction,
                cancellationToken: CancellationToken));

        var assignTask = harness.Coordinator.AssignAsync(harness.AssignCommand("assign-profile"), CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(assignTask.IsCompleted);

        await deleteTransaction.CommitAsync(CancellationToken);
        var assigned = await assignTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
        Assert.False(assigned.Succeeded);
        Assert.Equal(EnrollmentFailureCodes.Ineligible, assigned.OutcomeCode);
    }

    [Fact]
    public async Task Invalid_commit_transaction_handle_fails_closed()
    {
        var connections = Fixture.Services.ConnectionAccessor;
        var directory = new PostgresHumanDisplayProfileDirectory(connections);
        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.RevalidateEligibleAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                EnrollmentAuthorizationActions.Receive,
                "forged",
                CancellationToken));
        Assert.Equal("commit.transaction.invalid", invalid.Message);

        var baselines = new PostgresAssessmentBaselineStore(
            connections,
            new PostgresAuditEventWriter(),
            new PostgresOutboxItemWriter());
        invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            baselines.FindBoundAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "forged",
                CancellationToken));
        Assert.Equal("commit.transaction.invalid", invalid.Message);

        var reader = new PostgresActivatedCohortBindingReader(
            connections,
            new PostgresAssessmentDraftStore(connections, new PostgresAuditEventWriter()),
            new PostgresAssessmentSourceCatalog(connections),
            new PostgresAssessmentSourceCatalog(connections),
            baselines,
            new ActivationBaselineDigester());
        invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.GetActivatedAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "forged",
                CancellationToken));
        Assert.Equal("commit.transaction.invalid", invalid.Message);

        var sessions = new PostgresApplicationSessionStore(connections);
        invalid = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sessions.RevalidateLiveAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "forged",
                CancellationToken));
        Assert.Equal("commit.transaction.invalid", invalid.Message);
    }

    [Fact]
    public async Task Live_uniqueness_conflict_keeps_the_transaction_usable()
    {
        var harness = await SeedActivatedAsync();
        var assigned = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-1"), CancellationToken);
        Assert.True(assigned.Succeeded, assigned.OutcomeCode);
        var store = new PostgresEnrollmentStore(Fixture.Services.ConnectionAccessor);
        var unitOfWork = new PostgresEnrollmentUnitOfWork(Fixture.Services.ConnectionAccessor);
        await unitOfWork.ExecuteAsync(async transaction =>
        {
            var current = await store.FindAsync(
                harness.OrganizationId,
                assigned.EnrollmentId!.Value,
                null,
                CancellationToken);
            var duplicate = current! with { EnrollmentId = Guid.CreateVersion7() };
            await Assert.ThrowsAsync<EnrollmentLiveUniquenessException>(() => store.InsertAsync(
                duplicate,
                new EnrollmentEvent(
                    Guid.CreateVersion7(),
                    duplicate.EnrollmentId,
                    duplicate.OrganizationId,
                    1,
                    "absent",
                    EnrollmentStates.Active,
                    EnrollmentReasonCodes.RestrictionRemoved,
                    harness.Actor.Actor.ActorId,
                    DateTimeOffset.UtcNow,
                    harness.Actor.CorrelationId,
                    null,
                    1),
                transaction,
                CancellationToken));
            var live = await store.FindLiveForParticipantAsync(
                harness.OrganizationId,
                harness.ActivityId,
                harness.ParticipantId,
                transaction,
                CancellationToken);
            Assert.Equal(assigned.EnrollmentId, live?.EnrollmentId);
            return true;
        }, CancellationToken);
    }

    [Fact]
    public async Task History_rejects_updates()
    {
        var harness = await SeedActivatedAsync();
        var assigned = await harness.Coordinator.AssignAsync(harness.AssignCommand("assign-1"), CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => connection.ExecuteAsync(
            """
            UPDATE submissions_enrollment_events
            SET reason_code = 'access_revoked'
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, EnrollmentId = assigned.EnrollmentId }));
        Assert.Contains("append-only", thrown.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid> CloneActivatedCohortAsync(EnrollmentHarness harness)
    {
        var otherCohortId = Guid.CreateVersion7();
        var otherBaselineId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO assessment_activation_baselines (
                organization_id, activity_id, baseline_id, content_digest, procedure_id,
                schema_version, canonicalization_version, document, created_at,
                actor_id, correlation_id)
            SELECT organization_id, activity_id, @OtherBaselineId, content_digest, procedure_id,
                   schema_version, canonicalization_version, document, CLOCK_TIMESTAMP(),
                   actor_id, correlation_id
            FROM assessment_activation_baselines
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            LIMIT 1;

            INSERT INTO assessment_cohorts (
                organization_id, activity_id, cohort_id, state, bound_revision_id,
                bound_revision_number, created_at, updated_at)
            SELECT organization_id, activity_id, @OtherCohortId, state, bound_revision_id,
                   bound_revision_number, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP()
            FROM assessment_cohorts
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId AND cohort_id = @CohortId;

            INSERT INTO assessment_cohort_baseline_bindings (
                organization_id, activity_id, cohort_id, baseline_id, bound_at)
            VALUES (@OrganizationId, @ActivityId, @OtherCohortId, @OtherBaselineId, CLOCK_TIMESTAMP());
            """,
            new
            {
                harness.OrganizationId,
                harness.ActivityId,
                harness.CohortId,
                OtherCohortId = otherCohortId,
                OtherBaselineId = otherBaselineId,
            });
        return otherCohortId;
    }

    private async Task<EnrollmentHarness> SeedActivatedAsync()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        foreach (var action in new[]
                 {
                     AssessmentAuthorizationActions.CreateActivity,
                     AssessmentAuthorizationActions.SelectSources,
                     AssessmentAuthorizationActions.ReadActivity,
                     AssessmentAuthorizationActions.SaveActivity,
                     AssessmentAuthorizationActions.ActivateCohort,
                     EnrollmentAuthorizationActions.Assign,
                     EnrollmentAuthorizationActions.Suspend,
                     EnrollmentAuthorizationActions.Close,
                     EnrollmentAuthorizationActions.Read,
                     EnrollmentAuthorizationActions.List,
                 })
        {
            await Fixture.GrantOrganizationActionAsync(seeded.OrganizationId, seeded.ActorId, action);
        }

        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            foreach (var source in AssessmentDevelopmentSources.ForOrganization(seeded.OrganizationId))
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
                        OrganizationId = seeded.OrganizationId,
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
        var activation = new AssessmentActivationCoordinator(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            catalog,
            store,
            new PostgresAssessmentUnitOfWork(connections),
            new ActivationBaselineDigester(),
            new AssessmentCommandDigest(),
            new PostgresAssessmentBaselineStore(connections, new PostgresAuditEventWriter(), new PostgresOutboxItemWriter()),
            new PostgresAssessmentAttemptStore(new PostgresAuditEventWriter()));
        var command = new ActivateCohortCommand(
            actor,
            created.Value.ActivityId,
            cohort!.CohortId,
            created.Value.RevisionId,
            created.Value.RevisionNumber,
            "act-1",
            string.Empty,
            DeploymentEnvironments.Development);
        command = command with { TrustedCommandDigest = new AssessmentCommandDigest().Compute(command) };
        var activated = await activation.ActivateAsync(command, CancellationToken);
        Assert.True(activated.Succeeded, activated.OutcomeCode);

        var participantId = Guid.CreateVersion7();
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO actors (id, created_at) VALUES (@ActorId, CLOCK_TIMESTAMP());
                INSERT INTO human_identity_bindings (binding_id, issuer, subject, actor_id, created_at)
                VALUES (@BindingId, 'https://issuer.test', @Subject, @ActorId, CLOCK_TIMESTAMP());
                INSERT INTO identity_human_display_profiles (organization_id, actor_id, display_label, created_at, updated_at)
                VALUES (@OrganizationId, @ActorId, 'Synthetic Participant', CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP());
                """,
                new
                {
                    ActorId = participantId,
                    BindingId = Guid.CreateVersion7(),
                    Subject = Guid.CreateVersion7().ToString("D"),
                    OrganizationId = seeded.OrganizationId,
                });
        }

        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            participantId,
            EnrollmentAuthorizationActions.Receive);

        var applicationSessionId = Guid.CreateVersion7();
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO application_sessions (
                    application_session_id, actor_id, organization_id, issuer, subject,
                    credential_digest, authentication_strength, created_at, last_seen_at,
                    idle_expires_at, absolute_expires_at)
                VALUES (
                    @ApplicationSessionId, @ActorId, @OrganizationId, 'https://issuer.test', @Subject,
                    @CredentialDigest, 'mfa', CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP(),
                    CLOCK_TIMESTAMP() + INTERVAL '20 minutes', CLOCK_TIMESTAMP() + INTERVAL '8 hours')
                """,
                new
                {
                    ApplicationSessionId = applicationSessionId,
                    ActorId = seeded.ActorId,
                    OrganizationId = seeded.OrganizationId,
                    Subject = seeded.ActorId.ToString("D"),
                    CredentialDigest = applicationSessionId.ToString("N") + new string('a', 32),
                });
        }

        var enrollmentActor = new EnrollmentActorContext(
            seeded.Actor,
            seeded.Scope,
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https",
            [
                EnrollmentAuthorizationActions.Assign,
                EnrollmentAuthorizationActions.Suspend,
                EnrollmentAuthorizationActions.Close,
                EnrollmentAuthorizationActions.List,
                EnrollmentAuthorizationActions.Read,
            ],
            applicationSessionId);
        var baselines = new PostgresAssessmentBaselineStore(connections, new PostgresAuditEventWriter(), new PostgresOutboxItemWriter());
        var coordinator = new EnrollmentCoordinator(
            new KernelEnrollmentAuthorizationPort(kernel, kernel),
            new AssessmentActivatedCohortPort(
                new PostgresActivatedCohortBindingReader(
                    connections,
                    store,
                    catalog,
                    catalog,
                    baselines,
                    new ActivationBaselineDigester())),
            new IdentityEnrollmentCandidatePort(new PostgresHumanDisplayProfileDirectory(connections)),
            new PostgresEnrollmentStore(connections),
            new PostgresEnrollmentOperationStore(),
            new PostgresEnrollmentAuditPort(new PostgresAuditEventWriter(), new PostgresOutboxItemWriter()),
            new PostgresEnrollmentUnitOfWork(connections),
            new IdentityEnrollmentSessionPort(new PostgresApplicationSessionStore(connections)));
        return new EnrollmentHarness(
            coordinator,
            enrollmentActor,
            seeded.OrganizationId,
            created.Value.ActivityId,
            cohort.CohortId,
            participantId,
            activated.BaselineDigest,
            applicationSessionId);
    }

    private sealed record EnrollmentHarness(
        EnrollmentCoordinator Coordinator,
        EnrollmentActorContext Actor,
        Guid OrganizationId,
        Guid ActivityId,
        Guid CohortId,
        Guid ParticipantId,
        string? BaselineDigest,
        Guid ApplicationSessionId)
    {
        public AssignEnrollmentCommand AssignCommand(string key, Guid? cohortId = null) =>
            new(
                Actor,
                ActivityId,
                cohortId ?? CohortId,
                ParticipantId,
                key,
                EnrollmentCommandDigest.Compute(
                    EnrollmentOperationKinds.Assign,
                    OrganizationId,
                    ActivityId,
                    cohortId ?? CohortId,
                    null,
                    ParticipantId,
                    null,
                    null));

        public EnrollmentLifecycleCommand LifecycleCommand(
            string operation,
            string reason,
            Guid enrollmentId,
            long revision,
            string key) =>
            new(
                Actor,
                ActivityId,
                CohortId,
                enrollmentId,
                operation,
                reason,
                revision,
                key,
                EnrollmentCommandDigest.Compute(
                    operation,
                    OrganizationId,
                    ActivityId,
                    CohortId,
                    enrollmentId,
                    null,
                    reason,
                    revision));
    }
}
