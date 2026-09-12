using System.Text.Json;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceLocatorVerifier
{
    public static EvaluationDecision<VerifiedEvidenceLocator> TryVerify(
        JsonElement locator,
        EvidenceLocatorVerificationContext context)
    {
        var structural = EvidenceLocatorStructuralValidator.Validate(locator);
        if (!structural.Succeeded || structural.Value.ValueKind == JsonValueKind.Undefined)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                structural.OutcomeCode,
                structural.Field);
        }

        if (!TryGetRequiredString(locator, "source_type", out var sourceType)
            || !TryGetRequiredString(locator, "precision", out var precision)
            || !locator.TryGetProperty("source_ref", out var sourceRef)
            || !locator.TryGetProperty("ownership_ref", out var ownershipRef)
            || !locator.TryGetProperty("integrity", out var integrity)
            || !locator.TryGetProperty("location", out var location)
            || !TryGetRequiredString(integrity, "adapter_version", out var adapterVersion)
            || !TryGetRequiredString(integrity, "source_digest", out var declaredSourceDigest)
            || !TryGetRequiredString(integrity, "verification_state", out var declaredVerificationState))
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (!string.Equals(
                adapterVersion,
                EvaluationEvidenceSourceIdentity.LocatorAdapterVersion,
                StringComparison.Ordinal))
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                EvaluationFailureCodes.InvalidField,
                "integrity.adapter_version");
        }

        if (!OwnershipMatches(ownershipRef, context.TrustedOwnership))
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                EvaluationFailureCodes.IncompleteOwnership,
                "ownership_ref");
        }

        if (RequiresTerminalCutoff(sourceType)
            && (!TryGetRequiredString(sourceRef, "terminal_cutoff_sequence", out var cutoffText)
                || !long.TryParse(cutoffText, out var cutoff)
                || cutoff != context.TerminalCutoffSequence))
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "source_ref.terminal_cutoff_sequence");
        }

        var material = ResolveMaterial(sourceType, sourceRef, context);
        if (material is null)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                EvaluationFailureCodes.ProtectedContent,
                "source_ref");
        }

        if (!string.Equals(material.ContentDigest, declaredSourceDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "integrity.source_digest");
        }

        var locationResult = VerifyLocation(
            sourceType,
            location,
            precision,
            material,
            context.PermitWholeItemFallback);
        if (!locationResult.Succeeded)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                locationResult.OutcomeCode,
                locationResult.Field);
        }

        var sourceRefDigest = EvidenceLocatorDigestComputer.TryComputeSourceRefDigest(sourceRef);
        if (!sourceRefDigest.Succeeded)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                sourceRefDigest.OutcomeCode,
                sourceRefDigest.Field);
        }

        var effectiveLocation = locationResult.EffectiveLocation ?? location;
        var verifiedPrecision = locationResult.EffectiveLocation is not null ? "whole_item" : precision;
        var locationDigest = EvidenceLocatorDigestComputer.TryComputeLocationDigest(effectiveLocation);
        if (!locationDigest.Succeeded)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                locationDigest.OutcomeCode,
                locationDigest.Field);
        }

        var verificationState = locationResult.VerificationState;
        if (declaredVerificationState != verificationState
            && !(declaredVerificationState == "verified" && verificationState == "lower_precision"))
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "integrity.verification_state");
        }

        var effectiveLocator = EvidenceLocatorVerifiedProjection.TryBuildEffectiveLocator(
            locator,
            effectiveLocation,
            verifiedPrecision,
            verificationState);
        if (!effectiveLocator.Succeeded)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                effectiveLocator.OutcomeCode,
                effectiveLocator.Field);
        }

        var verifiedLocatorDigest = EvidenceLocatorDigestComputer.TryComputeLocatorDigest(
            effectiveLocator.Value);
        if (!verifiedLocatorDigest.Succeeded)
        {
            return EvaluationDecision<VerifiedEvidenceLocator>.Fail(
                verifiedLocatorDigest.OutcomeCode,
                verifiedLocatorDigest.Field);
        }

        return EvaluationDecision<VerifiedEvidenceLocator>.Ok(
            new VerifiedEvidenceLocator(
                sourceType,
                sourceRefDigest.Value!,
                locationDigest.Value!,
                verificationState,
                material.ContentDigest,
                verifiedPrecision,
                verifiedLocatorDigest.Value!));
    }

    private static LocationVerificationResult VerifyLocation(
        string sourceType,
        JsonElement location,
        string precision,
        ResolvedMaterial material,
        bool permitWholeItemFallback)
    {
        if (!TryGetRequiredString(location, "location_type", out var locationType))
        {
            return LocationVerificationResult.Fail(EvaluationFailureCodes.InvalidField, "location.location_type");
        }

        return locationType switch
        {
            "whole_item" when precision is "whole_item" =>
                VerifyWholeItem(location, material),
            "utf8_byte_range" when precision is "exact_range" =>
                VerifyByteRange(location, material, permitWholeItemFallback),
            "line_range" when precision is "exact_range" =>
                VerifyLineRange(location, material, permitWholeItemFallback),
            "json_pointer" when precision is "exact_range" =>
                VerifyJsonPointer(sourceType, location, material),
            _ => LocationVerificationResult.Fail(EvaluationFailureCodes.InvalidField, "precision"),
        };
    }

    private static LocationVerificationResult VerifyWholeItem(JsonElement location, ResolvedMaterial material)
    {
        if (!TryGetRequiredString(location, "item_id", out var itemId)
            || !string.Equals(itemId, material.ItemId, StringComparison.Ordinal))
        {
            return LocationVerificationResult.Fail(EvaluationFailureCodes.CitationIntegrity, "location.item_id");
        }

        return LocationVerificationResult.Ok("verified");
    }

    private static LocationVerificationResult VerifyByteRange(
        JsonElement location,
        ResolvedMaterial material,
        bool permitWholeItemFallback)
    {
        if (!TryGetRequiredString(location, "item_id", out var itemId)
            || !string.Equals(itemId, material.ItemId, StringComparison.Ordinal)
            || !TryGetRequiredString(location, "excerpt_digest", out var excerptDigest)
            || !location.TryGetProperty("start_inclusive", out var startProperty)
            || !location.TryGetProperty("end_exclusive", out var endProperty)
            || !startProperty.TryGetInt32(out var startInclusive)
            || !endProperty.TryGetInt32(out var endExclusive))
        {
            return LocationVerificationResult.Fail(EvaluationFailureCodes.InvalidField, "location");
        }

        var range = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(
            material.ExactUtf8,
            startInclusive,
            endExclusive,
            excerptDigest);
        if (range.Succeeded)
        {
            return LocationVerificationResult.Ok("verified");
        }

        if (permitWholeItemFallback)
        {
            var wholeItemLocation = EvidenceLocatorVerifiedProjection.CreateWholeItemLocation(itemId);
            var wholeItem = VerifyWholeItem(wholeItemLocation, material);
            return wholeItem.Succeeded
                ? LocationVerificationResult.Ok("lower_precision", wholeItemLocation)
                : wholeItem;
        }

        return LocationVerificationResult.Fail(range.OutcomeCode, range.Field);
    }

    private static LocationVerificationResult VerifyLineRange(
        JsonElement location,
        ResolvedMaterial material,
        bool permitWholeItemFallback)
    {
        if (!TryGetRequiredString(location, "item_id", out var itemId)
            || !string.Equals(itemId, material.ItemId, StringComparison.Ordinal)
            || !TryGetRequiredString(location, "line_split_procedure_version", out var splitVersion)
            || !location.TryGetProperty("start_line_inclusive", out var startProperty)
            || !location.TryGetProperty("end_line_inclusive", out var endProperty)
            || !startProperty.TryGetInt32(out var startLineInclusive)
            || !endProperty.TryGetInt32(out var endLineInclusive))
        {
            return LocationVerificationResult.Fail(EvaluationFailureCodes.InvalidField, "location");
        }

        var range = EvidenceTextSourceNormalizer.TryExtractLineRange(
            material.ExactUtf8,
            startLineInclusive,
            endLineInclusive,
            splitVersion);
        if (range.Succeeded)
        {
            return LocationVerificationResult.Ok("verified");
        }

        if (permitWholeItemFallback)
        {
            var wholeItemLocation = EvidenceLocatorVerifiedProjection.CreateWholeItemLocation(itemId);
            var wholeItem = VerifyWholeItem(wholeItemLocation, material);
            return wholeItem.Succeeded
                ? LocationVerificationResult.Ok("lower_precision", wholeItemLocation)
                : wholeItem;
        }

        return LocationVerificationResult.Fail(range.OutcomeCode, range.Field);
    }

    private static LocationVerificationResult VerifyJsonPointer(
        string sourceType,
        JsonElement location,
        ResolvedMaterial material)
    {
        if (sourceType is not ("configuration.fact" or "manifest.fact" or "deterministic.fact")
            || !TryGetRequiredString(location, "json_pointer", out var jsonPointer))
        {
            return LocationVerificationResult.Fail(EvaluationFailureCodes.InvalidField, "location.json_pointer");
        }

        if (material.ProjectionUtf8 is null
            || !TryResolveJsonPointer(material.ProjectionUtf8.Value, jsonPointer))
        {
            return LocationVerificationResult.Fail(
                EvaluationFailureCodes.ProtectedContent,
                "location.json_pointer");
        }

        return LocationVerificationResult.Ok("verified");
    }

    private static ResolvedMaterial? ResolveMaterial(
        string sourceType,
        JsonElement sourceRef,
        EvidenceLocatorVerificationContext context)
    {
        if (!TryGetRequiredString(sourceRef, "source_id", out var sourceId)
            || !TryGetRequiredString(sourceRef, "source_version", out var sourceVersion))
        {
            return null;
        }

        return sourceType switch
        {
            "submission.direct_text" or "submission.text_attachment"
                when context.SubmissionItemsBySourceId.TryGetValue(sourceId, out var submission)
                     && string.Equals(submission.SourceVersion, sourceVersion, StringComparison.Ordinal)
                     && string.Equals(submission.Category, sourceType, StringComparison.Ordinal) =>
                new ResolvedMaterial(
                    sourceId,
                    submission.ContentDigest,
                    submission.ExactUtf8,
                    null),
            "session.transcript_item"
                when context.TranscriptItemsByMessageId.TryGetValue(sourceId, out var transcript)
                     && string.Equals(transcript.SourceVersion, sourceVersion, StringComparison.Ordinal)
                     && transcript.PublishedSequence <= context.TerminalCutoffSequence =>
                new ResolvedMaterial(
                    sourceId,
                    transcript.ContentDigest,
                    transcript.ExactUtf8,
                    null),
            "session.transcript_item"
                when context.TranscriptItemsByMessageId.ContainsKey(sourceId) =>
                null,
            "session.work_trace"
                when context.WorkTraceItemsBySourceId.TryGetValue(sourceId, out var workTrace)
                     && string.Equals(workTrace.SourceVersion, sourceVersion, StringComparison.Ordinal)
                     && workTrace.PublishedSequence <= context.TerminalCutoffSequence =>
                new ResolvedMaterial(
                    sourceId,
                    workTrace.ContentDigest,
                    workTrace.ExactUtf8,
                    null),
            "session.work_trace"
                when context.WorkTraceItemsBySourceId.ContainsKey(sourceId) =>
                null,
            "configuration.fact"
                when context.SafeConfigurationFactsBySourceId.TryGetValue(sourceId, out var configuration)
                     && string.Equals(configuration.SourceVersion, sourceVersion, StringComparison.Ordinal) =>
                new ResolvedMaterial(
                    sourceId,
                    configuration.ContentDigest,
                    configuration.ProjectionUtf8,
                    configuration.ProjectionUtf8),
            "manifest.fact"
                when context.SafeManifestFactsBySourceId.TryGetValue(sourceId, out var manifest)
                     && string.Equals(manifest.SourceVersion, sourceVersion, StringComparison.Ordinal) =>
                new ResolvedMaterial(
                    sourceId,
                    manifest.ContentDigest,
                    manifest.ProjectionUtf8,
                    manifest.ProjectionUtf8),
            "deterministic.fact"
                when context.SafeDeterministicFactsBySourceId.TryGetValue(sourceId, out var deterministic)
                     && string.Equals(deterministic.SourceVersion, sourceVersion, StringComparison.Ordinal) =>
                new ResolvedMaterial(
                    sourceId,
                    deterministic.ContentDigest,
                    deterministic.ProjectionUtf8,
                    deterministic.ProjectionUtf8),
            _ => null,
        };
    }

    private static bool OwnershipMatches(
        JsonElement ownershipRef,
        EvaluationStableOwnershipReference trusted) =>
        TryGetRequiredString(ownershipRef, "organization_id", out var organizationId)
        && TryGetRequiredString(ownershipRef, "activity_id", out var activityId)
        && TryGetRequiredString(ownershipRef, "participant_id", out var participantId)
        && TryGetRequiredString(ownershipRef, "attempt_id", out var attemptId)
        && TryGetRequiredString(ownershipRef, "session_id", out var sessionId)
        && TryGetRequiredString(ownershipRef, "evaluation_id", out var evaluationId)
        && string.Equals(organizationId, trusted.OrganizationId, StringComparison.Ordinal)
        && string.Equals(activityId, trusted.ActivityId, StringComparison.Ordinal)
        && string.Equals(participantId, trusted.ParticipantId, StringComparison.Ordinal)
        && string.Equals(attemptId, trusted.AttemptId, StringComparison.Ordinal)
        && string.Equals(sessionId, trusted.SessionId, StringComparison.Ordinal)
        && string.Equals(evaluationId, trusted.EvaluationId, StringComparison.Ordinal);

    private static bool RequiresTerminalCutoff(string sourceType) =>
        sourceType is "session.transcript_item" or "session.work_trace";

    private static bool TryResolveJsonPointer(ReadOnlyMemory<byte> projectionUtf8, string jsonPointer)
    {
        try
        {
            using var document = JsonDocument.Parse(projectionUtf8);
            var element = document.RootElement;
            if (jsonPointer == "/")
            {
                return true;
            }

            foreach (var segment in jsonPointer.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var unescaped = segment.Replace("~1", "/", StringComparison.Ordinal)
                    .Replace("~0", "~", StringComparison.Ordinal);
                if (int.TryParse(unescaped, out var index))
                {
                    if (element.ValueKind != JsonValueKind.Array
                        || index < 0
                        || index >= element.GetArrayLength())
                    {
                        return false;
                    }

                    element = element[index];
                    continue;
                }

                if (!element.TryGetProperty(unescaped, out element))
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

    private sealed record LocationVerificationResult(
        bool Succeeded,
        string OutcomeCode,
        string VerificationState,
        string? Field,
        JsonElement? EffectiveLocation = null)
    {
        public static LocationVerificationResult Ok(
            string verificationState,
            JsonElement? effectiveLocation = null) =>
            new(true, "evaluation.ok", verificationState, null, effectiveLocation);

        public static LocationVerificationResult Fail(string outcomeCode, string? field) =>
            new(false, outcomeCode, "failed", field);
    }
}
