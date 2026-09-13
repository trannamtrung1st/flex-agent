using System.Text;
using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application.Review;

internal static class AssignedReviewEvidenceDisplayExtractor
{
    internal static EvaluationDecision<string> TryExtractDisplayText(
        JsonElement canonicalLocator,
        EvidenceLocatorVerificationContext context)
    {
        if (!TryGetRequiredString(canonicalLocator, "source_type", out var sourceType)
            || !canonicalLocator.TryGetProperty("source_ref", out var sourceRef)
            || !canonicalLocator.TryGetProperty("location", out var location)
            || !TryGetRequiredString(sourceRef, "source_id", out var sourceId)
            || !TryGetRequiredString(sourceRef, "source_version", out var sourceVersion))
        {
            return EvaluationDecision<string>.Fail(ReviewFailureCodes.InvalidField);
        }

        var material = ResolveMaterial(sourceType, sourceRef, sourceId, sourceVersion, context);
        if (material is null)
        {
            return EvaluationDecision<string>.Fail(ReviewFailureCodes.Unavailable);
        }

        if (!TryGetRequiredString(location, "location_type", out var locationType))
        {
            return EvaluationDecision<string>.Fail(ReviewFailureCodes.InvalidField, "location.location_type");
        }

        return locationType switch
        {
            "whole_item" => EvaluationDecision<string>.Ok(Encoding.UTF8.GetString(material.ExactUtf8.Span)),
            "utf8_byte_range"
                when location.TryGetProperty("start_inclusive", out var startProperty)
                     && location.TryGetProperty("end_exclusive", out var endProperty)
                     && startProperty.TryGetInt32(out var startInclusive)
                     && endProperty.TryGetInt32(out var endExclusive)
                     && TryGetRequiredString(location, "excerpt_digest", out var excerptDigest) =>
                ExtractUtf8Range(material.ExactUtf8, startInclusive, endExclusive, excerptDigest),
            "line_range"
                when location.TryGetProperty("start_line_inclusive", out var startLineProperty)
                     && location.TryGetProperty("end_line_inclusive", out var endLineProperty)
                     && startLineProperty.TryGetInt32(out var startLineInclusive)
                     && endLineProperty.TryGetInt32(out var endLineInclusive)
                     && TryGetRequiredString(location, "line_split_procedure_version", out var splitVersion) =>
                ExtractLineRange(material.ExactUtf8, startLineInclusive, endLineInclusive, splitVersion),
            "json_pointer"
                when TryGetRequiredString(location, "json_pointer", out var jsonPointer)
                     && material.ProjectionUtf8 is { } projectionUtf8 =>
                ExtractJsonPointer(projectionUtf8, jsonPointer),
            _ => EvaluationDecision<string>.Fail(ReviewFailureCodes.Unavailable),
        };
    }

    private static EvaluationDecision<string> ExtractUtf8Range(
        ReadOnlyMemory<byte> exactUtf8,
        int startInclusive,
        int endExclusive,
        string excerptDigest)
    {
        var range = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(
            exactUtf8,
            startInclusive,
            endExclusive,
            excerptDigest);
        return range.Succeeded
            ? EvaluationDecision<string>.Ok(Encoding.UTF8.GetString(range.Value.Span))
            : EvaluationDecision<string>.Fail(ReviewFailureCodes.Unavailable);
    }

    private static EvaluationDecision<string> ExtractLineRange(
        ReadOnlyMemory<byte> exactUtf8,
        int startLineInclusive,
        int endLineInclusive,
        string splitVersion)
    {
        var range = EvidenceTextSourceNormalizer.TryExtractLineRange(
            exactUtf8,
            startLineInclusive,
            endLineInclusive,
            splitVersion);
        return range.Succeeded
            ? EvaluationDecision<string>.Ok(Encoding.UTF8.GetString(range.Value.Span))
            : EvaluationDecision<string>.Fail(ReviewFailureCodes.Unavailable);
    }

