using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationCompletionAndImmutabilityTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task MarkCompletedAsync_reconciles_when_evaluation_already_exists()
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
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                CancellationToken);
        }

        var completed = await prepared.Work.MarkCompletedAsync(
            claimed!,
            prepared.WorkerActorId,
            CancellationToken);

        Assert.True(completed);
        await using var verification = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var state = await verification.QuerySingleAsync<(string WorkState, string AttemptState)>(
            """
            SELECT work.state, attempt.state
            FROM evaluation_durable_work AS work
            INNER JOIN evaluation_invocation_attempts AS attempt
              ON attempt.organization_id = work.organization_id
             AND attempt.request_id = work.request_id
             AND attempt.invocation_attempt_id = @InvocationAttemptId
            WHERE work.organization_id = @OrganizationId AND work.work_id = @WorkId;
            """,
            new
            {
                claimed!.Ownership.OrganizationId,
                claimed.WorkId,
                claimed.InvocationAttemptId,
            });
        Assert.Equal(("completed", "completed"), state);
    }

    [Fact]
    public async Task Concurrent_MarkCompletedAsync_reconciles_to_one_completed_work_state()
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
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                CancellationToken);
        }

        var results = await Task.WhenAll(
            prepared.Work.MarkCompletedAsync(claimed!, prepared.WorkerActorId, CancellationToken),
            prepared.Work.MarkCompletedAsync(claimed!, prepared.WorkerActorId, CancellationToken));

        Assert.Contains(true, results);
        Assert.Contains(false, results);
        await using var verification = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await verification.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_durable_work
            WHERE organization_id = @OrganizationId
              AND work_id = @WorkId
              AND state = 'completed';
            """,
            new
            {
                claimed!.Ownership.OrganizationId,
                claimed.WorkId,
            }));
    }

    [Fact]
    public async Task Duplicate_evaluation_on_same_request_is_rejected()
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
        await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
            connection,
            claimed!,
            CancellationToken);

        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO evaluations (
                organization_id, evaluation_id, request_id, activity_id, participant_id,
                attempt_id, session_id, evidence_set_id, procedure_source_id,
                procedure_source_version_id, procedure_digest, aggregate_status,
                creation_service_id, completed_at, predecessor_evaluation_id)
            SELECT
                request.organization_id, @DuplicateEvaluationId, request.request_id, request.activity_id,
                request.participant_id, request.attempt_id, request.session_id, evaluation.evidence_set_id,
                request.rubric_source_id, request.rubric_source_version_id, request.rubric_content_digest,
                'complete', 'evaluation.synthetic', clock_timestamp(), request.predecessor_evaluation_id
            FROM evaluation_requests AS request
            INNER JOIN evaluations AS evaluation
              ON evaluation.organization_id = request.organization_id
             AND evaluation.request_id = request.request_id
            WHERE request.organization_id = @OrganizationId AND request.request_id = @RequestId;
            """,
            new
            {
                claimed!.Ownership.OrganizationId,
                claimed.RequestId,
                DuplicateEvaluationId = Guid.CreateVersion7(),
            }));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
    }

    [Fact]
    public async Task Post_completion_retry_and_release_are_denied()
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
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                CancellationToken);
        }
        Assert.True(await prepared.Work.MarkCompletedAsync(
            claimed!,
            prepared.WorkerActorId,
            CancellationToken));

        Assert.False(await prepared.Work.ReleaseForRetryAsync(
            claimed,
            prepared.WorkerActorId,
            "provider.timeout",
            CancellationToken));
        Assert.False(await prepared.Work.MarkCompletedAsync(
            claimed,
            prepared.WorkerActorId,
            CancellationToken));
    }

    [Theory]
    [InlineData("evaluations", "UPDATE evaluations SET aggregate_status = 'conflict' WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId")]
    [InlineData("evaluation_evidence_sets", "UPDATE evaluation_evidence_sets SET seal_digest = @Digest WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId")]
    [InlineData("evaluation_criterion_judgments", "UPDATE evaluation_criterion_judgments SET rationale = 'tampered.rationale' WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId")]
    [InlineData("evaluation_annotations", "UPDATE evaluation_annotations SET reason = 'tampered.reason' WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId")]
    [InlineData("evaluation_manifest_refs", "UPDATE evaluation_manifest_refs SET content_digest = @Digest WHERE organization_id = @OrganizationId AND evaluation_id = @EvaluationId")]
    public async Task Completed_artifact_tables_reject_ordinary_update(
        string _,
        string updateSql)
    {
        var context = await SeedCompletedArtifactsAsync();

        var update = await Assert.ThrowsAsync<PostgresException>(() => context.Connection.ExecuteAsync(
            updateSql,
            new
            {
                context.OrganizationId,
                context.EvaluationId,
                Digest = new string('1', 64),
            }));
        Assert.Contains("append-only", update.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("evaluations")]
    [InlineData("evaluation_evidence_sets")]
    [InlineData("evaluation_criterion_judgments")]
    [InlineData("evaluation_annotations")]
    [InlineData("evaluation_manifest_refs")]
    public async Task Completed_artifact_tables_reject_ordinary_delete(string tableName)
    {
        var context = await SeedCompletedArtifactsAsync();

        var delete = await Assert.ThrowsAsync<PostgresException>(() => context.Connection.ExecuteAsync(
            $"""
            DELETE FROM {tableName}
            WHERE organization_id = @OrganizationId
              AND evaluation_id = @EvaluationId;
            """,
            new
            {
                context.OrganizationId,
                context.EvaluationId,
            }));
        Assert.Contains("append-only", delete.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(NpgsqlConnection Connection, Guid OrganizationId, Guid EvaluationId)> SeedCompletedArtifactsAsync()
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
        var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
            connection,
            claimed!,
            CancellationToken);
        var evidenceId = Guid.CreateVersion7();
        var judgmentId = Guid.CreateVersion7();
        var annotationId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_evidence_items (
                organization_id, evaluation_id, evidence_id, request_id, activity_id,
                participant_id, attempt_id, session_id, source_type, source_id,
                source_version_id, source_content_digest, locator_schema, locator_digest,
                precision, integrity_state, created_by_service, created_at)
            SELECT
                organization_id, @EvaluationId, @EvidenceId, request_id, activity_id,
                participant_id, attempt_id, session_id, 'submission.direct_text', rubric_source_id,
                rubric_source_version_id, rubric_content_digest, 'evidence-locator.v1', @LocatorDigest,
                'exact_range', 'verified', 'evaluation.synthetic', clock_timestamp()
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;

            INSERT INTO evaluation_criterion_judgments (
                organization_id, evaluation_id, judgment_id, request_id, criterion_id,
                criterion_version, evaluator_mode, status, confidence, uncertainty_json,
                rationale, score_json, provisional_feedback, deterministic_attempt_id)
            SELECT
                organization_id, @EvaluationId, @JudgmentId, request_id, 'criterion.text.quality',
                'criterion.text.quality.v1', 'deterministic', 'satisfied', 'high',
                '{"level":"low"}'::jsonb, 'Synthetic rationale.', NULL, NULL, NULL
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;

            INSERT INTO evaluation_annotations (
                organization_id, annotation_id, evaluation_id, evidence_id, kind,
                disposition, reason, actor_type, actor_id, occurred_at)
            VALUES (
                @OrganizationId, @AnnotationId, @EvaluationId, @EvidenceId,
                'source_integrity_changed', 'attention_required', 'integrity.changed',
                'service', @ActorId, clock_timestamp());

            INSERT INTO evaluation_manifest_refs (
                organization_id, evaluation_id, manifest_ref_id, ref_kind, protected_ref,
                content_digest, created_at)
            VALUES (
                @OrganizationId, @EvaluationId, @ManifestRefId, 'evaluation',
                'protected.evaluation.ref', @Digest, clock_timestamp());
            """,
            new
            {
                claimed!.Ownership.OrganizationId,
                claimed.RequestId,
                EvaluationId = evaluationId,
                EvidenceId = evidenceId,
                JudgmentId = judgmentId,
                AnnotationId = annotationId,
                ManifestRefId = Guid.CreateVersion7(),
                ActorId = prepared.WorkerActorId,
                LocatorDigest = new string('2', 64),
                Digest = new string('3', 64),
            });
        return (connection, claimed.Ownership.OrganizationId, evaluationId);
    }
}
