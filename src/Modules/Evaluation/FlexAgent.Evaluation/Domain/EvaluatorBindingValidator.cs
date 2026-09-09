using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluatorBindingValidator
{
    public const int MinMemoryLimitBytes = 1;

    public const int MaxMemoryLimitBytes = 268_435_456;

    public const int MinOutputLimitBytes = 1;

    public const int MaxOutputLimitBytes = 1_048_576;

    public static EvaluationDecision<EvaluatorRegistryEntry> TryValidateBinding(
        EvaluatorRegistrySnapshot registry,
        DeterministicEvaluatorBindingV1 binding)
    {
        if (EvaluationIdentity.ContainsMutableAlias(binding.EvaluatorId))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.MutableAlias,
                "evaluator_id");
        }

        if (EvaluationIdentity.ContainsMutableAlias(binding.EvaluatorVersion))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.MutableAlias,
                "evaluator_version");
        }

        if (!registry.Entries.TryGetValue(binding.EvaluatorId, out var entry))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "evaluator_id");
        }

        if (!string.Equals(entry.QualificationState, EvaluatorQualificationStates.Qualified, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "evaluator_id");
        }

        if (!string.Equals(binding.EvaluatorVersion, entry.EvaluatorVersion, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "evaluator_version");
        }

        if (!string.Equals(binding.EvaluatorDigest, entry.EvaluatorDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "evaluator_digest");
        }

        if (!string.Equals(binding.Operation, entry.Operation, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "operation");
        }

        if (!string.Equals(binding.InputSchemaId, entry.InputSchemaId, StringComparison.Ordinal)
            || !string.Equals(binding.OutputSchemaId, entry.OutputSchemaId, StringComparison.Ordinal)
            || !string.Equals(binding.CanonicalizationProcedure, entry.CanonicalizationProcedure, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "input_schema_id");
        }

        if (!string.Equals(binding.ConfigurationDigest, entry.ConfigurationDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "configuration_digest");
        }

        if (!string.Equals(binding.DependencyDigest, entry.DependencyDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "dependency_digest");
        }

        if (!string.Equals(binding.NetworkEgress, entry.NetworkEgress, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "network_egress");
        }

        if (!string.Equals(binding.ExecutableSelection, entry.ExecutableSelection, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "executable_selection");
        }

        if (binding.MemoryLimitBytes < MinMemoryLimitBytes
            || binding.MemoryLimitBytes > MaxMemoryLimitBytes)
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "memory_limit_bytes");
        }

        if (binding.OutputLimitBytes < MinOutputLimitBytes
            || binding.OutputLimitBytes > MaxOutputLimitBytes)
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "output_limit_bytes");
        }

        if (binding.MemoryLimitBytes > entry.MemoryLimitBytes)
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "memory_limit_bytes");
        }

        if (binding.OutputLimitBytes > entry.OutputLimitBytes)
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "output_limit_bytes");
        }

        if (!EvaluationPositiveDuration.TryParseTotalSeconds(binding.CpuTimeLimit, out var cpuSeconds))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "cpu_time_limit");
        }

        if (!EvaluationPositiveDuration.TryParseTotalSeconds(binding.ElapsedTimeLimit, out var elapsedSeconds))
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "elapsed_time_limit");
        }

        if (!EvaluationPositiveDuration.TryParseTotalSeconds(entry.CpuTimeLimit, out var entryCpuLimitSeconds)
            || cpuSeconds > entryCpuLimitSeconds)
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "cpu_time_limit");
        }

        if (!EvaluationPositiveDuration.TryParseTotalSeconds(entry.ElapsedTimeLimit, out var entryElapsedLimitSeconds)
            || elapsedSeconds > entryElapsedLimitSeconds)
        {
            return EvaluationDecision<EvaluatorRegistryEntry>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "elapsed_time_limit");
        }

        return EvaluationDecision<EvaluatorRegistryEntry>.Ok(entry);
    }
}
