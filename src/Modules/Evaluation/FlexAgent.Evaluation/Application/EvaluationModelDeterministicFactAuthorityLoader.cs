using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelDeterministicFactAuthorityLoader
{
    public static async Task<EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>> TryLoadAsync(
        EvaluationOwnership ownership,
        Guid requestId,
        Guid expectedDeterministicInvocationId,
        string criterionId,
        string criterionVersion,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection> verifiedDeterministicFacts,
        IProtectedDeterministicOutputStore outputStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(verifiedDeterministicFacts);
        ArgumentNullException.ThrowIfNull(outputStore);

        if (requestId == Guid.Empty
            || expectedDeterministicInvocationId == Guid.Empty
            || string.IsNullOrWhiteSpace(criterionId)
            || string.IsNullOrWhiteSpace(criterionVersion)
            || verifiedDeterministicFacts.Count == 0)
        {
            return EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "deterministic_facts");
        }

        var materials = new Dictionary<string, VerifiedDeterministicOutputMaterial>(
            verifiedDeterministicFacts.Count,
            StringComparer.Ordinal);

        foreach (var (sourceId, verifiedFact) in verifiedDeterministicFacts.OrderBy(
                     pair => pair.Key,
                     StringComparer.Ordinal))
        {
            if (!EvaluationEvidenceSourceIdentity.TryParseDeterministicFactSourceId(
                    sourceId,
                    out var deterministicAttemptId))
            {
                return EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            if (deterministicAttemptId != expectedDeterministicInvocationId)
            {
                return EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>.Fail(
                    EvaluationFailureCodes.DeterministicConflict,
                    "deterministic_facts");
            }

            var material = await outputStore.TryLoadVerifiedMaterialAsync(
                ownership,
                requestId,
                deterministicAttemptId,
                verifiedFact.ContentDigest,
                criterionId,
                criterionVersion,
                cancellationToken);
            if (material is null)
            {
                return EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>.Fail(
                    EvaluationFailureCodes.ProtectedContent,
                    "deterministic_facts");
            }

            if (!FactsMatch(verifiedFact, material))
            {
                return EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>.Fail(
                    EvaluationFailureCodes.DeterministicConflict,
                    "deterministic_facts");
            }

            materials[sourceId] = material;
        }

        return EvaluationDecision<IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>>.Ok(materials);
    }

    private static bool FactsMatch(
        EvaluationSafeFactProjection verifiedFact,
        VerifiedDeterministicOutputMaterial material) =>
        string.Equals(verifiedFact.SourceId, material.Projection.SourceId, StringComparison.Ordinal)
        && string.Equals(verifiedFact.SourceVersion, material.Projection.SourceVersion, StringComparison.Ordinal)
        && string.Equals(verifiedFact.ContentDigest, material.Projection.ContentDigest, StringComparison.Ordinal)
        && string.Equals(
            verifiedFact.ContentDigest,
            material.ProtectedRef.ContentDigest,
            StringComparison.Ordinal)
        && verifiedFact.ProjectionUtf8.Span.SequenceEqual(material.Projection.ProjectionUtf8.Span);
}
