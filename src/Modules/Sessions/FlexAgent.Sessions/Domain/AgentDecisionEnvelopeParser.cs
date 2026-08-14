using System.Text.Json;

namespace FlexAgent.Sessions.Domain;

public static class AgentDecisionEnvelopeParser
{
    private static readonly HashSet<string> MessageOutputProperties =
    [
        "kind",
        "local_ref",
        "communication_purpose",
        "turn_id",
        "response_slot_id",
        "agent_output_id",
        "audience",
        "references",
    ];

    private static readonly HashSet<string> VoiceOutputProperties =
    [
        "kind",
        "local_ref",
        "agent_output_id",
        "audience",
        "references",
        "payload_ref",
    ];

    private static readonly HashSet<string> EnvelopeProperties =
    [
        "schema_version",
        "agent_decision_id",
        "agent_invocation_id",
        "produced_at",
        "disposition",
        "outputs",
        "requested_actions",
        "no_action",
        "payload_ref",
    ];

    public static EnvelopeParseResult Parse(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            return Fail(EnvelopeParseOutcomeCodes.IncompleteControl, ExecutionFailureReasons.IncompleteControl);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json.ToArray());
        }
        catch (JsonException)
        {
            return Fail(EnvelopeParseOutcomeCodes.MalformedControl, ExecutionFailureReasons.MalformedControl);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Fail(EnvelopeParseOutcomeCodes.MalformedControl, ExecutionFailureReasons.MalformedControl);
            }

            var root = document.RootElement;
            foreach (var property in root.EnumerateObject())
            {
                if (!EnvelopeProperties.Contains(property.Name))
                {
                    return Fail(EnvelopeParseOutcomeCodes.MalformedControl, ExecutionFailureReasons.MalformedControl);
                }
            }

            if (!TryReadRequiredString(root, "schema_version", out var schemaVersion)
                || !TryReadRequiredString(root, "agent_decision_id", out var decisionId)
                || !TryReadRequiredString(root, "agent_invocation_id", out var invocationId)
                || !TryReadRequiredString(root, "produced_at", out var producedAtRaw)
                || !TryReadRequiredString(root, "disposition", out var disposition))
            {
                return Fail(EnvelopeParseOutcomeCodes.IncompleteControl, ExecutionFailureReasons.IncompleteControl);
            }

            if (!string.Equals(schemaVersion, "v2", StringComparison.Ordinal)
                || (disposition is not (DecisionDispositions.Respond or DecisionDispositions.NoAction))
                || !DateTimeOffset.TryParse(producedAtRaw, out var producedAt)
                || producedAt.Offset != TimeSpan.Zero)
            {
                return Fail(EnvelopeParseOutcomeCodes.MalformedControl, ExecutionFailureReasons.MalformedControl);
            }

            if (!root.TryGetProperty("outputs", out var outputsElement)
                || !root.TryGetProperty("requested_actions", out var actionsElement)
                || outputsElement.ValueKind != JsonValueKind.Array
                || actionsElement.ValueKind != JsonValueKind.Array)
            {
                return Fail(EnvelopeParseOutcomeCodes.IncompleteControl, ExecutionFailureReasons.IncompleteControl);
            }

            if (!TryParseOutputs(outputsElement, out var outputs)
                || !TryParseActions(actionsElement, out var actions))
            {
                return Fail(EnvelopeParseOutcomeCodes.MalformedControl, ExecutionFailureReasons.MalformedControl);
            }

            string? noActionReason = null;
            var hasNoAction = root.TryGetProperty("no_action", out var noActionElement);
            if (string.Equals(disposition, DecisionDispositions.NoAction, StringComparison.Ordinal))
            {
                if (!hasNoAction
                    || noActionElement.ValueKind != JsonValueKind.Object
                    || !TryReadRequiredString(noActionElement, "reason_category", out noActionReason))
                {
                    return Fail(EnvelopeParseOutcomeCodes.IncompleteControl, ExecutionFailureReasons.IncompleteControl);
                }
            }
            else if (hasNoAction)
            {
                return Fail(EnvelopeParseOutcomeCodes.MalformedControl, ExecutionFailureReasons.MalformedControl);
            }

            return new EnvelopeParseResult(
                true,
                EnvelopeParseOutcomeCodes.Succeeded,
                null,
                new EnvelopeRecommendation(
                    decisionId,
                    invocationId,
                    producedAt,
                    disposition,
                    outputs,
                    actions,
                    noActionReason));
        }
    }

    private static bool TryParseOutputs(JsonElement element, out IReadOnlyList<OutputRecommendation> outputs)
    {
        var parsed = new List<OutputRecommendation>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryReadRequiredString(item, "kind", out var kind)
                || !TryReadRequiredString(item, "local_ref", out var localRef)
                || kind is not (AgentOutputKinds.Message or AgentOutputKinds.Voice)
                || !OutputPropertiesAreAllowed(item, kind))
            {
                outputs = [];
                return false;
            }

            if (string.Equals(kind, AgentOutputKinds.Message, StringComparison.Ordinal)
                && !TryReadRequiredString(item, "communication_purpose", out _))
            {
                outputs = [];
                return false;
            }

            if (!TryReadOptionalPayloadRef(item, out var payloadRef))
            {
                outputs = [];
                return false;
            }

            parsed.Add(new OutputRecommendation(
                kind,
                localRef,
                ReadOptionalString(item, "communication_purpose"),
                ReadOptionalString(item, "turn_id"),
                ReadOptionalString(item, "response_slot_id"),
                ReadOptionalString(item, "agent_output_id"),
                ReadOptionalString(item, "audience"),
                PayloadRef: payloadRef));
        }

        outputs = parsed;
        return true;
    }

    private static bool TryParseActions(JsonElement element, out IReadOnlyList<RequestedActionRecommendation> actions)
    {
        var parsed = new List<RequestedActionRecommendation>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryReadRequiredString(item, "kind", out var kind)
                || !TryReadRequiredString(item, "local_ref", out var localRef))
            {
                actions = [];
                return false;
            }

            if (kind is not (
                AgentRequestedActionKinds.NextTimerRequest
                or AgentRequestedActionKinds.RequestTool
                or AgentRequestedActionKinds.ProposeTransition
                or AgentRequestedActionKinds.Escalate))
            {
                actions = [];
                return false;
            }

            parsed.Add(new RequestedActionRecommendation(
                kind,
                localRef,
                ReadOptionalString(item, "relative_delay"),
                ReadOptionalString(item, "expected_schedule_revision")));
        }

        actions = parsed;
        return true;
    }

    private static bool OutputPropertiesAreAllowed(JsonElement item, string kind)
    {
        var allowed = string.Equals(kind, AgentOutputKinds.Voice, StringComparison.Ordinal)
            ? VoiceOutputProperties
            : MessageOutputProperties;
        foreach (var property in item.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryReadOptionalPayloadRef(JsonElement item, out ProtectedContentRef? payloadRef)
    {
        payloadRef = null;
        if (!item.TryGetProperty("payload_ref", out var payload))
        {
            return true;
        }

        if (payload.ValueKind != JsonValueKind.Object
            || !TryReadRequiredString(payload, "protected_ref", out var protectedRef)
            || !TryReadRequiredString(payload, "content_digest", out var digest))
        {
            return false;
        }

        payloadRef = new ProtectedContentRef(protectedRef, digest);
        return true;
    }

    private static bool TryReadRequiredString(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? ReadOptionalString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static EnvelopeParseResult Fail(string outcomeCode, string reasonCategory) =>
        new(false, outcomeCode, reasonCategory, null);
}
