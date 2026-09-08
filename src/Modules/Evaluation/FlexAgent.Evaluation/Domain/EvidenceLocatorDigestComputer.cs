using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceLocatorDigestComputer
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 16_384,
        maxNestingDepth: 32,
        maxObjectProperties: 128,
        maxArrayElements: 128);

    public static EvaluationDecision<string> TryComputeSourceRefDigest(JsonElement sourceRef)
    {
        if (!TryGetRequiredString(sourceRef, "source_id", out var sourceId)
            || !TryGetRequiredString(sourceRef, "source_version", out var sourceVersion))
        {
            return EvaluationDecision<string>.Fail(EvaluationFailureCodes.InvalidField, "source_ref");
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("source_id", sourceId);
            writer.WriteString("source_version", sourceVersion);
            if (sourceRef.TryGetProperty("terminal_cutoff_sequence", out var cutoff)
                && cutoff.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(cutoff.GetString()))
            {
                writer.WriteString("terminal_cutoff_sequence", cutoff.GetString());
            }

            writer.WriteEndObject();
        }

        return EvaluationDecision<string>.Ok(
            CanonicalJsonProcessor.CanonicalizeSha256Hex(stream.ToArray(), Limits));
    }

    public static EvaluationDecision<string> TryComputeLocationDigest(JsonElement location)
    {
        if (!TryGetRequiredString(location, "location_type", out _))
        {
            return EvaluationDecision<string>.Fail(EvaluationFailureCodes.InvalidField, "location");
        }

        return EvaluationDecision<string>.Ok(
            CanonicalJsonProcessor.CanonicalizeSha256Hex(
                JsonSerializer.SerializeToUtf8Bytes(location),
                Limits));
    }

    public static EvaluationDecision<string> TryComputeLocatorDigest(JsonElement locator)
    {
        if (locator.ValueKind != JsonValueKind.Object)
        {
            return EvaluationDecision<string>.Fail(EvaluationFailureCodes.InvalidField, "locator");
        }

        return EvaluationDecision<string>.Ok(
            CanonicalJsonProcessor.CanonicalizeSha256Hex(
                JsonSerializer.SerializeToUtf8Bytes(locator),
                Limits));
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
