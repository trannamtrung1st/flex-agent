using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationCompletionEvidenceSeal
{
    public static EvaluationDecision<string> TryComputeExpectedDigest(
        Guid evidenceSetId,
        Guid invocationAttemptId,
        FrozenInputIdentity frozenInput,
        IReadOnlyList<EvidenceItem> evidenceItems)
    {
        ArgumentNullException.ThrowIfNull(frozenInput);
        ArgumentNullException.ThrowIfNull(evidenceItems);

        var ownership = new EvidenceSetOwnershipReference(
            frozenInput.Ownership.OrganizationId.ToString("D"),
            frozenInput.Ownership.ActivityId.ToString("D"),
            frozenInput.Ownership.ParticipantId.ToString("D"),
            frozenInput.Ownership.AttemptId.ToString("D"),
            frozenInput.Ownership.SessionId.ToString("D"));

        var sealedItems = evidenceItems
            .Select(item => new SealedEvidenceItemReference(
                EvaluationEvidenceSourceIdentity.StableEvidenceId(item.EvidenceId),
                item.SourceType,
                item.Source.ContentDigest,
                item.Source.ContentDigest,
                "verified"))
            .ToArray();

        return EvidenceSetSealComputer.TryComputeDigest(
            new EvidenceSetSealRequest(
                $"evidence-set.{evidenceSetId:N}",
                $"invocation.{invocationAttemptId:N}",
                ownership,
                frozenInput.TerminalSealDigest,
                FrozenInputDigest.Compute(frozenInput),
                sealedItems));
    }
}
