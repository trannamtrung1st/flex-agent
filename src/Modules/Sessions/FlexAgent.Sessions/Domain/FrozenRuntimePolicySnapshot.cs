using System.Text.Json;

namespace FlexAgent.Sessions.Domain;

internal static class FrozenRuntimePolicySnapshot
{
    internal static string ToCanonicalJson(FrozenTextSessionRuntimePolicy policy) =>
        RuntimePolicyDigestComputer.BuildCanonicalPayload(policy);

    internal static FrozenTextSessionRuntimePolicy? TryParse(string json, string expectedDigest)
    {
        if (string.IsNullOrWhiteSpace(json)
            || string.IsNullOrWhiteSpace(expectedDigest)
            || !LowercaseSha256(expectedDigest))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryReadEffectiveValues(document.RootElement, out var values))
            {
                return null;
            }

            return FrozenRuntimePolicyResolver.TryRehydrate(values, expectedDigest);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool LowercaseSha256(string digest) =>
        digest.Length == 64 && digest.All(static character =>
            char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');

    private static bool TryReadEffectiveValues(JsonElement root, out RuntimePolicyEffectiveValues values)
    {
        values = null!;
        if (!TryReadString(root, "invocation_contract_version", out var invocationContract)
            || !TryReadString(root, "decision_contract_version", out var decisionContract)
            || !TryReadString(root, "decision_validation_policy_version", out var validationPolicy)
            || !TryReadSchemaBindings(root, out var schemaBindings)
            || !TryReadTriggers(root, out var triggers)
            || !TryReadStringArray(root, "permitted_decision_types", out var decisionTypes)
            || !TryReadStringArray(root, "explicitly_disabled_capabilities", out var disabled)
            || !TryReadBoolean(root, "agent_initiated_opening_permitted", out var opening)
            || !TryReadBoolean(root, "agent_initiated_closing_permitted", out var closing)
            || !TryReadBoolean(root, "no_action_permitted", out var noAction)
            || !TryReadInvocationBounds(root, out var invocationBounds)
            || !TryReadStreamingBounds(root, out var streamingBounds)
            || !TryReadTimerLane(root, out var timerLane))
        {
            return false;
        }

        values = new RuntimePolicyEffectiveValues
        {
            InvocationContractVersion = invocationContract,
            DecisionContractVersion = decisionContract,
            DecisionValidationPolicyVersion = validationPolicy,
            DecisionSchemaBindings = schemaBindings,
            PermittedNonTimerTriggers = triggers,
            PermittedDecisionTypes = decisionTypes,
            ExplicitlyDisabledCapabilities = disabled,
            AgentInitiatedOpeningPermitted = opening,
            AgentInitiatedClosingPermitted = closing,
            NoActionPermitted = noAction,
            InvocationBounds = invocationBounds,
            StreamingPublicationBounds = streamingBounds,
            TimerLane = timerLane,
        };
        return true;
    }

    private static bool TryReadString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadBoolean(JsonElement root, string name, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(name, out var element)
            || (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False))
        {
            return false;
        }

        value = element.GetBoolean();
        return true;
    }

