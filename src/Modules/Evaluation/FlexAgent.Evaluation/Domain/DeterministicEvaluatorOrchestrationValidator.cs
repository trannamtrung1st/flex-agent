using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class DeterministicEvaluatorOrchestrationValidator
{
    public static EvaluationDecision<EvaluationProcedureCriterionV1> TryValidateRequest(
        EvaluationProcedureV1 procedure,
        DeterministicEvaluatorExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(request);

        var criterion = procedure.Criteria.FirstOrDefault(item =>
            string.Equals(item.CriterionId, request.CriterionId, StringComparison.Ordinal)
            && string.Equals(item.CriterionVersion, request.CriterionVersion, StringComparison.Ordinal));
        if (criterion is null)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "criterion");
        }

        if (criterion.EvaluatorMode == EvaluatorModes.AgentJudgment)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "evaluator_mode");
        }

        if (criterion.DeterministicEvaluator is null)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                EvaluationFailureCodes.InvalidProcedure,
                "deterministic_evaluator");
        }

        var bindingMatch = TryMatchFrozenBinding(criterion.DeterministicEvaluator, request.Binding);
        if (!bindingMatch.Succeeded)
        {
            return EvaluationDecision<EvaluationProcedureCriterionV1>.Fail(
                bindingMatch.OutcomeCode,
                bindingMatch.Field);
        }

        return EvaluationDecision<EvaluationProcedureCriterionV1>.Ok(criterion);
    }

    private static EvaluationDecision<bool> TryMatchFrozenBinding(
        DeterministicEvaluatorBindingV1 frozen,
        DeterministicEvaluatorBindingV1 request)
    {
        if (!string.Equals(frozen.EvaluatorId, request.EvaluatorId, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "evaluator_id");
        }

        if (!string.Equals(frozen.EvaluatorVersion, request.EvaluatorVersion, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "evaluator_version");
        }

        if (!string.Equals(frozen.EvaluatorDigest, request.EvaluatorDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "evaluator_digest");
        }

        if (!string.Equals(frozen.Operation, request.Operation, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "operation");
        }

        if (!string.Equals(frozen.InputSchemaId, request.InputSchemaId, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "input_schema_id");
        }

        if (!string.Equals(frozen.OutputSchemaId, request.OutputSchemaId, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "output_schema_id");
        }

        if (!string.Equals(frozen.CanonicalizationProcedure, request.CanonicalizationProcedure, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "canonicalization_procedure");
        }

        if (!string.Equals(frozen.ConfigurationDigest, request.ConfigurationDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "configuration_digest");
        }

        if (!string.Equals(frozen.DependencyDigest, request.DependencyDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "dependency_digest");
        }

        if (!string.Equals(frozen.CpuTimeLimit, request.CpuTimeLimit, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "cpu_time_limit");
        }

        if (!string.Equals(frozen.ElapsedTimeLimit, request.ElapsedTimeLimit, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "elapsed_time_limit");
        }

        if (frozen.MemoryLimitBytes != request.MemoryLimitBytes)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "memory_limit_bytes");
        }

        if (frozen.OutputLimitBytes != request.OutputLimitBytes)
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "output_limit_bytes");
        }

        if (!string.Equals(frozen.NetworkEgress, request.NetworkEgress, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "network_egress");
        }

        if (!string.Equals(frozen.ExecutableSelection, request.ExecutableSelection, StringComparison.Ordinal))
        {
            return EvaluationDecision<bool>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator, "executable_selection");
        }

        return EvaluationDecision<bool>.Ok(true);
    }
}
