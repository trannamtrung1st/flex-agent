using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public sealed record EvaluationModelInvocationContext(
    string CriterionId,
    string CriterionVersion);

public static class AgentEvaluatorOrchestrationValidator
{
    public static EvaluationDecision<EvaluationProcedureCriterionV1> TryValidateInvocation(
        EvaluationProcedureV1 procedure,
        EvaluationModelInvocationContext context,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(context);

        var criterion = procedure.Criteria.FirstOrDefault(item =>
            string.Equals(item.CriterionId, context.CriterionId, StringComparison.Ordinal)
            && string.Equals(item.CriterionVersion, context.CriterionVersion, StringComparison.Ordinal));
        if (criterion is null)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "criterion");
        }

        if (criterion.EvaluatorMode == EvaluatorModes.Deterministic)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "evaluator_mode");
        }

        if (criterion.AgentIo is null)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.InvalidProcedure,
                "agent_io");
        }

        if (criterion.EvaluatorMode == EvaluatorModes.AgentAssisted)
        {
            if (criterion.DeterministicEvaluator is null)
            {
                return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                    EvaluationFailureCodes.InvalidProcedure,
                    "deterministic_evaluator");
            }

            if (verifiedDeterministicFacts is null || verifiedDeterministicFacts.Count == 0)
            {
                return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }
        }

        if (criterion.EvaluatorMode == EvaluatorModes.AgentJudgment
            && verifiedDeterministicFacts is { Count: > 0 })
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.DeterministicConflict,
                "deterministic_facts");
        }

        return EvaluationDecision<EvaluationProcedureCriterionV1>.Ok(criterion);
    }
}
