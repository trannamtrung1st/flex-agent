using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelPermittedEvidenceAuthorityVerifier
{
    public static EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>> TryResolve(
        EvaluationModelExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.VerifiedPermittedEvidence.Count == 0
            || context.PermittedEvidenceIdBindings.Count == 0)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        if (context.PermittedEvidence.Count != context.VerifiedPermittedEvidence.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "permitted_evidence");
        }

        var authoritative = new List<EvaluationModelPermittedEvidenceV1>(context.VerifiedPermittedEvidence.Count);
        foreach (var verified in context.VerifiedPermittedEvidence.OrderBy(
                     item => item.EvidenceStableId,
                     StringComparer.Ordinal))
        {
            if (!EvaluationIdentity.IsStableId(verified.EvidenceStableId)
                || verified.EvidenceId == Guid.Empty
                || !EvaluationIdentity.IsSha256Hex(verified.ContentDigest)
                || !context.PermittedEvidenceIdBindings.TryGetValue(
                    verified.EvidenceStableId,
                    out var boundEvidenceId)
                || boundEvidenceId != verified.EvidenceId)
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "permitted_evidence");
            }

            var claim = context.PermittedEvidence.SingleOrDefault(item =>
                string.Equals(item.EvidenceId, verified.EvidenceStableId, StringComparison.Ordinal));
            if (claim is null
                || !string.Equals(claim.SourceType, verified.SourceType, StringComparison.Ordinal)
                || !string.Equals(claim.ContentDigest, verified.ContentDigest, StringComparison.Ordinal))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "permitted_evidence");
            }

            authoritative.Add(new EvaluationModelPermittedEvidenceV1(
                verified.EvidenceStableId,
                verified.SourceType,
                verified.ContentDigest));
        }

        foreach (var claim in context.PermittedEvidence)
        {
            if (!context.VerifiedPermittedEvidence.Any(item =>
                    string.Equals(item.EvidenceStableId, claim.EvidenceId, StringComparison.Ordinal)))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "permitted_evidence");
            }
        }

        return EvaluationDecision<IReadOnlyList<EvaluationModelPermittedEvidenceV1>>.Ok(authoritative);
    }
}
