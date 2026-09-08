using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvidenceLocatorVerificationContextBuilder
{
    public static EvidenceLocatorVerificationContext Build(
        EvaluationStableOwnershipReference trustedOwnership,
        EvaluationSessionEvidenceBundle? sessionEvidence,
        EvaluationSubmissionEvidenceBundle? submissionEvidence,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? configurationFacts = null,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? manifestFacts = null,
        bool permitWholeItemFallback = false)
    {
        if (sessionEvidence is null)
        {
            throw new ArgumentNullException(nameof(sessionEvidence));
        }

        return new EvidenceLocatorVerificationContext(
            trustedOwnership,
            sessionEvidence.Handoff.CutoffSequence,
            sessionEvidence.TranscriptItemsAtOrBeforeCutoff.ToDictionary(
                item => item.MessageId,
                StringComparer.Ordinal),
            (submissionEvidence?.BoundItems ?? [])
                .ToDictionary(item => item.SourceId, StringComparer.Ordinal),
            configurationFacts ?? new Dictionary<string, EvaluationSafeFactProjection>(),
            manifestFacts ?? new Dictionary<string, EvaluationSafeFactProjection>(),
            permitWholeItemFallback);
    }
}
