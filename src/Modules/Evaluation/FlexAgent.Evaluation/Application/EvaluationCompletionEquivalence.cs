using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record StoredCompletionSnapshot(
    Guid EvaluationId,
    Guid EvidenceSetId,
    string ProcedureDigest,
    string AggregateStatus,
    string EvidenceSetDigest);

public static class EvaluationCompletionEquivalence
{
    public static bool IsEquivalent(
        CompletedEvaluation completed,
        StoredCompletionSnapshot stored) =>
        completed.EvaluationId == stored.EvaluationId
        && completed.EvidenceSetId == stored.EvidenceSetId
        && string.Equals(completed.ProcedureRef.ContentDigest, stored.ProcedureDigest, StringComparison.Ordinal)
        && string.Equals(completed.AggregateStatus, stored.AggregateStatus, StringComparison.Ordinal)
        && string.Equals(completed.EvidenceSetDigest, stored.EvidenceSetDigest, StringComparison.Ordinal);
}
