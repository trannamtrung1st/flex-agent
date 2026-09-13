using System.Text.Json;
using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Audit;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class EvaluationCompletionTestSupport
{
    internal const string IntegrationCanonicalInputDigest = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";

    internal sealed record CompletionBundle(EvaluationCompletionCommand Command);

    internal static PostgresEvaluationCompletionCoordinator CreateCoordinator(
        PostgresIntegrationFixture fixture,
        InMemoryArtifactStore artifacts,
        IAuditEventWriter? auditEventWriter = null) =>
        new(
            fixture.Services.ConnectionAccessor,
            auditEventWriter,
            outboxItemWriter: null,
            new EvidenceLocatorCompletionService(
                new PostgresEvaluationSessionEvidenceSource(
                    fixture.Services.ConnectionAccessor,
                    new PostgresEvaluationHandoffSource(fixture.Services.ConnectionAccessor)),
                new PostgresEvaluationSubmissionEvidenceSource(
                    fixture.Services.ConnectionAccessor,
                    artifacts),
                new PostgresProtectedDeterministicOutputStore(fixture.Services.ConnectionAccessor),
                new PostgresEvaluationEvidenceLocatorStore(fixture.Services.ConnectionAccessor)));

    internal static async Task<CompletionBundle> BuildBundleAsync(
        PostgresIntegrationFixture fixture,
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared,
        EvaluationDurableWorkItem claimed,
        InMemoryArtifactStore artifacts,
        CancellationToken cancellationToken,
        string requestKind = EvaluationRequestKinds.Initial,
        Guid? predecessorEvaluationId = null,
        string? replacementReason = null,
        string? forgedCanonicalInputDigest = null)
    {
        await using var connection = await fixture.Services.ConnectionAccessor.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE evaluation_requests
            SET state = 'completing'
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;

            UPDATE evaluation_invocation_attempts
            SET state = 'completing'
            WHERE organization_id = @OrganizationId
              AND request_id = @RequestId
              AND invocation_attempt_id = @InvocationAttemptId;
            """,
            new
            {
                claimed.Ownership.OrganizationId,
                claimed.RequestId,
                claimed.InvocationAttemptId,
            });

        var requestRow = await connection.QuerySingleAsync<RequestRow>(
            """
            SELECT handoff_id, rubric_source_id, rubric_source_version_id, rubric_content_digest
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new { claimed.Ownership.OrganizationId, claimed.RequestId });
        var procedureSource = new PostgresProtectedEvaluationProcedureSource(fixture.Services.ConnectionAccessor);
        var procedurePayload = await procedureSource.GetCanonicalUtf8Async(
            claimed.Ownership.OrganizationId,
            requestRow.rubric_source_id,
            requestRow.rubric_source_version_id,
            requestRow.rubric_content_digest,
            cancellationToken);
        var procedure = EvaluationProcedureResolver.TryResolve(procedurePayload!.Utf8).Value!;
        var deterministicAttemptIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var completedAt = DateTimeOffset.UtcNow;
        foreach (var criterion in procedure.Criteria)
        {
            if (criterion.EvaluatorMode is not (EvaluatorModes.Deterministic or EvaluatorModes.AgentAssisted)
                || criterion.DeterministicEvaluator is null)
            {
                continue;
            }

            var attemptId = Guid.CreateVersion7();
            deterministicAttemptIds[criterion.CriterionId] = attemptId;
            await connection.ExecuteAsync(
                """
                INSERT INTO evaluation_deterministic_attempts (
                    organization_id, deterministic_attempt_id, request_id, invocation_attempt_id,
                    criterion_id, criterion_version, evaluator_id, evaluator_version, evaluator_digest,
                    canonical_input_digest, dependency_digest, configuration_digest, outcome,
                    protected_input_ref, protected_output_ref, output_content_digest,
                    started_at, finished_at)
                VALUES (
                    @OrganizationId, @DeterministicAttemptId, @RequestId, @InvocationAttemptId,
                    @CriterionId, @CriterionVersion, @EvaluatorId, @EvaluatorVersion, @EvaluatorDigest,
                    @CanonicalInputDigest, @DependencyDigest, @ConfigurationDigest, 'succeeded',
                    @ProtectedInputRef, 'protected.output.ref', @OutputContentDigest,
                    @StartedAt, @FinishedAt);
                """,
                new
                {
                    claimed.Ownership.OrganizationId,
                    DeterministicAttemptId = attemptId,
                    claimed.RequestId,
                    claimed.InvocationAttemptId,
                    criterion.CriterionId,
                    criterion.CriterionVersion,
                    criterion.DeterministicEvaluator.EvaluatorId,
                    criterion.DeterministicEvaluator.EvaluatorVersion,
                    criterion.DeterministicEvaluator.EvaluatorDigest,
                    CanonicalInputDigest = forgedCanonicalInputDigest ?? IntegrationCanonicalInputDigest,
                    ProtectedInputRef = DeterministicInvocationProvenance.ProtectedInputRef(
                        IntegrationCanonicalInputDigest),
                    criterion.DeterministicEvaluator.DependencyDigest,
                    criterion.DeterministicEvaluator.ConfigurationDigest,
                    OutputContentDigest = new string('2', 64),
                    StartedAt = completedAt.AddSeconds(-1),
                    FinishedAt = completedAt,
                });
        }

        var evaluationId = Guid.CreateVersion7();
        var submissionSource = new PostgresEvaluationSubmissionEvidenceSource(
            fixture.Services.ConnectionAccessor,
            artifacts);
        var submissionBundle = await submissionSource.LoadBoundItemsAsync(
            claimed.Ownership.OrganizationId,
            claimed.Ownership.ActivityId,
            claimed.Ownership.ParticipantId,
            claimed.Ownership.AttemptId,
            claimed.Ownership.SessionId,
            cancellationToken);
        if (submissionBundle is null || submissionBundle.BoundItems.Count == 0)
        {
            throw new InvalidOperationException("bound submission evidence is required for completion tests");
        }

        var submissionMaterial = submissionBundle.BoundItems[0];
        var evidenceIds = procedure.Criteria.Select(_ => Guid.CreateVersion7()).ToArray();
        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(claimed.Ownership, evaluationId);
        var locatorEntries = new EvidenceLocatorVerificationEntry[procedure.Criteria.Count];
        for (var index = 0; index < procedure.Criteria.Count; index++)
        {
            using var locatorDocument = JsonDocument.Parse(
                BuildSubmissionWholeItemLocatorJson(
                    trustedOwnership,
                    submissionMaterial.SourceId,
                    submissionMaterial.SourceVersion,
                    submissionMaterial.ContentDigest));
            locatorEntries[index] = new EvidenceLocatorVerificationEntry(
                evidenceIds[index],
                procedure.Criteria[index].CriterionId,
                locatorDocument.RootElement.Clone());
        }

        var locatorCompletionService = new EvidenceLocatorCompletionService(
            new PostgresEvaluationSessionEvidenceSource(
                fixture.Services.ConnectionAccessor,
                new PostgresEvaluationHandoffSource(fixture.Services.ConnectionAccessor)),
            submissionSource,
            new PostgresProtectedDeterministicOutputStore(fixture.Services.ConnectionAccessor),
            new PostgresEvaluationEvidenceLocatorStore(fixture.Services.ConnectionAccessor));
        var verified = await locatorCompletionService.TryVerifyAsync(
            claimed.Ownership.OrganizationId,
            claimed.Ownership.SessionId,
            procedure,
            new EvidenceLocatorCompletionRequest(
                evaluationId,
                claimed.RequestId,
                requestRow.handoff_id,
                locatorEntries),
            cancellationToken);
        if (!verified.Succeeded || verified.Value is null)
        {
            throw new InvalidOperationException($"locator verification rejected: {verified.OutcomeCode}");
        }

        var items = new List<EvidenceItem>(verified.Value.LocatorRecords.Count);
        foreach (var record in verified.Value.LocatorRecords)
        {
            var item = EvaluationCompletionPersistedEvidenceVerifier.ToAuthoritativeEvidenceItem(
                record,
                claimed.Ownership,
                evaluationId);
            if (!item.Succeeded || item.Value is null)
            {
                throw new InvalidOperationException($"evidence item rejected: {item.OutcomeCode}");
            }

            items.Add(item.Value);
        }

        var evidenceSetId = Guid.CreateVersion7();
        var evidenceSetDigest = EvaluationCompletionEvidenceSeal.TryComputeFromPersistedRecords(
            evidenceSetId,
            claimed.InvocationAttemptId,
            prepared.Request.FrozenInput,
            verified.Value.LocatorRecords);
        if (!evidenceSetDigest.Succeeded || evidenceSetDigest.Value is null)
        {
            throw new InvalidOperationException(
                $"evidence seal rejected: {evidenceSetDigest.OutcomeCode}");
        }

        var evidenceSet = EvidenceSet.TryCreate(
            evidenceSetId,
            evaluationId,
            claimed.Ownership,
            items,
            evidenceSetDigest.Value).Value!;
        var request = EvaluationRequest.TryCreate(
            claimed.RequestId,
            requestKind,
            prepared.Request.FrozenInput,
            prepared.Request.IdempotencyKey,
            prepared.Request.DelegationRef,
            EvaluationRequestStates.Completing,
            predecessorEvaluationId,
            replacementReason).Value!;
        var judgments = procedure.Criteria.Select((criterion, index) =>
        {
            object? score = criterion.EvaluatorMode == EvaluatorModes.AgentAssisted
                ? "pass"
                : criterion.EvaluatorMode == EvaluatorModes.AgentJudgment
                    ? 3
                    : null;
            var uncertainty = criterion.EvaluatorMode == EvaluatorModes.Deterministic
                ? new[] { "evaluator_bound" }
                : new[] { "ambiguous_language" };
            var created = CriterionJudgmentValidator.TryCreate(
                procedure,
                new CriterionJudgmentDraft(
                    Guid.CreateVersion7(),
                    evaluationId,
                    criterion.CriterionId,
                    criterion.CriterionVersion,
                    criterion.EvaluatorMode,
                    CriterionStatuses.Satisfied,
                    "high",
                    uncertainty,
                    "The criterion is judged against the frozen Evidence set.",
                    [evidenceIds[index]],
                    score,
                    criterion.EvaluatorMode == EvaluatorModes.Deterministic ? null : "Keep the explanation specific.",
                    criterion.EvaluatorMode == EvaluatorModes.AgentJudgment
                        ? null
                        : deterministicAttemptIds[criterion.CriterionId]));
            if (!created.Succeeded || created.Value is null)
            {
                throw new InvalidOperationException(
                    $"judgment rejected for {criterion.CriterionId}: {created.OutcomeCode}");
            }

            return created.Value;
        }).ToArray();
        var completed = EvaluationCompletionPreparer.TryPrepare(
            request,
            procedure,
            evidenceSet,
            items,
            judgments,
            completedAt,
            "evaluation.integration").Value!;

        var command = new EvaluationCompletionCommand(
            claimed.WorkId,
            claimed.RequestId,
            claimed.InvocationAttemptId,
            prepared.DelegationId,
            prepared.WorkerActorId,
            Guid.CreateVersion7(),
            "integration.test",
            completed,
            items,
            locatorEntries,
            judgments,
            [
                new EvaluationManifestRefDraft(
                    Guid.CreateVersion7(),
                    "evaluation",
                    "protected.evaluation.ref",
                    new string('a', 64)),
            ]);

        return new CompletionBundle(command);
    }

    internal static string BuildSubmissionWholeItemLocatorJson(
        EvaluationStableOwnershipReference trustedOwnership,
        string sourceId,
        string sourceVersion,
        string sourceDigest) =>
        $$"""
        {
          "locator_schema":"evidence-locator.v1",
          "source_type":"submission.direct_text",
          "source_ref":{"source_id":"{{sourceId}}","source_version":"{{sourceVersion}}"},
          "ownership_ref":{
            "organization_id":"{{trustedOwnership.OrganizationId}}",
            "activity_id":"{{trustedOwnership.ActivityId}}",
            "participant_id":"{{trustedOwnership.ParticipantId}}",
            "attempt_id":"{{trustedOwnership.AttemptId}}",
            "session_id":"{{trustedOwnership.SessionId}}",
            "evaluation_id":"{{trustedOwnership.EvaluationId}}"
          },
          "location":{"location_type":"whole_item","item_id":"{{sourceId}}"},
          "precision":"whole_item",
          "integrity":{
            "source_digest":"{{sourceDigest}}",
            "adapter_version":"locator-adapter.v1",
            "verification_state":"verified"
          },
          "created_by":{"service_id":"evaluation.integration","invocation_id":"inv.completion.test"}
        }
        """;

    private sealed record RequestRow(
        string handoff_id,
        Guid rubric_source_id,
        Guid rubric_source_version_id,
        string rubric_content_digest);
}
