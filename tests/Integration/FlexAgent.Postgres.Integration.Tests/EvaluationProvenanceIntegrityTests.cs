using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationProvenanceIntegrityTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Disposition_cannot_cite_another_evaluation_annotation()
    {
        var first = await AdmitAndCompleteAsync();
        var secondEvaluationId = await InsertReplacementEvaluationAsync(first.Claimed, first.EvaluationId);
        var annotationId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_annotations (
                organization_id, annotation_id, evaluation_id, evidence_id, kind,
                disposition, reason, actor_type, actor_id, occurred_at)
            VALUES (
                @OrganizationId, @AnnotationId, @EvaluationId, NULL,
                'source_integrity_changed', 'attention_required', 'integrity.changed',
                'service', @ActorId, clock_timestamp());
            """,
            new
            {
                first.Claimed.Ownership.OrganizationId,
                AnnotationId = annotationId,
                first.EvaluationId,
                ActorId = first.Prepared.WorkerActorId,
            });

        var sameEvaluation = await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_dispositions (
                organization_id, evaluation_id, current_disposition, last_annotation_id,
                version, updated_at)
            VALUES (
                @OrganizationId, @EvaluationId, 'attention_required', @AnnotationId,
                1, clock_timestamp());
            """,
            new
            {
                first.Claimed.Ownership.OrganizationId,
                first.EvaluationId,
                AnnotationId = annotationId,
            });
        Assert.Equal(1, sameEvaluation);

        var crossEvaluation = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO evaluation_dispositions (
                organization_id, evaluation_id, current_disposition, last_annotation_id,
                version, updated_at)
            VALUES (
                @OrganizationId, @ForeignEvaluationId, 'attention_required', @AnnotationId,
                1, clock_timestamp());
            """,
            new
            {
                first.Claimed.Ownership.OrganizationId,
                ForeignEvaluationId = secondEvaluationId,
                AnnotationId = annotationId,
            }));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, crossEvaluation.SqlState);
    }

    [Fact]
    public async Task Lifecycle_disposition_event_cannot_cite_another_organizations_audit()
    {
        var local = await AdmitAndCompleteAsync();
        var foreign = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var matchingAuditId = Guid.CreateVersion7();
        var foreignAuditId = Guid.CreateVersion7();
        var failedAuditId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO audit_events (
                event_id, organization_id, event_schema_version, occurred_at,
                correlation_id, actor_type, actor_id, action, resource_type, resource_id,
                outcome, source_channel)
            VALUES
                (@MatchingAuditId, @OrganizationId, 'audit-event.v1', clock_timestamp(),
                 @CorrelationId, 'service', @ActorId, 'evaluation.lifecycle.dispose',
                 'evaluation', @EvaluationId, 'succeeded', 'integration.test'),
                (@FailedAuditId, @OrganizationId, 'audit-event.v1', clock_timestamp(),
                 @FailedCorrelationId, 'service', @ActorId, 'evaluation.lifecycle.dispose',
                 'evaluation', @EvaluationId, 'failed', 'integration.test'),
                (@ForeignAuditId, @ForeignOrganizationId, 'audit-event.v1', clock_timestamp(),
                 @ForeignCorrelationId, 'service', @ForeignActorId, 'evaluation.lifecycle.dispose',
                 'evaluation', @EvaluationId, 'succeeded', 'integration.test');
            """,
            new
            {
                MatchingAuditId = matchingAuditId,
                FailedAuditId = failedAuditId,
                ForeignAuditId = foreignAuditId,
                local.Claimed.Ownership.OrganizationId,
                ForeignOrganizationId = foreign.Request.FrozenInput.Ownership.OrganizationId,
                CorrelationId = Guid.CreateVersion7(),
                FailedCorrelationId = Guid.CreateVersion7(),
                ForeignCorrelationId = Guid.CreateVersion7(),
                ActorId = local.Prepared.WorkerActorId,
                ForeignActorId = foreign.WorkerActorId,
                local.EvaluationId,
            });

        var matched = await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_lifecycle_disposition_events (
                organization_id, disposition_event_id, evaluation_id, object_kind, object_id,
                reason_code, actor_id, audit_event_id, disposed_at)
            VALUES (
                @OrganizationId, @DispositionEventId, @EvaluationId, 'provider_artifact',
                @ObjectId, 'retention_expired', @ActorId, @MatchingAuditId, clock_timestamp());
            """,
            new
            {
                local.Claimed.Ownership.OrganizationId,
                DispositionEventId = Guid.CreateVersion7(),
                local.EvaluationId,
                ObjectId = Guid.CreateVersion7(),
                ActorId = local.Prepared.WorkerActorId,
                MatchingAuditId = matchingAuditId,
            });
        Assert.Equal(1, matched);

        var crossOrganization = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO evaluation_lifecycle_disposition_events (
                organization_id, disposition_event_id, evaluation_id, object_kind, object_id,
                reason_code, actor_id, audit_event_id, disposed_at)
            VALUES (
                @OrganizationId, @DispositionEventId, @EvaluationId, 'provider_artifact',
                @ObjectId, 'authorized_erasure', @ActorId, @ForeignAuditId, clock_timestamp());
            """,
            new
            {
                local.Claimed.Ownership.OrganizationId,
                DispositionEventId = Guid.CreateVersion7(),
                local.EvaluationId,
                ObjectId = Guid.CreateVersion7(),
                ActorId = local.Prepared.WorkerActorId,
                ForeignAuditId = foreignAuditId,
            }));
        Assert.True(
            crossOrganization.SqlState is PostgresErrorCodes.ForeignKeyViolation or "P0001",
            crossOrganization.SqlState);

        var failedOutcome = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO evaluation_lifecycle_disposition_events (
                organization_id, disposition_event_id, evaluation_id, object_kind, object_id,
                reason_code, actor_id, audit_event_id, disposed_at)
            VALUES (
                @OrganizationId, @DispositionEventId, @EvaluationId, 'provider_artifact',
                @ObjectId, 'authorized_erasure', @ActorId, @FailedAuditId, clock_timestamp());
            """,
            new
            {
                local.Claimed.Ownership.OrganizationId,
                DispositionEventId = Guid.CreateVersion7(),
                local.EvaluationId,
                ObjectId = Guid.CreateVersion7(),
                ActorId = local.Prepared.WorkerActorId,
                FailedAuditId = failedAuditId,
            }));
        Assert.Contains("matching succeeded", failedOutcome.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(EvaluationPersistenceTestSeed.PreparedEvaluation Prepared, EvaluationDurableWorkItem Claimed, Guid EvaluationId)>
        AdmitAndCompleteAsync()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
            connection,
            claimed!,
            CancellationToken);
        return (prepared, claimed!, evaluationId);
    }

    private async Task<Guid> InsertReplacementEvaluationAsync(
        EvaluationDurableWorkItem claimed,
        Guid predecessorEvaluationId)
    {
        var replacementRequestId = Guid.CreateVersion7();
        var replacementEvaluationId = Guid.CreateVersion7();
        var evidenceSetId = Guid.CreateVersion7();
        var invocationAttemptId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_requests (
                organization_id, request_id, activity_id, participant_id, attempt_id,
                session_id, handoff_id, terminal_record_id, handoff_eligibility, handoff_terminal_state,
                cutoff_sequence, request_kind, frozen_input_digest, idempotency_key,
                delegation_id, state, predecessor_evaluation_id, replacement_reason,
                rubric_source_id, rubric_source_version_id, rubric_content_digest,
                submission_source_id, submission_version_id, submission_content_digest,
                configuration_id, configuration_record_id, configuration_digest,
                manifest_id, manifest_record_id, manifest_digest,
                manifest_seal_procedure_id, terminal_seal_digest,
                model_profile_id, model_profile_version, model_profile_digest,
                provider_id, credential_mode, credential_binding_reference,
                credential_binding_version, evaluator_registry_version,
                lifecycle_policy_ref, correlation_id, created_at, completed_at, failure_category)
            SELECT
                organization_id, @ReplacementRequestId, activity_id, participant_id, attempt_id,
                session_id, handoff_id, terminal_record_id, handoff_eligibility, handoff_terminal_state,
                cutoff_sequence, 'replacement', frozen_input_digest, @IdempotencyKey,
                delegation_id, 'completed', @PredecessorEvaluationId, 'authorized.replacement',
                rubric_source_id, rubric_source_version_id, rubric_content_digest,
                submission_source_id, submission_version_id, submission_content_digest,
                configuration_id, configuration_record_id, configuration_digest,
                manifest_id, manifest_record_id, manifest_digest,
                manifest_seal_procedure_id, terminal_seal_digest,
                model_profile_id, model_profile_version, model_profile_digest,
                provider_id, credential_mode, credential_binding_reference,
                credential_binding_version, evaluator_registry_version,
                lifecycle_policy_ref, @CorrelationId, clock_timestamp(), clock_timestamp(), NULL
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;

            INSERT INTO evaluation_invocation_attempts (
                organization_id, request_id, invocation_attempt_id,
                activity_id, participant_id, attempt_id, session_id,
                attempt_ordinal, state, started_at, finished_at)
            SELECT
                organization_id, @ReplacementRequestId, @InvocationAttemptId,
                activity_id, participant_id, attempt_id, session_id,
                1, 'completed', clock_timestamp(), clock_timestamp()
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @ReplacementRequestId;

            INSERT INTO evaluation_evidence_sets (
                organization_id, evaluation_id, evidence_set_id, request_id,
                invocation_attempt_id, seal_schema, seal_digest, sealed_at, sealed_by_service)
            VALUES (
                @OrganizationId, @ReplacementEvaluationId, @EvidenceSetId, @ReplacementRequestId,
                @InvocationAttemptId, 'evidence-set-jcs-sha256-v1', @Digest,
                clock_timestamp(), 'evaluation.synthetic');

            INSERT INTO evaluations (
                organization_id, evaluation_id, request_id, activity_id, participant_id,
                attempt_id, session_id, evidence_set_id, procedure_source_id,
                procedure_source_version_id, procedure_digest, aggregate_status,
                creation_service_id, completed_at, predecessor_evaluation_id)
            SELECT
                organization_id, @ReplacementEvaluationId, request_id, activity_id, participant_id,
                attempt_id, session_id, @EvidenceSetId, rubric_source_id,
                rubric_source_version_id, rubric_content_digest, 'complete',
                'evaluation.synthetic', clock_timestamp(), predecessor_evaluation_id
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @ReplacementRequestId;
            """,
            new
            {
                claimed.Ownership.OrganizationId,
                claimed.RequestId,
                ReplacementRequestId = replacementRequestId,
                ReplacementEvaluationId = replacementEvaluationId,
                PredecessorEvaluationId = predecessorEvaluationId,
                IdempotencyKey = $"idem.eval.{Guid.CreateVersion7():N}",
                CorrelationId = Guid.CreateVersion7(),
                InvocationAttemptId = invocationAttemptId,
                EvidenceSetId = evidenceSetId,
                Digest = new string('8', 64),
            });
        return replacementEvaluationId;
    }
}
