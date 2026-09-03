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

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class SubmissionIntakeTestSeed
{
    internal static async Task<SubmissionIntakeHarness> CreateAsync(
        PostgresIntegrationFixture fixture,
        CancellationToken cancellationToken)
    {
        var seeded = await fixture.SeedOrganizationAsync();
        foreach (var action in new[]
                 {
                     AssessmentAuthorizationActions.CreateActivity,
                     AssessmentAuthorizationActions.SelectSources,
                     AssessmentAuthorizationActions.ReadActivity,
                     AssessmentAuthorizationActions.SaveActivity,
                     AssessmentAuthorizationActions.ActivateCohort,
                     EnrollmentAuthorizationActions.Assign,
                     EnrollmentAuthorizationActions.Discover,
                 })
        {
            await fixture.GrantOrganizationActionAsync(seeded.OrganizationId, seeded.ActorId, action);
        }

        await using (var connection = await fixture.Services.ConnectionAccessor.OpenConnectionAsync(cancellationToken))
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

        var connections = fixture.Services.ConnectionAccessor;
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
                "Submission Intake Assessment",
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
                    3600,
                    900,
                    300),
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
            cancellationToken);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(created.OutcomeCode);
        }

        var cohort = await store.FindCohortForActivityAsync(seeded.OrganizationId, created.Value!.ActivityId, cancellationToken);
        var activation = new AssessmentActivationCoordinator(
            new KernelAssessmentAuthorizationPort(kernel, kernel),
            catalog,
            store,
            new PostgresAssessmentUnitOfWork(connections),
            new ActivationBaselineDigester(),
            new AssessmentCommandDigest(),
            new PostgresAssessmentBaselineStore(connections, new PostgresAuditEventWriter(), new PostgresOutboxItemWriter()),
            new PostgresAssessmentAttemptStore(new PostgresAuditEventWriter()));
        var activateCommand = new ActivateCohortCommand(
            actor,
            created.Value.ActivityId,
            cohort!.CohortId,
            created.Value.RevisionId,
            created.Value.RevisionNumber,
            "act-submission",
            string.Empty,
            DeploymentEnvironments.Development);
        activateCommand = activateCommand with { TrustedCommandDigest = new AssessmentCommandDigest().Compute(activateCommand) };
        var activated = await activation.ActivateAsync(activateCommand, cancellationToken);
        if (!activated.Succeeded)
        {
            throw new InvalidOperationException(activated.OutcomeCode);
        }

        var participantId = Guid.CreateVersion7();
        await using (var connection = await connections.OpenConnectionAsync(cancellationToken))
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

        await fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            participantId,
            EnrollmentAuthorizationActions.Receive);
        await fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            participantId,
            EnrollmentAuthorizationActions.Discover);

        var participantSessionId = Guid.CreateVersion7();
        await using (var connection = await connections.OpenConnectionAsync(cancellationToken))
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
                    ApplicationSessionId = participantSessionId,
                    ActorId = participantId,
                    OrganizationId = seeded.OrganizationId,
                    Subject = participantId.ToString("D"),
                    CredentialDigest = participantSessionId.ToString("N") + new string('b', 32),
                });
        }

        var adminSessionId = Guid.CreateVersion7();
        await using (var connection = await connections.OpenConnectionAsync(cancellationToken))
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
                    ApplicationSessionId = adminSessionId,
                    ActorId = seeded.ActorId,
                    OrganizationId = seeded.OrganizationId,
                    Subject = seeded.ActorId.ToString("D"),
                    CredentialDigest = adminSessionId.ToString("N") + new string('a', 32),
                });
        }

        var enrollmentActor = new EnrollmentActorContext(
            seeded.Actor,
            seeded.Scope,
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.CreateVersion7(),
            "https",
            [EnrollmentAuthorizationActions.Assign, EnrollmentAuthorizationActions.Discover],
            adminSessionId);
        var baselines = new PostgresAssessmentBaselineStore(connections, new PostgresAuditEventWriter(), new PostgresOutboxItemWriter());
        var sessions = new IdentityEnrollmentSessionPort(new PostgresApplicationSessionStore(connections));
        var enrollmentCoordinator = new EnrollmentCoordinator(
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
            new PostgresEnrollmentUnitOfWork(connections, sessions),
            sessions);
        var assigned = await enrollmentCoordinator.AssignAsync(
            new AssignEnrollmentCommand(
                enrollmentActor,
                created.Value.ActivityId,
                cohort.CohortId,
                participantId,
                "assign-submission",
                EnrollmentCommandDigest.Compute(
                    EnrollmentOperationKinds.Assign,
                    seeded.OrganizationId,
                    created.Value.ActivityId,
                    cohort.CohortId,
                    null,
                    participantId,
                    null,
                    null)),
            cancellationToken);
        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException(assigned.OutcomeCode);
        }

        var participantActor = new EnrollmentActorContext(
            new TrustedActor(participantId, HumanInteractiveActorTypes.Interactive),
            seeded.Scope,
            string.Empty,
            new AuthenticationStrength(null, []),
            Guid.CreateVersion7(),
            "https",
            [EnrollmentAuthorizationActions.Discover],
            participantSessionId);
        var intakeCoordinator = new IntakeCoordinator(
            new KernelEnrollmentAuthorizationPort(kernel, kernel),
            new PostgresEnrollmentStore(connections),
            new AssessmentActivatedCohortPort(
                new PostgresActivatedCohortBindingReader(
                    connections,
                    store,
                    catalog,
                    catalog,
                    baselines,
                    new ActivationBaselineDigester())),
            new FixedFrozenSubmissionRequirementPort(),
            new FixedMaterialPolicyPort(),
            new PostgresIntakeStore(connections),
            new PostgresSubmissionVersionStore(connections),
            new PostgresEnrollmentOperationStore(),
            new PostgresEnrollmentAuditPort(new PostgresAuditEventWriter(), new PostgresOutboxItemWriter()),
            new PostgresEnrollmentUnitOfWork(connections, sessions),
            sessions,
            new DisabledArtifactSafetyScanner(),
            new OpenSubmissionTimingPort(),
            new InMemoryArtifactStore(),
            new FixedEnrollmentClock(DateTimeOffset.Parse("2026-08-24T12:00:00Z")));

        return new SubmissionIntakeHarness(
            intakeCoordinator,
            enrollmentCoordinator,
            enrollmentActor,
            participantActor,
            seeded.OrganizationId,
            created.Value.ActivityId,
            cohort.CohortId,
            participantId,
            assigned.EnrollmentId!.Value);
    }

    internal sealed record SubmissionIntakeHarness(
        IntakeCoordinator IntakeCoordinator,
        EnrollmentCoordinator EnrollmentCoordinator,
        EnrollmentActorContext AdminActor,
        EnrollmentActorContext ParticipantActor,
        Guid OrganizationId,
        Guid ActivityId,
        Guid CohortId,
        Guid ParticipantId,
        Guid EnrollmentId)
    {
        public AssignEnrollmentCommand AssignCommand(string key, Guid participantId) =>
            new(
                AdminActor,
                ActivityId,
                CohortId,
                participantId,
                key,
                EnrollmentCommandDigest.Compute(
                    EnrollmentOperationKinds.Assign,
                    OrganizationId,
                    ActivityId,
                    CohortId,
                    null,
                    participantId,
                    null,
                    null));

        public BeginIntakeCommand BeginCommand(string key) =>
            new(
                ParticipantActor,
                EnrollmentId,
                key,
                SubmissionCommandDigest.Compute(
                    IntakeOperationKinds.Begin,
                    OrganizationId.ToString("D"),
                    EnrollmentId.ToString("D")));

        public CompleteIntakeItemCommand CompleteCommand(Guid intakeId, long revision, string text, string key)
        {
            var content = System.Text.Encoding.UTF8.GetBytes(text);
            var digest = MaterialContentValidator.Sha256Hex(content);
            return new CompleteIntakeItemCommand(
                ParticipantActor,
                EnrollmentId,
                intakeId,
                Guid.Empty,
                MaterialCategories.DirectText,
                null,
                "text/plain",
                content,
                digest,
                revision,
                key,
                SubmissionCommandDigest.Compute(
                    IntakeOperationKinds.CompleteItem,
                    OrganizationId.ToString("D"),
                    EnrollmentId.ToString("D"),
                    intakeId.ToString("D"),
                    revision.ToString(),
                    digest));
        }
    }

    private sealed class OpenSubmissionTimingPort : IEnrollmentTimingQueryService
    {
        public Task<EnrollmentDecision<EnrollmentTimingDetail>> GetEnrollmentTimingAsync(
            EnrollmentActorContext actor,
            Guid activityId,
            Guid cohortId,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EnrollmentDecision<AssignmentTimingSummary>> GetMyWorkTimingAsync(
            EnrollmentActorContext actor,
            Guid enrollmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(EnrollmentDecision<AssignmentTimingSummary>.Ok(
                new AssignmentTimingSummary(
                    new AssignmentSummary(
                        enrollmentId,
                        EnrollmentStates.Active,
                        EnrollmentVisibilityStates.Current,
                        "Campaign",
                        "Task",
                        "UTC",
                        DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                        DateTimeOffset.Parse("2026-09-30T23:59:00Z"),
                        DateTimeOffset.Parse("2026-09-30T17:00:00Z"),
                        true,
                        []),
                    new EffectiveTiming(
                        new BaselineTiming(
                            DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                            DateTimeOffset.Parse("2026-09-30T23:59:00Z"),
                            DateTimeOffset.Parse("2026-09-30T17:00:00Z"),
                            "UTC",
                            1,
                            3600,
                            new AccommodationPolicyIdentity(Guid.CreateVersion7(), Guid.CreateVersion7(), new string('c', 64)),
                            false),
                        DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                        DateTimeOffset.Parse("2026-09-30T17:00:00Z"),
                        DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                        DateTimeOffset.Parse("2026-09-30T23:59:00Z"),
                        3600,
                        DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                        "open",
                        true,
                        [],
                        AccommodationConsequenceCodes.None,
                        "UTC"),
                    AccommodationConsequenceCodes.None),
                "enrollment.ok"));

        public Task<EffectiveTiming?> ComposeAuthoritativeInTransactionAsync(
            Enrollment enrollment,
            IEnrollmentTransaction transaction,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default)
        {
            _ = (enrollment, transaction, asOfUtc, cancellationToken);
            return Task.FromResult<EffectiveTiming?>(new EffectiveTiming(
                new BaselineTiming(
                    DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                    DateTimeOffset.Parse("2026-09-30T23:59:00Z"),
                    DateTimeOffset.Parse("2026-09-30T17:00:00Z"),
                    "UTC",
                    1,
                    3600,
                    new AccommodationPolicyIdentity(Guid.CreateVersion7(), Guid.CreateVersion7(), new string('c', 64)),
                    false),
                DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                DateTimeOffset.Parse("2026-09-30T17:00:00Z"),
                DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                DateTimeOffset.Parse("2026-09-30T23:59:00Z"),
                3600,
                DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
                "open",
                true,
                [],
                AccommodationConsequenceCodes.None,
                "UTC"));
        }
    }
}