    private static bool TryReadInt(JsonElement root, string name, out int value)
    {
        value = 0;
        return root.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out value);
    }

    private static bool TryReadStringArray(JsonElement root, string name, out IReadOnlyList<string> values)
    {
        values = [];
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var items = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var value = item.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            items.Add(value);
        }

        values = items;
        return true;
    }

    private static bool TryReadSchemaBindings(JsonElement root, out IReadOnlyList<DecisionTypeSchemaBinding> bindings)
    {
        bindings = [];
        if (!root.TryGetProperty("decision_schema_bindings", out var element)
            || element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var items = new List<DecisionTypeSchemaBinding>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryReadString(item, "decision_type", out var decisionType)
                || !TryReadString(item, "schema_version", out var schemaVersion))
            {
                return false;
            }

            items.Add(new DecisionTypeSchemaBinding(decisionType, schemaVersion));
        }

        bindings = items;
        return true;
    }

    private static bool TryReadTriggers(JsonElement root, out IReadOnlyList<RuntimeTriggerDescriptor> triggers)
    {
        triggers = [];
        if (!root.TryGetProperty("permitted_non_timer_triggers", out var element)
            || element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var items = new List<RuntimeTriggerDescriptor>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !TryReadString(item, "trigger_family", out var family)
                || !TryReadString(item, "trigger_type", out var type))
            {
                return false;
            }

            items.Add(new RuntimeTriggerDescriptor(family, type));
        }

        triggers = items;
        return true;
    }

    private static bool TryReadInvocationBounds(JsonElement root, out InvocationBounds bounds)
    {
        bounds = null!;
        if (!root.TryGetProperty("invocation_bounds", out var element)
            || element.ValueKind != JsonValueKind.Object
            || !TryReadInt(element, "max_attempts_per_invocation", out var maxAttempts)
            || !TryReadInt(element, "max_chained_invocations_per_session", out var maxChained)
            || !TryReadInt(element, "max_tool_iterations", out var maxTools)
            || !TryReadInt(element, "cooldown_seconds", out var cooldown)
            || !TryReadInt(element, "duplicate_suppression_window_seconds", out var suppression))
        {
            return false;
        }

        bounds = new InvocationBounds(maxAttempts, maxChained, maxTools, cooldown, suppression);
        return true;
    }

    private static bool TryReadStreamingBounds(JsonElement root, out StreamingPublicationBounds bounds)
    {
        bounds = null!;
        if (!root.TryGetProperty("streaming_publication_bounds", out var element)
            || element.ValueKind != JsonValueKind.Object
            || !TryReadInt(element, "max_fragment_utf8_bytes", out var fragmentBytes)
            || !TryReadInt(element, "max_fragments_per_second", out var fragmentsPerSecond)
            || !TryReadInt(element, "max_fragment_count_per_message", out var fragmentCount)
            || !TryReadInt(element, "max_assembled_response_utf8_bytes", out var assembledBytes)
            || !TryReadInt(element, "max_in_flight_streams_per_session", out var inFlight))
        {
            return false;
        }

        bounds = new StreamingPublicationBounds(
            fragmentBytes,
            fragmentsPerSecond,
            fragmentCount,
            assembledBytes,
            inFlight);
        return true;
    }

    private static bool TryReadTimerLane(JsonElement root, out TimerLanePolicyValues? timerLane)
    {
        timerLane = null;
        if (!root.TryGetProperty("timer_lane", out var element))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Object
            || !TryReadString(element, "clock_basis", out var clockBasis)
            || !TryReadString(element, "default_delay", out var defaultDelay)
            || !TryReadString(element, "min_requested_delay", out var minDelay)
            || !TryReadString(element, "max_requested_delay", out var maxDelay)
            || !TryReadStringArray(element, "permitted_stages", out var stages)
            || !TryReadStringArray(element, "permitted_decision_types", out var decisionTypes)
            || !element.TryGetProperty("budgets", out var budgetsElement)
            || budgetsElement.ValueKind != JsonValueKind.Object
            || !TryReadInt(budgetsElement, "max_accepted_replacements_per_session", out var maxReplacements)
            || !TryReadInt(budgetsElement, "max_timer_triggered_invocations_per_session", out var maxInvocations)
            || !TryReadInt(budgetsElement, "cooldown_seconds", out var cooldown)
            || !TryReadInt(budgetsElement, "max_concurrent_replacements", out var maxConcurrent)
            || !TryReadInt(budgetsElement, "duplicate_suppression_window_seconds", out var suppression))
        {
            return false;
        }

        timerLane = new TimerLanePolicyValues
        {
            Enabled = true,
            ClockBasis = clockBasis,
            DefaultDelay = defaultDelay,
            MinRequestedDelay = minDelay,
            MaxRequestedDelay = maxDelay,
            PermittedStages = stages,
            PermittedDecisionTypes = decisionTypes,
            Budgets = new TimerLaneBudgets(
                maxReplacements,
                maxInvocations,
                cooldown,
                maxConcurrent,
                suppression),
        };
        return true;
    }
}
