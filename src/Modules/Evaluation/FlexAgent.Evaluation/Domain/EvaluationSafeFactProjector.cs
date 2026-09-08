using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationSafeFactProjector
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    private static readonly HashSet<string> ConfigurationAllowlistedRootProperties =
    [
        "sources",
        "permitted_submissions",
        "model_profile_id",
        "model_profile_version",
        "model_profile_digest",
    ];

    private static readonly HashSet<string> ManifestAllowlistedRootProperties =
    [
        "manifest_id",
        "configuration_id",
        "configuration_digest",
        "provenance",
    ];

    private static readonly HashSet<string> SourceReferenceProperties =
    [
        "source_key",
        "source_id",
        "source_version_id",
        "content_digest",
    ];

    private static readonly HashSet<string> PermittedSubmissionProperties =
    [
        "protected_ref",
        "content_digest",
    ];

    public static EvaluationDecision<EvaluationSafeFactProjection> TryBuildConfigurationFact(
        Guid configurationId,
        string configurationDigest,
        ReadOnlyMemory<byte> canonicalUtf8)
    {
        if (configurationId == Guid.Empty
            || !EvaluationIdentity.IsSha256Hex(configurationDigest))
        {
            return EvaluationDecision<EvaluationSafeFactProjection>.Fail(EvaluationFailureCodes.InvalidField);
        }

        return TryBuildFact(
            EvaluationEvidenceSourceIdentity.ConfigurationFactSourceId(configurationId),
            EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(configurationDigest),
            configurationDigest,
            canonicalUtf8,
            ConfigurationAllowlistedRootProperties,
            WriteConfigurationProjection);
    }

    public static EvaluationDecision<EvaluationSafeFactProjection> TryBuildManifestFact(
        Guid manifestId,
        string manifestDigest,
        ReadOnlyMemory<byte> canonicalUtf8)
    {
        if (manifestId == Guid.Empty
            || !EvaluationIdentity.IsSha256Hex(manifestDigest))
        {
            return EvaluationDecision<EvaluationSafeFactProjection>.Fail(EvaluationFailureCodes.InvalidField);
        }

        return TryBuildFact(
            EvaluationEvidenceSourceIdentity.ManifestFactSourceId(manifestId),
            EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(manifestDigest),
            manifestDigest,
            canonicalUtf8,
            ManifestAllowlistedRootProperties,
            WriteManifestProjection);
    }

    private static EvaluationDecision<EvaluationSafeFactProjection> TryBuildFact(
        string sourceId,
        string sourceVersion,
        string boundDigest,
        ReadOnlyMemory<byte> canonicalUtf8,
        IReadOnlySet<string> allowlistedRootProperties,
        Action<Utf8JsonWriter, JsonElement, IReadOnlySet<string>> writeProjection)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(canonicalUtf8);
        }
        catch (JsonException)
        {
            return EvaluationDecision<EvaluationSafeFactProjection>.Fail(EvaluationFailureCodes.InvalidField);
        }

        using (document)
        {
            var canonicalDigest = CanonicalJsonProcessor.CanonicalizeSha256Hex(canonicalUtf8.Span, Limits);
            if (!string.Equals(canonicalDigest, boundDigest, StringComparison.Ordinal))
            {
                return EvaluationDecision<EvaluationSafeFactProjection>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "canonical_digest");
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writeProjection(writer, document.RootElement, allowlistedRootProperties);
            }

            var projectionUtf8 = stream.ToArray();
            if (projectionUtf8.Length == 0)
            {
                return EvaluationDecision<EvaluationSafeFactProjection>.Fail(EvaluationFailureCodes.InvalidField);
            }

            var projectionDigest = CanonicalJsonProcessor.CanonicalizeSha256Hex(projectionUtf8, Limits);

            return EvaluationDecision<EvaluationSafeFactProjection>.Ok(
                new EvaluationSafeFactProjection(
                    sourceId,
                    sourceVersion,
                    projectionDigest,
                    projectionUtf8));
        }
    }

    private static void WriteConfigurationProjection(
        Utf8JsonWriter writer,
        JsonElement canonical,
        IReadOnlySet<string> allowlistedRootProperties)
    {
        writer.WriteStartObject();
        foreach (var property in canonical.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            if (!allowlistedRootProperties.Contains(property.Name))
            {
                continue;
            }

            if (property.NameEquals("sources"))
            {
                WriteSourceReferenceArray(writer, property.Name, property.Value);
                continue;
            }

            if (property.NameEquals("permitted_submissions"))
            {
                WriteObjectArray(
                    writer,
                    property.Name,
                    property.Value,
                    PermittedSubmissionProperties);
                continue;
            }

            writer.WritePropertyName(property.Name);
            property.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteManifestProjection(
        Utf8JsonWriter writer,
        JsonElement canonical,
        IReadOnlySet<string> allowlistedRootProperties)
    {
        writer.WriteStartObject();
        foreach (var property in canonical.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            if (!allowlistedRootProperties.Contains(property.Name))
            {
                continue;
            }

            if (property.NameEquals("provenance"))
            {
                WriteSourceReferenceArray(writer, property.Name, property.Value);
                continue;
            }

            writer.WritePropertyName(property.Name);
            property.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteSourceReferenceArray(
        Utf8JsonWriter writer,
        string propertyName,
        JsonElement value) =>
        WriteObjectArray(writer, propertyName, value, SourceReferenceProperties);

    private static void WriteObjectArray(
        Utf8JsonWriter writer,
        string propertyName,
        JsonElement value,
        IReadOnlySet<string> allowlistedProperties)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            writer.WriteStartObject();
            foreach (var property in item.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (!allowlistedProperties.Contains(property.Name))
                {
                    continue;
                }

                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
