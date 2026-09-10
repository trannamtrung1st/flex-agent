using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationModelResponseValidator
{
    public static EvaluationDecision<CriterionJudgmentDraft> TryValidate(
        EvaluationProcedureV1 procedure,
        CriterionJudgmentDraft draft,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(draft);

        var criterionDecision = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            procedure,
            new EvaluationModelInvocationContext(draft.CriterionId, draft.CriterionVersion),
            verifiedDeterministicFacts);
        if (!criterionDecision.Succeeded || criterionDecision.Value is null)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                criterionDecision.OutcomeCode,
                criterionDecision.Field);
        }

        if (EvaluationModelResponseValidatorHelpers.TryDetectDeterministicConflict(
                criterionDecision.Value,
                draft,
                verifiedDeterministicFacts))
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                EvaluationFailureCodes.DeterministicConflict,
                "status");
        }

        var judgment = CriterionJudgmentValidator.TryCreate(procedure, draft);
        if (!judgment.Succeeded)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                judgment.OutcomeCode,
                judgment.Field);
        }

        return EvaluationDecision<CriterionJudgmentDraft>.Ok(draft);
    }
}

internal static class EvaluationModelResponseValidatorHelpers
{
    internal static bool TryDetectDeterministicConflict(
        EvaluationProcedureCriterionV1 criterion,
        CriterionJudgmentDraft draft,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        if (criterion.EvaluatorMode != EvaluatorModes.AgentAssisted
            || verifiedDeterministicFacts is null
            || verifiedDeterministicFacts.Count == 0
            || draft.Status != CriterionStatuses.Satisfied)
        {
            return false;
        }

        foreach (var fact in verifiedDeterministicFacts.Values)
        {
            var projection = System.Text.Encoding.UTF8.GetString(fact.ProjectionUtf8.Span);
            if (projection.Contains("\"valid\":false", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
