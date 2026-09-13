using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record DeterministicAttemptProvenanceRow(
    Guid DeterministicAttemptId,
    Guid InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorId,
    string EvaluatorVersion,
    string EvaluatorDigest,
    string DependencyDigest,
    string ConfigurationDigest,
    string CanonicalInputDigest,
    string Outcome);

public static class EvaluationCompletionDeterministicProvenanceVerifier
{
    public static bool IsJudgmentProvenanceValid(
        CriterionJudgment judgment,
        EvaluationProcedureCriterionV1 criterion,
        Guid invocationAttemptId,
        IReadOnlyList<DeterministicAttemptProvenanceRow> invocationAttempts,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> authoritativeEvidence,
        out string? failureField)
    {
        ArgumentNullException.ThrowIfNull(judgment);
        ArgumentNullException.ThrowIfNull(criterion);
        ArgumentNullException.ThrowIfNull(invocationAttempts);
        ArgumentNullException.ThrowIfNull(authoritativeEvidence);
        failureField = null;

        if (judgment.DeterministicInvocationId is null)
        {
            return true;
        }

        if (criterion.DeterministicEvaluator is null)
        {
            failureField = "deterministic_evaluator";
            return false;
        }

        var citedAttempt = invocationAttempts.FirstOrDefault(attempt =>
            attempt.DeterministicAttemptId == judgment.DeterministicInvocationId);
        if (citedAttempt is null)
        {
            failureField = "deterministic_attempt_id";
            return false;
        }

        if (!MatchesEvaluatorBinding(citedAttempt, criterion.DeterministicEvaluator)
            || citedAttempt.InvocationAttemptId != invocationAttemptId
            || !string.Equals(citedAttempt.CriterionId, judgment.CriterionId, StringComparison.Ordinal)
            || !string.Equals(citedAttempt.CriterionVersion, judgment.CriterionVersion, StringComparison.Ordinal)
            || !string.Equals(citedAttempt.Outcome, "succeeded", StringComparison.Ordinal))
        {
            failureField = "deterministic_attempt_id";
            return false;
        }

        var succeededAttempts = invocationAttempts
            .Where(attempt =>
                attempt.InvocationAttemptId == invocationAttemptId
                && string.Equals(attempt.CriterionId, judgment.CriterionId, StringComparison.Ordinal)
                && string.Equals(attempt.CriterionVersion, judgment.CriterionVersion, StringComparison.Ordinal)
                && MatchesEvaluatorBinding(attempt, criterion.DeterministicEvaluator)
                && string.Equals(attempt.Outcome, "succeeded", StringComparison.Ordinal))
            .ToArray();
        var distinctDigests = succeededAttempts
            .Select(attempt => attempt.CanonicalInputDigest)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var expectedDigest = distinctDigests.Length switch
        {
            0 => null,
            1 => distinctDigests[0],
            _ => TryResolveCanonicalInputFromEvidence(
                judgment,
                authoritativeEvidence,
                invocationAttempts),
        };
        if (expectedDigest is null
            || !string.Equals(citedAttempt.CanonicalInputDigest, expectedDigest, StringComparison.Ordinal))
        {
            failureField = "canonical_input_digest";
            return false;
        }

        return true;
    }

    private static string? TryResolveCanonicalInputFromEvidence(
        CriterionJudgment judgment,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> authoritativeEvidence,
        IReadOnlyList<DeterministicAttemptProvenanceRow> invocationAttempts)
    {
        var evidenceById = authoritativeEvidence.ToDictionary(record => record.EvidenceId);
        foreach (var evidenceId in judgment.EvidenceIds)
        {
            if (!evidenceById.TryGetValue(evidenceId, out var record)
                || !string.Equals(record.SourceType, "deterministic.fact", StringComparison.Ordinal))
            {
                continue;
            }

            var boundAttempt = invocationAttempts.FirstOrDefault(attempt =>
                attempt.DeterministicAttemptId == record.SourceId
                && string.Equals(attempt.Outcome, "succeeded", StringComparison.Ordinal));
            if (boundAttempt is null)
            {
                continue;
            }

            return boundAttempt.CanonicalInputDigest;
        }

        return null;
    }

    private static bool MatchesEvaluatorBinding(
        DeterministicAttemptProvenanceRow attempt,
        DeterministicEvaluatorBindingV1 binding) =>
        string.Equals(attempt.EvaluatorId, binding.EvaluatorId, StringComparison.Ordinal)
        && string.Equals(attempt.EvaluatorVersion, binding.EvaluatorVersion, StringComparison.Ordinal)
        && string.Equals(attempt.EvaluatorDigest, binding.EvaluatorDigest, StringComparison.Ordinal)
        && string.Equals(attempt.DependencyDigest, binding.DependencyDigest, StringComparison.Ordinal)
        && string.Equals(attempt.ConfigurationDigest, binding.ConfigurationDigest, StringComparison.Ordinal);
}
