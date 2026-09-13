using System.Text.Json;
using System.Text.Json.Nodes;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record StoredCompletionSnapshot(
    Guid EvaluationId,
    Guid RequestId,
    EvaluationOwnership Ownership,
    Guid ProcedureSourceId,
    Guid ProcedureSourceVersionId,
    string ProcedureDigest,
    string AggregateStatus,
    Guid EvidenceSetId,
    string EvidenceSetDigest,
    DateTimeOffset CompletedAtUtc,
    string CreationServiceId,
    Guid? PredecessorEvaluationId,
    IReadOnlyList<StoredJudgmentSnapshot> Judgments,
    IReadOnlyList<StoredManifestRefSnapshot> ManifestRefs);

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

public sealed record StoredManifestRefSnapshot(
    string RefKind,
    string ProtectedRef,
    string ContentDigest);

public static class EvaluationCompletionEquivalence
{
    public static bool IsEquivalent(
        CompletedEvaluation completed,
        StoredCompletionSnapshot stored,
        IReadOnlyList<CriterionJudgment> commandJudgments,
        IReadOnlyList<EvaluationManifestRefDraft> commandManifestRefs,
        EvaluationRequest authoritativeRequest)
    {
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(commandJudgments);
        ArgumentNullException.ThrowIfNull(commandManifestRefs);
        ArgumentNullException.ThrowIfNull(authoritativeRequest);

        return completed.EvaluationId == stored.EvaluationId
            && completed.RequestId == stored.RequestId
            && completed.RequestId == authoritativeRequest.RequestId
            && completed.Ownership == stored.Ownership
            && completed.Ownership == authoritativeRequest.FrozenInput.Ownership
            && completed.ProcedureRef.SourceId == stored.ProcedureSourceId
            && completed.ProcedureRef.SourceVersionId == stored.ProcedureSourceVersionId
            && string.Equals(completed.ProcedureRef.ContentDigest, stored.ProcedureDigest, StringComparison.Ordinal)
            && string.Equals(completed.ProcedureRef.ContentDigest, authoritativeRequest.FrozenInput.Rubric.ContentDigest, StringComparison.Ordinal)
            && completed.FrozenInput == authoritativeRequest.FrozenInput
            && completed.EvidenceSetId == stored.EvidenceSetId
            && string.Equals(completed.AggregateStatus, stored.AggregateStatus, StringComparison.Ordinal)
            && string.Equals(completed.EvidenceSetDigest, stored.EvidenceSetDigest, StringComparison.Ordinal)
            && EvaluationCompletionTimestampCanonicalization.AreEquivalent(
                completed.CompletedAtUtc,
                stored.CompletedAtUtc)
            && string.Equals(completed.CreationServiceId, stored.CreationServiceId, StringComparison.Ordinal)
            && completed.PredecessorEvaluationId == stored.PredecessorEvaluationId
            && completed.PredecessorEvaluationId == authoritativeRequest.PredecessorEvaluationId
            && JudgmentsEquivalent(commandJudgments, stored.Judgments)
            && ManifestRefsEquivalent(commandManifestRefs, stored.ManifestRefs);
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

    private static bool ManifestRefsEquivalent(
        IReadOnlyList<EvaluationManifestRefDraft> command,
        IReadOnlyList<StoredManifestRefSnapshot> stored)
    {
        if (command.Count != stored.Count)
        {
            return false;
        }

        var orderedCommand = command
            .OrderBy(reference => reference.RefKind, StringComparer.Ordinal)
            .ThenBy(reference => reference.ProtectedRef, StringComparer.Ordinal)
            .ToArray();
        var orderedStored = stored
            .OrderBy(reference => reference.RefKind, StringComparer.Ordinal)
            .ThenBy(reference => reference.ProtectedRef, StringComparer.Ordinal)
            .ToArray();
        for (var index = 0; index < orderedCommand.Length; index++)
        {
            if (!string.Equals(orderedCommand[index].RefKind, orderedStored[index].RefKind, StringComparison.Ordinal)
                || !string.Equals(orderedCommand[index].ProtectedRef, orderedStored[index].ProtectedRef, StringComparison.Ordinal)
                || !string.Equals(orderedCommand[index].ContentDigest, orderedStored[index].ContentDigest, StringComparison.Ordinal))
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
        && JsonValuesEquivalent(JsonSerializer.Serialize(command.Uncertainty), stored.UncertaintyJson)
        && JsonValuesEquivalent(
            command.Score is null ? null : JsonSerializer.Serialize(command.Score),
            stored.ScoreJson);

    internal static bool JsonValuesEquivalent(string? expectedJson, string? storedJson)
    {
        if (expectedJson is null && storedJson is null)
        {
            return true;
        }

        if (expectedJson is null || storedJson is null)
        {
            return false;
        }

        try
        {
            var expectedNode = JsonNode.Parse(expectedJson);
            var storedNode = JsonNode.Parse(storedJson);
            return JsonNode.DeepEquals(expectedNode, storedNode);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
