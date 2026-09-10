using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class DeterministicFactContextLoader
{
    public static async Task<EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>> TryLoadForCompletionAsync(
        Guid organizationId,
        Guid requestId,
        EvaluationProcedureV1 procedure,
        IReadOnlyList<EvidenceLocatorVerificationEntry> entries,
        IProtectedDeterministicOutputStore outputStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(outputStore);
        ArgumentNullException.ThrowIfNull(procedure);

        if (organizationId == Guid.Empty || requestId == Guid.Empty)
        {
            return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                EvaluationFailureCodes.InvalidField);
        }

        var required = new Dictionary<string, Requirement>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!TryGetDeterministicFactRequirement(entry.Locator, out var sourceId, out var attemptId, out var digest))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.CriterionId))
            {
                return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                    EvaluationFailureCodes.InvalidField);
            }

            var criterion = procedure.Criteria.SingleOrDefault(item =>
                string.Equals(item.CriterionId, entry.CriterionId, StringComparison.Ordinal));
            if (criterion is null)
            {
                return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                    EvaluationFailureCodes.InvalidField,
                    "criterion_id");
            }

            if (required.TryGetValue(sourceId, out var existing))
            {
                if (existing.AttemptId != attemptId
                    || !string.Equals(existing.Digest, digest, StringComparison.Ordinal)
                    || !string.Equals(existing.CriterionId, entry.CriterionId, StringComparison.Ordinal)
                    || !string.Equals(existing.CriterionVersion, criterion.CriterionVersion, StringComparison.Ordinal))
                {
                    return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                        EvaluationFailureCodes.DeterministicConflict);
                }

                continue;
            }

            required[sourceId] = new Requirement(
                attemptId,
                digest,
                entry.CriterionId,
                criterion.CriterionVersion);
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
                requirement.CriterionId,
                requirement.CriterionVersion,
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

    private sealed record Requirement(
        Guid AttemptId,
        string Digest,
        string CriterionId,
        string CriterionVersion);
}
