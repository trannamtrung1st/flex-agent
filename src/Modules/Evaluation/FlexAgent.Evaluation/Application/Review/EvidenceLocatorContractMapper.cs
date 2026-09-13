using System.Text.Json;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application.Review;

internal static class EvidenceLocatorContractMapper
{
    internal static EvaluationDecision<EvidenceLocatorV1> TryMapV1(JsonElement canonicalLocator)
    {
        if (!TryGetRequiredString(canonicalLocator, "locator_schema", out var locatorSchema)
            || !TryGetRequiredString(canonicalLocator, "source_type", out var sourceType)
            || !canonicalLocator.TryGetProperty("source_ref", out var sourceRef)
            || !canonicalLocator.TryGetProperty("ownership_ref", out var ownershipRef)
            || !canonicalLocator.TryGetProperty("location", out var location)
            || !canonicalLocator.TryGetProperty("integrity", out var integrity)
            || !canonicalLocator.TryGetProperty("created_by", out var createdBy)
            || !TryGetRequiredString(sourceRef, "source_id", out var sourceId)
            || !TryGetRequiredString(sourceRef, "source_version", out var sourceVersion)
            || !TryGetRequiredString(ownershipRef, "organization_id", out var organizationId)
            || !TryGetRequiredString(ownershipRef, "activity_id", out var activityId)
            || !TryGetRequiredString(ownershipRef, "participant_id", out var participantId)
            || !TryGetRequiredString(ownershipRef, "attempt_id", out var attemptId)
            || !TryGetRequiredString(ownershipRef, "session_id", out var sessionId)
            || !TryGetRequiredString(ownershipRef, "evaluation_id", out var evaluationId)
            || !TryGetRequiredString(canonicalLocator, "precision", out var precision)
            || !TryGetRequiredString(integrity, "source_digest", out var sourceDigest)
            || !TryGetRequiredString(integrity, "adapter_version", out var adapterVersion)
            || !TryGetRequiredString(integrity, "verification_state", out var verificationState)
            || !TryGetRequiredString(createdBy, "service_id", out var serviceId)
            || !TryGetRequiredString(createdBy, "invocation_id", out var invocationId))
        {
            return EvaluationDecision<EvidenceLocatorV1>.Fail(ReviewFailureCodes.InvalidField);
        }

        var terminalCutoff = sourceRef.TryGetProperty("terminal_cutoff_sequence", out var cutoffProperty)
                             && cutoffProperty.ValueKind == JsonValueKind.String
            ? cutoffProperty.GetString()
            : null;

        var mappedLocation = TryMapLocation(location);
        if (!mappedLocation.Succeeded || mappedLocation.Value is null)
        {
            return EvaluationDecision<EvidenceLocatorV1>.Fail(
                mappedLocation.OutcomeCode,
                mappedLocation.Field);
        }

        return EvaluationDecision<EvidenceLocatorV1>.Ok(
            new EvidenceLocatorV1(
                locatorSchema,
                sourceType,
                new EvidenceSourceRefV1(sourceId, sourceVersion, terminalCutoff),
                new EvidenceOwnershipRefV1(
                    organizationId,
                    activityId,
                    participantId,
                    attemptId,
                    sessionId,
                    evaluationId),
                mappedLocation.Value,
                precision,
                new EvidenceIntegrityV1(sourceDigest, adapterVersion, verificationState),
                new EvidenceCreatedByV1(serviceId, invocationId)));
    }

    private static EvaluationDecision<object> TryMapLocation(JsonElement location)
    {
        if (!TryGetRequiredString(location, "location_type", out var locationType))
        {
            return EvaluationDecision<object>.Fail(ReviewFailureCodes.InvalidField, "location.location_type");
        }

        return locationType switch
        {
            "whole_item"
                when TryGetRequiredString(location, "item_id", out var wholeItemId) =>
                EvaluationDecision<object>.Ok(new WholeItemLocationV1(locationType, wholeItemId)),
            "utf8_byte_range"
                when TryGetRequiredString(location, "item_id", out var rangeItemId)
                     && location.TryGetProperty("start_inclusive", out var startProperty)
                     && location.TryGetProperty("end_exclusive", out var endProperty)
                     && startProperty.TryGetInt32(out var startInclusive)
                     && endProperty.TryGetInt32(out var endExclusive)
                     && TryGetRequiredString(location, "excerpt_digest", out var excerptDigest) =>
                EvaluationDecision<object>.Ok(
                    new Utf8ByteRangeLocationV1(
                        locationType,
                        rangeItemId,
                        startInclusive,
                        endExclusive,
                        excerptDigest)),
            "line_range"
                when TryGetRequiredString(location, "item_id", out var lineItemId)
                     && location.TryGetProperty("start_line_inclusive", out var startLineProperty)
                     && location.TryGetProperty("end_line_inclusive", out var endLineProperty)
                     && startLineProperty.TryGetInt32(out var startLineInclusive)
                     && endLineProperty.TryGetInt32(out var endLineInclusive)
                     && TryGetRequiredString(location, "line_split_procedure_version", out var splitVersion) =>
                EvaluationDecision<object>.Ok(
                    new LineRangeLocationV1(
                        locationType,
                        lineItemId,
                        startLineInclusive,
                        endLineInclusive,
                        splitVersion)),
            "json_pointer"
                when TryGetRequiredString(location, "json_pointer", out var jsonPointer) =>
                EvaluationDecision<object>.Ok(new JsonPointerLocationV1(locationType, jsonPointer)),
            _ => EvaluationDecision<object>.Fail(ReviewFailureCodes.InvalidField, "location.location_type"),
        };
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
