using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record StoredCompletionSnapshot(
    Guid EvaluationId,
    Guid EvidenceSetId,
    string ProcedureDigest,
    string AggregateStatus,
    string EvidenceSetDigest,
    IReadOnlyList<StoredJudgmentSnapshot> Judgments);

public sealed record StoredJudgmentSnapshot(
    Guid JudgmentId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    string Status,
    string Confidence,
    string UncertaintyJson,
    string Rationale,
    string? ScoreJson,
    string? ProvisionalFeedback,
    Guid? DeterministicAttemptId);

public static class EvaluationCompletionEquivalence
{
    public static bool IsEquivalent(
        CompletedEvaluation completed,
        StoredCompletionSnapshot stored,
        IReadOnlyList<CriterionJudgment> commandJudgments)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(commandJudgments);

        return completed.EvaluationId == stored.EvaluationId
            && completed.EvidenceSetId == stored.EvidenceSetId
            && string.Equals(completed.ProcedureRef.ContentDigest, stored.ProcedureDigest, StringComparison.Ordinal)
            && string.Equals(completed.AggregateStatus, stored.AggregateStatus, StringComparison.Ordinal)
            && string.Equals(completed.EvidenceSetDigest, stored.EvidenceSetDigest, StringComparison.Ordinal)
            && JudgmentsEquivalent(commandJudgments, stored.Judgments);
    }

    public static bool JudgmentsEquivalent(
        IReadOnlyList<CriterionJudgment> command,
        IReadOnlyList<StoredJudgmentSnapshot> stored)
    {
        if (command.Count != stored.Count)
        {
            return false;
        }

        var orderedCommand = command
            .OrderBy(judgment => judgment.CriterionId, StringComparer.Ordinal)
            .ThenBy(judgment => judgment.JudgmentId)
            .ToArray();
        var orderedStored = stored
            .OrderBy(judgment => judgment.CriterionId, StringComparer.Ordinal)
            .ThenBy(judgment => judgment.JudgmentId)
            .ToArray();
        for (var index = 0; index < orderedCommand.Length; index++)
        {
            if (!JudgmentMatches(orderedCommand[index], orderedStored[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool JudgmentMatches(CriterionJudgment command, StoredJudgmentSnapshot stored) =>
        command.JudgmentId == stored.JudgmentId
        && string.Equals(command.CriterionId, stored.CriterionId, StringComparison.Ordinal)
        && string.Equals(command.CriterionVersion, stored.CriterionVersion, StringComparison.Ordinal)
        && string.Equals(command.EvaluatorMode, stored.EvaluatorMode, StringComparison.Ordinal)
        && string.Equals(command.Status, stored.Status, StringComparison.Ordinal)
        && string.Equals(command.Confidence, stored.Confidence, StringComparison.Ordinal)
        && string.Equals(command.Rationale, stored.Rationale, StringComparison.Ordinal)
        && string.Equals(command.ProvisionalFeedback, stored.ProvisionalFeedback, StringComparison.Ordinal)
        && command.DeterministicInvocationId == stored.DeterministicAttemptId
        && string.Equals(
            JsonSerializer.Serialize(command.Uncertainty),
            stored.UncertaintyJson,
            StringComparison.Ordinal)
        && string.Equals(
            command.Score is null ? null : JsonSerializer.Serialize(command.Score),
            stored.ScoreJson,
            StringComparison.Ordinal);
}
