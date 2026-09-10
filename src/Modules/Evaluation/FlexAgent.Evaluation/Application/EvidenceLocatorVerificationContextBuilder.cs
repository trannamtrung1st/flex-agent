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
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? deterministicFacts = null,
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
        var resolvedDeterministicFacts = deterministicFacts
            ?? new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal);

        return new EvidenceLocatorVerificationContext(
            trustedOwnership,
            sessionEvidence.Handoff.CutoffSequence,
            sessionEvidence.TranscriptItemsAtOrBeforeCutoff.ToDictionary(
                item => item.MessageId,
                StringComparer.Ordinal),
            new Dictionary<string, EvaluationSessionTranscriptMaterial>(StringComparer.Ordinal),
            (submissionEvidence?.BoundItems ?? [])
                .ToDictionary(item => item.SourceId, StringComparer.Ordinal),
            resolvedConfigurationFacts,
            resolvedManifestFacts,
            resolvedDeterministicFacts,
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
