using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationCompletionPreparerTests
{
    [Fact]
    public void Completing_request_with_full_criteria_prepares_completed_evaluation()
    {
        var context = CreateContext();
        var prepared = EvaluationCompletionPreparer.TryPrepare(
            context.Request,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.True(prepared.Succeeded, prepared.OutcomeCode);
        Assert.Equal(EvaluationAggregateStatuses.Complete, prepared.Value!.AggregateStatus);
    }

    [Fact]
    public void Non_completing_request_is_rejected()
    {
        var context = CreateContext();
        var queued = context.Request with { State = EvaluationRequestStates.Queued };
        var prepared = EvaluationCompletionPreparer.TryPrepare(
            queued,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.False(prepared.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, prepared.OutcomeCode);
        Assert.Equal("state", prepared.Field);
    }

    [Fact]
    public void Missing_criterion_blocks_preparation()
    {
        var context = CreateContext();
        var prepared = EvaluationCompletionPreparer.TryPrepare(
            context.Request,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            [context.Judgments[0]],
            DateTimeOffset.UtcNow,
            "evaluation-service");

        Assert.False(prepared.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteCriteria, prepared.OutcomeCode);
    }

    private static CompletionContext CreateContext()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var frozen = EvaluationFixtures.CreateFrozenInput().Value!;
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
            EvidenceItem.TryCreate(
                id,
                "submission.direct_text",
                EvaluationFixtures.Submission(),
                frozen.Ownership,
                evaluationId,
                "exact_range").Value!).ToArray();
        var set = EvidenceSet.TryCreate(
            Guid.NewGuid(),
            evaluationId,
            frozen.Ownership,
            items,
            new string('d', 64)).Value!;
        var judgments = procedure.Criteria.Select((criterion, index) =>
        {
            object? score = criterion.EvaluatorMode == EvaluatorModes.AgentAssisted
                ? "pass"
                : criterion.EvaluatorMode == EvaluatorModes.AgentJudgment
                    ? 3
                    : null;
            IReadOnlyList<string> uncertainty = criterion.EvaluatorMode == EvaluatorModes.Deterministic
                ? new[] { "evaluator_bound" }
                : new[] { "ambiguous_language" };
            var created = CriterionJudgmentValidator.TryCreate(
                procedure,
                new CriterionJudgmentDraft(
                    Guid.NewGuid(),
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
                    criterion.EvaluatorMode == EvaluatorModes.AgentJudgment ? null : Guid.NewGuid()));
            Assert.True(created.Succeeded, created.OutcomeCode);
            return created.Value!;
        }).ToArray();
        return new CompletionContext(request, procedure, set, items, judgments);
    }

    private sealed record CompletionContext(
        EvaluationRequest Request,
        EvaluationProcedureV1 Procedure,
        EvidenceSet EvidenceSet,
        IReadOnlyList<EvidenceItem> EvidenceItems,
        IReadOnlyList<CriterionJudgment> Judgments);
}