    private static EvaluationDecision<string> ExtractJsonPointer(ReadOnlyMemory<byte> projectionUtf8, string jsonPointer)
    {
        if (!TryResolveJsonPointer(projectionUtf8, jsonPointer))
        {
            return EvaluationDecision<string>.Fail(ReviewFailureCodes.Unavailable);
        }

        return EvaluationDecision<string>.Ok(Encoding.UTF8.GetString(projectionUtf8.Span));
    }

    private static ResolvedMaterial? ResolveMaterial(
        string sourceType,
        JsonElement sourceRef,
        string sourceId,
        string sourceVersion,
        EvidenceLocatorVerificationContext context)
    {
        return sourceType switch
        {
            "submission.direct_text" or "submission.text_attachment"
                when context.SubmissionItemsBySourceId.TryGetValue(sourceId, out var submission)
                     && string.Equals(submission.SourceVersion, sourceVersion, StringComparison.Ordinal)
                     && string.Equals(submission.Category, sourceType, StringComparison.Ordinal) =>
                new ResolvedMaterial(sourceId, submission.ContentDigest, submission.ExactUtf8, null),
            "session.transcript_item"
                when context.TranscriptItemsByMessageId.TryGetValue(sourceId, out var transcript)
                     && string.Equals(transcript.SourceVersion, sourceVersion, StringComparison.Ordinal)
                     && transcript.PublishedSequence <= context.TerminalCutoffSequence =>
                new ResolvedMaterial(sourceId, transcript.ContentDigest, transcript.ExactUtf8, null),
            "session.work_trace"
                when context.WorkTraceItemsBySourceId.TryGetValue(sourceId, out var workTrace)
                     && string.Equals(workTrace.SourceVersion, sourceVersion, StringComparison.Ordinal)
                     && workTrace.PublishedSequence <= context.TerminalCutoffSequence =>
                new ResolvedMaterial(sourceId, workTrace.ContentDigest, workTrace.ExactUtf8, null),
            "configuration.fact"
                when context.SafeConfigurationFactsBySourceId.TryGetValue(sourceId, out var configuration)
                     && string.Equals(configuration.SourceVersion, sourceVersion, StringComparison.Ordinal) =>
                new ResolvedMaterial(sourceId, configuration.ContentDigest, configuration.ProjectionUtf8, configuration.ProjectionUtf8),
            "manifest.fact"
                when context.SafeManifestFactsBySourceId.TryGetValue(sourceId, out var manifest)
                     && string.Equals(manifest.SourceVersion, sourceVersion, StringComparison.Ordinal) =>
                new ResolvedMaterial(sourceId, manifest.ContentDigest, manifest.ProjectionUtf8, manifest.ProjectionUtf8),
            "deterministic.fact"
                when context.SafeDeterministicFactsBySourceId.TryGetValue(sourceId, out var deterministic)
                     && string.Equals(deterministic.SourceVersion, sourceVersion, StringComparison.Ordinal) =>
                new ResolvedMaterial(sourceId, deterministic.ContentDigest, deterministic.ProjectionUtf8, deterministic.ProjectionUtf8),
            _ => null,
        };
    }

    private static bool TryResolveJsonPointer(ReadOnlyMemory<byte> projectionUtf8, string jsonPointer)
    {
        try
        {
            using var document = JsonDocument.Parse(projectionUtf8);
            var current = document.RootElement;
            if (jsonPointer == "/")
            {
                return true;
            }

            foreach (var segment in jsonPointer.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var decoded = segment.Replace("~1", "/", StringComparison.Ordinal)
                    .Replace("~0", "~", StringComparison.Ordinal);
                if (!current.TryGetProperty(decoded, out current))
                {
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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

    private sealed record ResolvedMaterial(
        string ItemId,
        string ContentDigest,
        ReadOnlyMemory<byte> ExactUtf8,
        ReadOnlyMemory<byte>? ProjectionUtf8);
}
