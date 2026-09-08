using System.Text.Json;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceLocatorStructuralValidator
{
    private static readonly HashSet<string> AllowedSourceTypes =
    [
        "submission.direct_text",
        "submission.text_attachment",
        "session.transcript_item",
        "session.work_trace",
        "configuration.fact",
        "manifest.fact",
        "deterministic.fact",
    ];

    private static readonly HashSet<string> AllowedPrecisions =
    [
        "exact_range",
        "stable_segment",
        "whole_item",
    ];

    private static readonly HashSet<string> AllowedVerificationStates =
    [
        "verified",
        "lower_precision",
        "unavailable",
        "integrity_changed",
    ];

    public static EvaluationDecision<JsonElement> ValidateJson(ReadOnlySpan<byte> utf8Json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json.ToArray());
        }
        catch (JsonException)
        {
            return EvaluationDecision<JsonElement>.Fail(EvaluationFailureCodes.InvalidField);
        }

        using (document)
        {
            return Validate(document.RootElement);
        }
    }

    public static EvaluationDecision<JsonElement> Validate(JsonElement locator)
    {
        if (!TryGetRequiredString(locator, "locator_schema", out var locatorSchema)
            || !string.Equals(locatorSchema, "evidence-locator.v1", StringComparison.Ordinal)
            || !TryGetRequiredString(locator, "source_type", out var sourceType)
            || !AllowedSourceTypes.Contains(sourceType)
            || !TryGetRequiredString(locator, "precision", out var precision)
            || !AllowedPrecisions.Contains(precision)
            || !locator.TryGetProperty("source_ref", out var sourceRef)
            || !TryGetRequiredString(sourceRef, "source_id", out var sourceId)
            || !TryGetRequiredString(sourceRef, "source_version", out var sourceVersion)
            || !locator.TryGetProperty("ownership_ref", out var ownershipRef)
            || !IsOwnershipValid(ownershipRef)
            || !locator.TryGetProperty("integrity", out var integrity)
            || !TryGetRequiredString(integrity, "adapter_version", out var adapterVersion)
            || !TryGetRequiredString(integrity, "source_digest", out var sourceDigest)
            || !TryGetRequiredString(integrity, "verification_state", out var verificationState)
            || !locator.TryGetProperty("created_by", out var createdBy)
            || !TryGetRequiredString(createdBy, "service_id", out var serviceId)
            || !TryGetRequiredString(createdBy, "invocation_id", out var invocationId)
            || !EvaluationIdentity.IsStableId(adapterVersion)
            || !EvaluationIdentity.IsSha256Hex(sourceDigest)
            || !AllowedVerificationStates.Contains(verificationState)
            || !EvaluationIdentity.IsStableId(serviceId)
            || !EvaluationIdentity.IsStableId(invocationId)
            || !EvaluationIdentity.IsStableId(sourceId)
            || !EvaluationIdentity.IsStableId(sourceVersion)
            || EvaluationIdentity.ContainsMutableAlias(sourceId)
            || EvaluationIdentity.ContainsMutableAlias(sourceVersion)
            || !locator.TryGetProperty("location", out var location)
            || !IsLocationValid(location, sourceType))
        {
            return EvaluationDecision<JsonElement>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (RequiresTerminalCutoff(sourceType)
            && (!sourceRef.TryGetProperty("terminal_cutoff_sequence", out var cutoff)
                || cutoff.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(cutoff.GetString())))
        {
            return EvaluationDecision<JsonElement>.Fail(
                EvaluationFailureCodes.InvalidField,
                "source_ref.terminal_cutoff_sequence");
        }

        return EvaluationDecision<JsonElement>.Ok(locator);
    }

    private static bool RequiresTerminalCutoff(string sourceType) =>
        sourceType is "session.transcript_item" or "session.work_trace";

    private static bool IsOwnershipValid(JsonElement ownership) =>
        TryGetRequiredString(ownership, "organization_id", out var organizationId)
        && TryGetRequiredString(ownership, "activity_id", out var activityId)
        && TryGetRequiredString(ownership, "participant_id", out var participantId)
        && TryGetRequiredString(ownership, "attempt_id", out var attemptId)
        && TryGetRequiredString(ownership, "session_id", out var sessionId)
        && TryGetRequiredString(ownership, "evaluation_id", out var evaluationId)
        && EvaluationIdentity.IsStableId(organizationId)
        && EvaluationIdentity.IsStableId(activityId)
        && EvaluationIdentity.IsStableId(participantId)
        && EvaluationIdentity.IsStableId(attemptId)
        && EvaluationIdentity.IsStableId(sessionId)
        && EvaluationIdentity.IsStableId(evaluationId);

    private static bool IsLocationValid(JsonElement location, string sourceType)
    {
        if (!TryGetRequiredString(location, "location_type", out var locationType))
        {
            return false;
        }

        return locationType switch
        {
            "whole_item" => ValidateWholeItem(location, sourceType),
            "line_range" => ValidateLineRange(location),
            "utf8_byte_range" => ValidateByteRange(location),
            "json_pointer" => ValidateJsonPointer(location, sourceType),
            _ => false,
        };
    }

    private static bool ValidateWholeItem(JsonElement location, string sourceType) =>
        sourceType is
            "submission.direct_text"
            or "submission.text_attachment"
            or "session.transcript_item"
            or "session.work_trace"
        && TryGetRequiredString(location, "item_id", out var itemId)
        && EvaluationIdentity.IsStableId(itemId)
        && !EvaluationIdentity.ContainsMutableAlias(itemId);

    private static bool ValidateLineRange(JsonElement location) =>
        TryGetRequiredString(location, "item_id", out var itemId)
        && TryGetRequiredString(location, "line_split_procedure_version", out var splitVersion)
        && location.TryGetProperty("start_line_inclusive", out var start)
        && location.TryGetProperty("end_line_inclusive", out var end)
        && start.TryGetInt32(out var startLine)
        && end.TryGetInt32(out var endLine)
        && EvaluationIdentity.IsStableId(itemId)
        && EvaluationIdentity.IsStableId(splitVersion)
        && startLine >= 1
        && endLine >= startLine;

    private static bool ValidateByteRange(JsonElement location) =>
        TryGetRequiredString(location, "item_id", out var itemId)
        && TryGetRequiredString(location, "excerpt_digest", out var excerptDigest)
        && location.TryGetProperty("start_inclusive", out var start)
        && location.TryGetProperty("end_exclusive", out var end)
        && start.TryGetInt32(out var startInclusive)
        && end.TryGetInt32(out var endExclusive)
        && EvaluationIdentity.IsStableId(itemId)
        && EvaluationIdentity.IsSha256Hex(excerptDigest)
        && startInclusive >= 0
        && endExclusive > startInclusive;

    private static bool ValidateJsonPointer(JsonElement location, string sourceType) =>
        sourceType is "configuration.fact" or "manifest.fact" or "deterministic.fact"
        && TryGetRequiredString(location, "json_pointer", out var jsonPointer)
        && jsonPointer.StartsWith("/", StringComparison.Ordinal)
        && !jsonPointer.Contains("..", StringComparison.Ordinal);

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
