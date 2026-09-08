using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceLocatorProcedurePolicy
{
    public static EvaluationDecision<bool> TryResolveWholeItemFallbackPermitted(
        EvaluationProcedureV1 procedure,
        string criterionId)
    {
        ArgumentNullException.ThrowIfNull(procedure);

        if (string.IsNullOrWhiteSpace(criterionId))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidField, "criterion_id");
        }

        var criterion = procedure.Criteria.FirstOrDefault(item =>
            string.Equals(item.CriterionId, criterionId, StringComparison.Ordinal));
        if (criterion is null)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.InvalidProcedure, "criterion_id");
        }

        return EvaluationDecision<bool>.Ok(criterion.EvidenceRequirements.WholeItemFallbackPermitted);
    }
}
