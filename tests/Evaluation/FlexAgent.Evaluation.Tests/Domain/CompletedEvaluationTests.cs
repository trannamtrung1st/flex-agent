using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class CompletedEvaluationTests
{
    [Fact]
    public void Complete_three_mode_evaluation_is_accepted()
    {
        var built = Build(Array.Empty<JudgmentOverride>());
        Assert.True(built.Succeeded, built.OutcomeCode);
        Assert.Equal(EvaluationAggregateStatuses.Complete, built.Value!.AggregateStatus);
        Assert.Equal(3, built.Value.CriterionIds.Count);
    }

    [Fact]
    public void Missing_criterion_blocks_completion()
    {
        var context = CreateContext();
        var result = CompletedEvaluation.TryCreate(
            context.EvaluationId,
            context.Request,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            [context.Judgments[0]],
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteCriteria, result.OutcomeCode);
    }

    [Fact]
    public void Uncited_evidence_fails_citation_integrity()
    {
        var context = CreateContext();
        var extra = EvidenceItem.TryCreate(
            Guid.NewGuid(),
            "deterministic.fact",
            EvaluationFixtures.Submission(),
            context.Frozen.Ownership,
            context.EvaluationId,
            "exact_range").Value!;
        var items = context.EvidenceItems.Concat([extra]).ToArray();
        var set = EvidenceSet.TryCreate(
            Guid.NewGuid(),
            context.EvaluationId,
            context.Frozen.Ownership,
            items,
            new string('e', 64)).Value!;

        var result = CompletedEvaluation.TryCreate(
            context.EvaluationId,
            context.Request,
            context.Procedure,
            set,
            items,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
    }

    [Fact]
    public void Evidence_source_type_outside_the_criterion_allowlist_fails_citation_integrity()
    {
        var context = CreateContext();
        var disallowed = EvidenceItem.TryCreate(
            context.EvidenceItems[0].EvidenceId,
            "session.transcript_item",
            EvaluationFixtures.Submission(),
            context.Frozen.Ownership,
            context.EvaluationId,
            "exact_range").Value!;
        var items = new[] { disallowed, context.EvidenceItems[1], context.EvidenceItems[2] };
        var set = EvidenceSet.TryCreate(
            context.EvidenceSet.EvidenceSetId,
            context.EvaluationId,
            context.Frozen.Ownership,
            items,
            context.EvidenceSet.Digest).Value!;

        var result = CompletedEvaluation.TryCreate(
            context.EvaluationId,
            context.Request,
            context.Procedure,
            set,
            items,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("activity")]
    [InlineData("participant")]
    [InlineData("attempt")]
    [InlineData("session")]
    public void Evidence_ownership_must_equal_frozen_request_ownership(string field)
    {
        var context = CreateContext();
        var foreignOwnership = EvaluationFixtures.PerturbOwnership(field);
        var items = context.EvidenceItems.Select(item =>
            EvidenceItem.TryCreate(
                item.EvidenceId,
                item.SourceType,
                item.Source,
                foreignOwnership,
                item.EvaluationId,
                item.Precision).Value!).ToArray();
        var set = EvidenceSet.TryCreate(
            context.EvidenceSet.EvidenceSetId,
            context.EvaluationId,
            foreignOwnership,
            items,
            context.EvidenceSet.Digest).Value!;

        var result = CompletedEvaluation.TryCreate(
            context.EvaluationId,
            context.Request,
            context.Procedure,
            set,
            items,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, result.OutcomeCode);
    }

    [Fact]
    public void Insufficient_criterion_blocks_all_required_satisfied_aggregation()
    {
        var built = Build(
        [
            new JudgmentOverride("crit.objective.word-count", CriterionStatuses.InsufficientEvidence, null, ["source_gap"]),
        ]);

        Assert.True(built.Succeeded, built.OutcomeCode);
        Assert.Equal(EvaluationAggregateStatuses.InsufficientEvidence, built.Value!.AggregateStatus);
    }

    [Fact]
    public void Not_satisfied_is_distinct_from_insufficient_evidence()
    {
        var built = Build(
        [
            new JudgmentOverride("crit.objective.word-count", CriterionStatuses.NotSatisfied, null, ["evaluator_bound"]),
        ]);

        Assert.True(built.Succeeded, built.OutcomeCode);
        Assert.Equal(EvaluationAggregateStatuses.RequirementsNotSatisfied, built.Value!.AggregateStatus);
    }

    [Fact]
    public void Conflict_requires_review_and_preserves_completion()
    {
        var built = Build(
        [
            new JudgmentOverride("crit.assisted.structure", CriterionStatuses.Conflict, "fail", ["ambiguous_language"]),
        ]);

        Assert.True(built.Succeeded, built.OutcomeCode);
        Assert.Equal(EvaluationAggregateStatuses.ConflictReviewRequired, built.Value!.AggregateStatus);
    }

    [Fact]
    public void Not_applicable_is_excluded_from_all_required_satisfied()
    {
        var built = Build(
        [
            new JudgmentOverride("crit.judgment.quality", CriterionStatuses.NotApplicable, 0, ["limited_context"]),
        ]);

        Assert.True(built.Succeeded, built.OutcomeCode);
        Assert.Equal(EvaluationAggregateStatuses.Complete, built.Value!.AggregateStatus);
    }

    private static EvaluationDecision<CompletedEvaluation> Build(IReadOnlyList<JudgmentOverride> overrides)
    {
        var context = CreateContext(overrides);
        return CompletedEvaluation.TryCreate(
            context.EvaluationId,
            context.Request,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service");
    }

    private static CompletionContext CreateContext(IReadOnlyList<JudgmentOverride>? overrides = null)
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var frozenResult = EvaluationFixtures.CreateFrozenInput();
        Assert.True(frozenResult.Succeeded, frozenResult.OutcomeCode);
        var frozen = frozenResult.Value!;
        var request = EvaluationRequest.TryCreate(
            Guid.NewGuid(),
            EvaluationRequestKinds.Initial,
            frozen,
            "idem-eval-complete",
            "deleg.eval.synthetic",
            EvaluationRequestStates.Completing).Value!;
        var evaluationId = Guid.NewGuid();
        var evidenceIds = procedure.Criteria.Select(_ => Guid.NewGuid()).ToArray();
        var items = evidenceIds.Select(id =>
        {
            var created = EvidenceItem.TryCreate(
                id,
                "submission.direct_text",
                EvaluationFixtures.Submission(),
                frozen.Ownership,
                evaluationId,
                "exact_range");
            Assert.True(created.Succeeded, created.OutcomeCode);
            return created.Value!;
        }).ToArray();
        var setResult = EvidenceSet.TryCreate(
            Guid.NewGuid(),
            evaluationId,
            frozen.Ownership,
            items,
            new string('d', 64));
        Assert.True(setResult.Succeeded, setResult.OutcomeCode);
        var set = setResult.Value!;
        var judgments = procedure.Criteria.Select((criterion, index) =>
        {
            var overrideValue = overrides?.FirstOrDefault(item =>
                string.Equals(item.CriterionId, criterion.CriterionId, StringComparison.Ordinal));
            var created = CriterionJudgmentValidator.TryCreate(
                procedure,
                Draft(
                    evaluationId,
                    evidenceIds[index],
                    criterion,
                    overrideValue));
            Assert.True(created.Succeeded, created.OutcomeCode);
            return created.Value!;
        }).ToArray();

        return new CompletionContext(evaluationId, frozen, request, procedure, set, items, judgments);
    }

    private static CriterionJudgmentDraft Draft(
        Guid evaluationId,
        Guid evidenceId,
        EvaluationProcedureCriterionV1 criterion,
        JudgmentOverride? overrideValue)
    {
        var status = overrideValue?.Status ?? CriterionStatuses.Satisfied;
        object? score = overrideValue?.Score ?? (criterion.EvaluatorMode == EvaluatorModes.AgentAssisted
            ? "pass"
            : criterion.EvaluatorMode == EvaluatorModes.AgentJudgment
                ? 3
                : null);
        var uncertainty = overrideValue?.Uncertainty
            ?? (criterion.EvaluatorMode == EvaluatorModes.Deterministic
                ? ["evaluator_bound"]
                : ["ambiguous_language"]);
        return new CriterionJudgmentDraft(
            Guid.NewGuid(),
            evaluationId,
            criterion.CriterionId,
            criterion.CriterionVersion,
            criterion.EvaluatorMode,
            status,
            "high",
            uncertainty,
            "The criterion is judged against the frozen Evidence set.",
            [evidenceId],
            score,
            criterion.EvaluatorMode == EvaluatorModes.Deterministic ? null : "Keep the explanation specific.",
            criterion.EvaluatorMode == EvaluatorModes.AgentJudgment ? null : Guid.NewGuid());
    }

    private sealed record JudgmentOverride(
        string CriterionId,
        string Status,
        object? Score,
        IReadOnlyList<string> Uncertainty);

    private sealed record CompletionContext(
        Guid EvaluationId,
        FrozenInputIdentity Frozen,
        EvaluationRequest Request,
        EvaluationProcedureV1 Procedure,
        EvidenceSet EvidenceSet,
        IReadOnlyList<EvidenceItem> EvidenceItems,
        IReadOnlyList<CriterionJudgment> Judgments);
}
