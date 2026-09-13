using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationCompletionEvidenceSeal
{
    public static EvaluationDecision<string> TryComputeExpectedDigest(
        Guid evidenceSetId,
        Guid invocationAttemptId,
        FrozenInputIdentity frozenInput,
        IReadOnlyList<EvidenceItem> evidenceItems) =>
        TryComputeFromPersistedRecords(
            evidenceSetId,
            invocationAttemptId,
            frozenInput,
            evidenceItems.Select(item => new EvaluationEvidenceLocatorRecord(
                item.EvidenceId,
                item.SourceType,
                item.Source.SourceId,
                item.Source.SourceVersionId,
                item.Source.ContentDigest,
                "evidence-locator.v1",
                item.Source.ContentDigest,
                item.Precision,
                "verified")).ToArray());

    public static EvaluationDecision<string> TryComputeFromPersistedRecords(
        Guid evidenceSetId,
        Guid invocationAttemptId,
        FrozenInputIdentity frozenInput,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> records)
    {
        ArgumentNullException.ThrowIfNull(frozenInput);
        ArgumentNullException.ThrowIfNull(records);

        var ownership = new EvidenceSetOwnershipReference(
            frozenInput.Ownership.OrganizationId.ToString("D"),
            frozenInput.Ownership.ActivityId.ToString("D"),
            frozenInput.Ownership.ParticipantId.ToString("D"),
            frozenInput.Ownership.AttemptId.ToString("D"),
            frozenInput.Ownership.SessionId.ToString("D"));

        var sealedItems = records
            .Select(record => new SealedEvidenceItemReference(
                EvaluationEvidenceSourceIdentity.StableEvidenceId(record.EvidenceId),
                record.SourceType,
                record.SourceContentDigest,
                record.LocatorDigest,
                record.IntegrityState))
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
