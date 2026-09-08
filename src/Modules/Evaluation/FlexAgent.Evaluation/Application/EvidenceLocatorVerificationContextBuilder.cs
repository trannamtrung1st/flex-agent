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

        var resolvedConfigurationFacts = configurationFacts
            ?? BuildFactDictionary(sessionEvidence.ConfigurationFact);
        var resolvedManifestFacts = manifestFacts
            ?? BuildFactDictionary(sessionEvidence.ManifestFact);

        return new EvidenceLocatorVerificationContext(
            trustedOwnership,
            sessionEvidence.Handoff.CutoffSequence,
            sessionEvidence.TranscriptItemsAtOrBeforeCutoff.ToDictionary(
                item => item.MessageId,
                StringComparer.Ordinal),
            (submissionEvidence?.BoundItems ?? [])
                .ToDictionary(item => item.SourceId, StringComparer.Ordinal),
            resolvedConfigurationFacts,
            resolvedManifestFacts,
            permitWholeItemFallback);
    }

    private static IReadOnlyDictionary<string, EvaluationSafeFactProjection> BuildFactDictionary(
        EvaluationSafeFactProjection? fact)
    {
        if (fact is null)
        {
            return new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal);
        }

        return new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            [fact.SourceId] = fact,
        };
    }
}
