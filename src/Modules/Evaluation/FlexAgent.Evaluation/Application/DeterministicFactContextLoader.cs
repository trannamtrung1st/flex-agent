using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class DeterministicFactContextLoader
{
    public static async Task<EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>> TryLoadForCompletionAsync(
        Guid organizationId,
        Guid requestId,
        IReadOnlyList<EvidenceLocatorVerificationEntry> entries,
        IProtectedDeterministicOutputStore outputStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(outputStore);

        if (organizationId == Guid.Empty || requestId == Guid.Empty)
        {
            return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                EvaluationFailureCodes.InvalidField);
        }

        var required = new Dictionary<string, (Guid AttemptId, string Digest)>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!TryGetDeterministicFactRequirement(entry.Locator, out var sourceId, out var attemptId, out var digest))
            {
                continue;
            }

            if (required.TryGetValue(sourceId, out var existing))
            {
                if (existing.AttemptId != attemptId
                    || !string.Equals(existing.Digest, digest, StringComparison.Ordinal))
                {
                    return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                        EvaluationFailureCodes.DeterministicConflict);
                }

                continue;
            }

            required[sourceId] = (attemptId, digest);
        }

        if (required.Count == 0)
        {
            return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Ok(
                new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal));
        }

        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal);
        foreach (var (sourceId, requirement) in required)
        {
            var projection = await outputStore.TryLoadProjectionAsync(
                organizationId,
                requestId,
                requirement.AttemptId,
                requirement.Digest,
                cancellationToken);
            if (projection is null)
            {
                return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                    EvaluationFailureCodes.ProtectedContent);
            }

            if (!string.Equals(projection.SourceId, sourceId, StringComparison.Ordinal))
            {
                return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                    EvaluationFailureCodes.DeterministicConflict);
            }

            facts[sourceId] = projection;
        }

        return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Ok(facts);
    }

    internal static bool TryGetDeterministicFactRequirement(
        JsonElement locator,
        out string sourceId,
        out Guid attemptId,
        out string digest)
    {
        sourceId = string.Empty;
        attemptId = Guid.Empty;
        digest = string.Empty;

        if (!locator.TryGetProperty("source_type", out var sourceTypeProperty)
            || sourceTypeProperty.ValueKind != JsonValueKind.String
            || !string.Equals(sourceTypeProperty.GetString(), "deterministic.fact", StringComparison.Ordinal))
        {
            return false;
        }

        if (!locator.TryGetProperty("source_ref", out var sourceRef)
            || !TryGetRequiredString(sourceRef, "source_id", out sourceId)
            || !EvaluationEvidenceSourceIdentity.TryParseDeterministicFactSourceId(sourceId, out attemptId))
        {
            return false;
        }

        if (!locator.TryGetProperty("integrity", out var integrity)
            || !TryGetRequiredString(integrity, "source_digest", out digest)
            || !EvaluationIdentity.IsSha256Hex(digest))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetRequiredString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
