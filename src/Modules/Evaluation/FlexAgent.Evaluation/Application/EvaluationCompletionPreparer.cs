using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationCompletionPreparer
{
    public static EvaluationDecision<CompletedEvaluation> TryPrepare(
        EvaluationRequest request,
        EvaluationProcedureV1 procedure,
        EvidenceSet evidenceSet,
        IReadOnlyList<EvidenceItem> evidenceItems,
        IReadOnlyList<CriterionJudgment> judgments,
        DateTimeOffset completedAtUtc,
        string creationServiceId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(evidenceSet);
        ArgumentNullException.ThrowIfNull(evidenceItems);
        ArgumentNullException.ThrowIfNull(judgments);

        if (!string.Equals(request.State, EvaluationRequestStates.Completing, StringComparison.Ordinal))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(
                EvaluationFailureCodes.InvalidField,
                "state");
        }

        return CompletedEvaluation.TryCreate(
            evidenceSet.EvaluationId,
            request,
            procedure,
            evidenceSet,
            evidenceItems,
            judgments,
            completedAtUtc,
            creationServiceId);
    }
}
