using System.Text.Json;
using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class DeterministicEvidenceCompletionTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Completion_service_persists_deterministic_fact_evidence_item_from_protected_store()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(Fixture, CancellationToken);
        var execution = context.First.Value!;
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var projection = await outputStore.TryLoadProjectionAsync(
            context.Claimed.Ownership.OrganizationId,
            context.Claimed.RequestId,
            execution.DeterministicAttemptId,
            execution.OutputContentDigest!,
            CancellationToken);
        Assert.NotNull(projection);

        RequestRow requestRow;
        Guid evaluationId;
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            requestRow = await connection.QuerySingleAsync<RequestRow>(
                """
                SELECT handoff_id, rubric_source_id, rubric_source_version_id, rubric_content_digest
                FROM evaluation_requests
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId;
                """,
                new
                {
                    context.Claimed.Ownership.OrganizationId,
                    context.Claimed.RequestId,
                });
            evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                context.Claimed,
                CancellationToken);
        }

        var ownership = context.Claimed.Ownership;
        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(ownership, evaluationId);
        var evidenceId = Guid.CreateVersion7();
        using var locatorDocument = JsonDocument.Parse(
            BuildDeterministicFactLocatorJson(
                projection!.SourceId,
                projection.SourceVersion,
                projection.ContentDigest,
                trustedOwnership,
                "/value"));
        var procedureSource = new PostgresProtectedEvaluationProcedureSource(Fixture.Services.ConnectionAccessor);
        var payload = await procedureSource.GetCanonicalUtf8Async(
            ownership.OrganizationId,
            requestRow.rubric_source_id,
            requestRow.rubric_source_version_id,
            requestRow.rubric_content_digest,
            CancellationToken);
        Assert.NotNull(payload);
        var procedure = EvaluationProcedureResolver.TryResolve(payload.Utf8).Value!;
        var completion = new EvidenceLocatorCompletionService(
            new PostgresEvaluationSessionEvidenceSource(
                Fixture.Services.ConnectionAccessor,
                new PostgresEvaluationHandoffSource(Fixture.Services.ConnectionAccessor)),
            new PostgresEvaluationSubmissionEvidenceSource(
                Fixture.Services.ConnectionAccessor,
                new InMemoryArtifactStore()),
            outputStore,
            new PostgresEvaluationEvidenceLocatorStore(Fixture.Services.ConnectionAccessor));
        var completionRequest = new EvidenceLocatorCompletionRequest(
            evaluationId,
            context.Claimed.RequestId,
            requestRow.handoff_id,
            [
                new EvidenceLocatorVerificationEntry(
                    evidenceId,
                    "crit.objective.word-count",
                    locatorDocument.RootElement.Clone()),
            ]);

        var result = await completion.TryVerifyAndPersistAsync(
            ownership.OrganizationId,
            ownership.SessionId,
            procedure,
            completionRequest,
            "evaluation.integration",
            CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Single(result.Value!.LocatorRecords);
        Assert.Equal(execution.DeterministicAttemptId, result.Value.LocatorRecords[0].SourceId);

        await using var verification = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var row = await verification.QuerySingleAsync<EvidenceItemRow>(
            """
            SELECT source_type, source_id, source_content_digest
            FROM evaluation_evidence_items
            WHERE organization_id = @OrganizationId
              AND evaluation_id = @EvaluationId
              AND evidence_id = @EvidenceId;
            """,
            new
            {
                ownership.OrganizationId,
                EvaluationId = evaluationId,
                EvidenceId = evidenceId,
            });
        Assert.Equal("deterministic.fact", row.source_type);
        Assert.Equal(execution.DeterministicAttemptId, row.source_id);
        Assert.Equal(projection.ContentDigest, row.source_content_digest);
    }

    [Fact]
    public async Task Completion_service_rejects_deterministic_fact_without_materialized_payload()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAttemptOnlyAsync(Fixture, CancellationToken);
        var execution = context.First.Value!;
        var sourceId = EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(
            execution.DeterministicAttemptId);
        var digest = execution.OutputContentDigest!;
        var sourceVersion = EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(digest);

        RequestRow requestRow;
        Guid evaluationId;
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            requestRow = await connection.QuerySingleAsync<RequestRow>(
                """
                SELECT handoff_id, rubric_source_id, rubric_source_version_id, rubric_content_digest
                FROM evaluation_requests
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId;
                """,
                new
                {
                    context.Claimed.Ownership.OrganizationId,
                    context.Claimed.RequestId,
                });
            evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                context.Claimed,
                CancellationToken);
        }

        var ownership = context.Claimed.Ownership;
        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(ownership, evaluationId);
        using var locatorDocument = JsonDocument.Parse(
            BuildDeterministicFactLocatorJson(
                sourceId,
                sourceVersion,
                digest,
                trustedOwnership,
                "/value"));
        var procedureSource = new PostgresProtectedEvaluationProcedureSource(Fixture.Services.ConnectionAccessor);
        var payload = await procedureSource.GetCanonicalUtf8Async(
            ownership.OrganizationId,
            requestRow.rubric_source_id,
            requestRow.rubric_source_version_id,
            requestRow.rubric_content_digest,
            CancellationToken);
        Assert.NotNull(payload);
        var procedure = EvaluationProcedureResolver.TryResolve(payload.Utf8).Value!;
        var completion = new EvidenceLocatorCompletionService(
            new PostgresEvaluationSessionEvidenceSource(
                Fixture.Services.ConnectionAccessor,
                new PostgresEvaluationHandoffSource(Fixture.Services.ConnectionAccessor)),
            new PostgresEvaluationSubmissionEvidenceSource(
                Fixture.Services.ConnectionAccessor,
                new InMemoryArtifactStore()),
            new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor),
            new PostgresEvaluationEvidenceLocatorStore(Fixture.Services.ConnectionAccessor));
        var completionRequest = new EvidenceLocatorCompletionRequest(
            evaluationId,
            context.Claimed.RequestId,
            requestRow.handoff_id,
            [
                new EvidenceLocatorVerificationEntry(
                    Guid.CreateVersion7(),
                    "crit.objective.word-count",
                    locatorDocument.RootElement.Clone()),
            ]);

        var result = await completion.TryVerifyAsync(
            ownership.OrganizationId,
            ownership.SessionId,
            procedure,
            completionRequest,
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
    }

    private static string BuildDeterministicFactLocatorJson(
        string sourceId,
        string sourceVersion,
        string sourceDigest,
        EvaluationStableOwnershipReference ownership,
        string jsonPointer) =>
        $$"""
        {
          "locator_schema":"evidence-locator.v1",
          "source_type":"deterministic.fact",
          "source_ref":{"source_id":"{{sourceId}}","source_version":"{{sourceVersion}}"},
          "ownership_ref":{
            "organization_id":"{{ownership.OrganizationId}}",
            "activity_id":"{{ownership.ActivityId}}",
            "participant_id":"{{ownership.ParticipantId}}",
            "attempt_id":"{{ownership.AttemptId}}",
            "session_id":"{{ownership.SessionId}}",
            "evaluation_id":"{{ownership.EvaluationId}}"
          },
          "location":{"location_type":"json_pointer","json_pointer":"{{jsonPointer}}"},
          "precision":"exact_range",
          "integrity":{
            "source_digest":"{{sourceDigest}}",
            "adapter_version":"locator-adapter.v1",
            "verification_state":"verified"
          },
          "created_by":{"service_id":"evaluation-service","invocation_id":"inv.integration.deterministic"}
        }
        """;

    private sealed record RequestRow(
        string handoff_id,
        Guid rubric_source_id,
        Guid rubric_source_version_id,
        string rubric_content_digest);

    private sealed record EvidenceItemRow(
        string source_type,
        Guid source_id,
        string source_content_digest);
}
