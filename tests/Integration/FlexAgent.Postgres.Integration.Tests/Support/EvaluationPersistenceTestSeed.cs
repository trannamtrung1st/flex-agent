using Dapper;
using System.Text.Json;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class EvaluationPersistenceTestSeed
{
    internal static EvaluationAdmissionAuthority Authority { get; } =
        new("evalreg.p0.v1", "lifecycle.activity-closure-365d.v1");

    internal static async Task<PreparedEvaluation> CreateAsync(
        PostgresIntegrationFixture fixture,
        string key,
        CancellationToken cancellationToken)
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(fixture, cancellationToken);
        var workerActorId = await fixture.SeedWorkerActorAsync();
        var ownership = new EvaluationOwnership(
            harness.OrganizationId,
            harness.ActivityId,
            harness.ParticipantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var terminalRecordId = Guid.CreateVersion7();
        var configurationId = Guid.CreateVersion7();
        var manifestId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var submissionVersionId = Guid.CreateVersion7();
        var delegationId = Guid.CreateVersion7();
        var configurationDigest = new string('c', 64);
        var manifestDigest = new string('d', 64);
        var terminalSealDigest = new string('f', 64);
        var submissionDigest = new string('e', 64);
        var handoffId = $"handoff.eval.{key}";

        await using var connection = await fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(cancellationToken);
        var enrollment = await connection.QuerySingleAsync<EnrollmentRow>(
            new CommandDefinition(
                """
                SELECT baseline_id, enrollment_id, cohort_id, participant_actor_id,
                       task_source_id, task_version_id, task_content_digest
                FROM submissions_enrollments
                WHERE organization_id = @OrganizationId
                  AND enrollment_id = @EnrollmentId;
                """,
                new { harness.OrganizationId, harness.EnrollmentId },
                cancellationToken: cancellationToken));
        var rubric = await connection.QuerySingleAsync<RubricRow>(
            new CommandDefinition(
                """
                SELECT payload.configuration_source_id, payload.source_version_id, payload.content_digest
                FROM configuration_source_payloads AS payload
                INNER JOIN configuration_source_readiness_descriptors AS descriptor
                  ON descriptor.organization_id = payload.organization_id
                 AND descriptor.configuration_source_id = payload.configuration_source_id
                 AND descriptor.version_id = payload.source_version_id
                WHERE payload.organization_id = @OrganizationId
                  AND descriptor.category = 'rubric_evaluation';
                """,
                new { harness.OrganizationId },
                cancellationToken: cancellationToken));
        var resolvedConfigurationJson = JsonSerializer.Serialize(new
        {
            sources = new[]
            {
                new
                {
                    source_key = "rubric_evaluation",
                    source_id = rubric.configuration_source_id.ToString("D"),
                    source_version_id = rubric.source_version_id.ToString("D"),
                    content_digest = rubric.content_digest,
                },
            },
            permitted_submissions = new[]
            {
                new
                {
                    protected_ref = $"submission.version.{submissionVersionId:N}",
                    content_digest = submissionDigest,
                },
            },
        });

        await connection.ExecuteAsync(
            new CommandDefinition(
                SeedSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    enrollment.cohort_id,
                    enrollment.baseline_id,
                    enrollment.enrollment_id,
                    TaskSourceId = enrollment.task_source_id,
                    enrollment.task_version_id,
                    enrollment.task_content_digest,
                    TerminalRecordId = terminalRecordId,
                    ConfigurationId = configurationId,
                    ManifestId = manifestId,
                    ConfigurationRuntimeId = configurationId.ToString("D"),
                    ManifestRuntimeId = manifestId.ToString("D"),
                    ConfigurationDigest = configurationDigest,
                    ManifestDigest = manifestDigest,
                    TerminalSealDigest = terminalSealDigest,
                    SubmissionId = submissionId,
                    SubmissionVersionId = submissionVersionId,
                    SubmissionDigest = submissionDigest,
                    ResolvedConfigurationJson = resolvedConfigurationJson,
                    InitialManifestJson = "{}",
                    HandoffId = handoffId,
                    WorkerActorId = workerActorId,
                    DelegationId = delegationId,
                },
                cancellationToken: cancellationToken));

        var frozenInput = FrozenInputIdentity.TryCreate(
            handoffId,
            ownership,
            terminalRecordId,
            "completed",
            42,
            "manifest-jcs-sha256-v2",
            terminalSealDigest,
            configurationId,
            configurationDigest,
            manifestId,
            manifestDigest,
            ExactSourceIdentity.TryCreate(
                "rubric_evaluation",
                rubric.configuration_source_id,
                rubric.source_version_id,
                rubric.content_digest).Value!,
            ExactSourceIdentity.TryCreate(
                "task_submission",
                submissionId,
                submissionVersionId,
                submissionDigest).Value!,
            "evalreg.p0.v1",
            FrozenModelIdentity.TryCreate(
                "mdl.p0.text.synthetic",
                "mdl.p0.text.synthetic.v1",
                new string('a', 64),
                "provider.synthetic",
                "organization_byok",
                "cred.bind.synthetic",
                "cred.bind.synthetic.v1").Value!,
            "lifecycle.activity-closure-365d.v1").Value!;
        var request = EvaluationRequest.TryCreate(
            Guid.CreateVersion7(),
            EvaluationRequestKinds.Initial,
            frozenInput,
            $"idem.eval.{key}",
            EvaluationDelegationReference.Format(delegationId),
            EvaluationRequestStates.Queued).Value!;

        return new PreparedEvaluation(
            new PostgresEvaluationAdmissionStore(fixture.Services.ConnectionAccessor, Authority),
            new PostgresEvaluationDurableWorkStore(fixture.Services.ConnectionAccessor),
            request,
            delegationId,
            workerActorId,
            Authority);
    }

    private const string SeedSql = """
        INSERT INTO session_resolved_configurations (
            organization_id, configuration_id, configuration_digest, canonical_json, created_at)
        VALUES (
            @OrganizationId, @ConfigurationId, @ConfigurationDigest,
            @ResolvedConfigurationJson, clock_timestamp());

        INSERT INTO session_initial_manifests (
            organization_id, manifest_id, configuration_id, manifest_digest,
            canonical_json, created_at)
        VALUES (
            @OrganizationId, @ManifestId, @ConfigurationId, @ManifestDigest,
            @InitialManifestJson, clock_timestamp());

        INSERT INTO submissions_submissions (
            organization_id, submission_id, activity_id, cohort_id, baseline_id,
            enrollment_id, participant_actor_id, task_source_id, task_version_id,
            task_content_digest, created_at)
        VALUES (
            @OrganizationId, @SubmissionId, @ActivityId, @cohort_id, @baseline_id,
            @enrollment_id, @ParticipantId, @TaskSourceId, @task_version_id,
            @task_content_digest, clock_timestamp());

        INSERT INTO submissions_accepted_versions (
            organization_id, submission_id, version_id, version_number, activity_id,
            cohort_id, baseline_id, enrollment_id, participant_actor_id, task_source_id,
            task_version_id, task_content_digest, policy_digest, predecessor_version_id,
            accepted_at, accepted_by_actor_id)
        VALUES (
            @OrganizationId, @SubmissionId, @SubmissionVersionId, 1, @ActivityId,
            @cohort_id, @baseline_id, @enrollment_id, @ParticipantId, @TaskSourceId,
            @task_version_id, @task_content_digest, @SubmissionDigest, NULL,
            clock_timestamp(), @ParticipantId);

        INSERT INTO submissions_attempts (
            organization_id, attempt_id, activity_id, cohort_id, baseline_id, enrollment_id,
            participant_actor_id, task_source_id, ordinal, entitlement_source, retry_entitlement_id,
            status, consumed, requested_at, started_at, terminal_at, terminal_reason_category,
            session_id, resolved_configuration_id, initial_manifest_id,
            configuration_digest, manifest_digest)
        VALUES (
            @OrganizationId, @AttemptId, @ActivityId, @cohort_id, @baseline_id, @enrollment_id,
            @ParticipantId, @TaskSourceId, 1, 'baseline', NULL,
            'completed', TRUE, clock_timestamp(), clock_timestamp(), clock_timestamp(), 'completed',
            @SessionId, @ConfigurationId, @ManifestId, @ConfigurationDigest, @ManifestDigest);

        INSERT INTO submissions_attempt_submission_bindings (
            organization_id, attempt_id, version_id, version_number, binding_order, content_digest)
        VALUES (
            @OrganizationId, @AttemptId, @SubmissionVersionId, 1, 1, @SubmissionDigest);

        INSERT INTO session_runtimes (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            configuration_id, configuration_digest, manifest_id, lifecycle_state,
            session_version, session_sequence, cutoff_sequence)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @ConfigurationRuntimeId, @ConfigurationDigest, @ManifestRuntimeId, 'completed',
            1, 42, 42);

        INSERT INTO session_frozen_model_deployments (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            profile_id, profile_version, profile_digest, provider_id, credential_mode,
            credential_binding_reference, credential_binding_version)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            'mdl.p0.text.synthetic', 'mdl.p0.text.synthetic.v1',
            'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
            'provider.synthetic', 'organization_byok',
            'cred.bind.synthetic', 'cred.bind.synthetic.v1');

        INSERT INTO session_terminal_records (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            terminal_record_id, lifecycle_state, cutoff_sequence, reason_category,
            attempt_mapping, procedure_id, seal_digest)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @TerminalRecordId, 'completed', 42, 'completed', 'completed',
            'manifest-jcs-sha256-v2', @TerminalSealDigest);

        INSERT INTO session_evaluation_handoffs (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            handoff_id, terminal_record_id, procedure_id, eligibility, terminal_state,
            cutoff_sequence, configuration_id, configuration_digest, manifest_id, seal_digest)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @HandoffId, @TerminalRecordId, 'manifest-jcs-sha256-v2', 'eligible', 'completed',
            42, @ConfigurationRuntimeId, @ConfigurationDigest, @ManifestRuntimeId, @TerminalSealDigest);

        INSERT INTO service_delegations (
            delegation_id, organization_id, activity_id, participant_id, attempt_id,
            session_id, service_actor_id, allowed_action, system_purpose,
            initiating_authority, effective_at, expires_at, revoked_at, delegation_version)
        VALUES (
            @DelegationId, @OrganizationId, @ActivityId, @ParticipantId, @AttemptId,
            @SessionId, @WorkerActorId, 'evaluation.execute', 'evaluation.execution',
            'synthetic.integration', clock_timestamp() - interval '1 minute',
            clock_timestamp() + interval '1 hour', NULL, 1);
        """;

    internal sealed record PreparedEvaluation(
        PostgresEvaluationAdmissionStore Admission,
        PostgresEvaluationDurableWorkStore Work,
        EvaluationRequest Request,
        Guid DelegationId,
        Guid WorkerActorId,
        EvaluationAdmissionAuthority Authority)
    {
        public AdmitEvaluationCommand Command() => new(
            Request,
            DelegationId,
            WorkerActorId,
            "service",
            Guid.CreateVersion7(),
            "integration.test",
            OrganizationBacklogLimit: 100,
            MaxAttempts: 3,
            AttemptTimeoutSeconds: 120,
            BackoffSeconds: 1);
    }

    private sealed record EnrollmentRow(
        Guid baseline_id,
        Guid enrollment_id,
        Guid cohort_id,
        Guid participant_actor_id,
        Guid task_source_id,
        Guid task_version_id,
        string task_content_digest);

    private sealed record RubricRow(
        Guid configuration_source_id,
        Guid source_version_id,
        string content_digest);
}
