using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public sealed record DeterministicInvocationProvenanceSnapshot(
    Guid InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorId,
    string EvaluatorVersion,
    string EvaluatorDigest,
    string ConfigurationDigest,
    string DependencyDigest,
    string CanonicalInputDigest,
    string? ProtectedInputRef,
    string? ProtectedOutputRef,
    string? OutputContentDigest,
    string Outcome,
    string? FailureCategory);

public static class DeterministicInvocationProvenance
{
    public static DeterministicInvocationProvenanceSnapshot FromAppendCommand(
        DeterministicInvocationAppendCommand command)
    {
        return new DeterministicInvocationProvenanceSnapshot(
            command.InvocationAttemptId,
            command.CriterionId,
            command.CriterionVersion,
            command.Binding.EvaluatorId,
            command.Binding.EvaluatorVersion,
            command.Binding.EvaluatorDigest,
            command.Binding.ConfigurationDigest,
            command.Binding.DependencyDigest,
            command.CanonicalInputDigest,
            command.Result.ProtectedInputRef,
            command.Result.ProtectedOutputRef,
            command.Result.OutputContentDigest,
            DeterministicInvocationOutcomes.ToPersistenceOutcome(command.Result.Outcome),
            command.Result.FailureCategory);
    }

    public static bool IsEquivalentRetry(
        DeterministicInvocationProvenanceSnapshot existing,
        DeterministicInvocationProvenanceSnapshot candidate) =>
        existing.InvocationAttemptId == candidate.InvocationAttemptId
        && string.Equals(existing.CriterionId, candidate.CriterionId, StringComparison.Ordinal)
        && string.Equals(existing.CriterionVersion, candidate.CriterionVersion, StringComparison.Ordinal)
        && string.Equals(existing.EvaluatorId, candidate.EvaluatorId, StringComparison.Ordinal)
        && string.Equals(existing.EvaluatorVersion, candidate.EvaluatorVersion, StringComparison.Ordinal)
        && string.Equals(existing.EvaluatorDigest, candidate.EvaluatorDigest, StringComparison.Ordinal)
        && string.Equals(existing.ConfigurationDigest, candidate.ConfigurationDigest, StringComparison.Ordinal)
        && string.Equals(existing.DependencyDigest, candidate.DependencyDigest, StringComparison.Ordinal)
        && string.Equals(existing.CanonicalInputDigest, candidate.CanonicalInputDigest, StringComparison.Ordinal)
        && string.Equals(existing.ProtectedInputRef, candidate.ProtectedInputRef, StringComparison.Ordinal)
        && string.Equals(existing.ProtectedOutputRef, candidate.ProtectedOutputRef, StringComparison.Ordinal)
        && string.Equals(existing.OutputContentDigest, candidate.OutputContentDigest, StringComparison.Ordinal)
        && string.Equals(existing.Outcome, candidate.Outcome, StringComparison.Ordinal)
        && string.Equals(existing.FailureCategory, candidate.FailureCategory, StringComparison.Ordinal);

    public static string ProtectedInputRef(string canonicalInputDigest) =>
        $"prot.eval.det-in.{canonicalInputDigest}";

    public static string? ProtectedOutputRef(string? outputContentDigest) =>
        outputContentDigest is null
            ? null
            : $"prot.eval.det-out.{outputContentDigest}";
}
