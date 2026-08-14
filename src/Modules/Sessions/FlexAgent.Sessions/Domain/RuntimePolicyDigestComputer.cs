using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Sessions.Domain;

internal static class RuntimePolicyDigestComputer
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    internal static string ComputeDigest(FrozenTextSessionRuntimePolicy policy)
    {
        var json = BuildCanonicalPayload(policy);
        return CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(json), Limits);
    }

    private static string BuildCanonicalPayload(FrozenTextSessionRuntimePolicy policy)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("domain_key", RuntimePolicyDomainKeys.AgentInvocationPolicy);
            writer.WriteString("decision_contract_version", policy.DecisionContractVersion);
            writer.WriteString("decision_validation_policy_version", policy.DecisionValidationPolicyVersion);
            writer.WriteString("invocation_contract_version", policy.InvocationContractVersion);

            writer.WritePropertyName("decision_schema_bindings");
            writer.WriteStartArray();
            foreach (var binding in policy.DecisionSchemaBindings
                         .OrderBy(static binding => binding.DecisionType, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("decision_type", binding.DecisionType);
                writer.WriteString("schema_version", binding.SchemaVersion);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WritePropertyName("explicitly_disabled_capabilities");
            writer.WriteStartArray();
            foreach (var capability in policy.ExplicitlyDisabledCapabilities.Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(capability);
            }

            writer.WriteEndArray();

            writer.WriteStartObject("invocation_bounds");
            writer.WriteNumber("cooldown_seconds", policy.InvocationBounds.CooldownSeconds);
            writer.WriteNumber(
                "duplicate_suppression_window_seconds",
                policy.InvocationBounds.DuplicateSuppressionWindowSeconds);
            writer.WriteNumber(
                "max_attempts_per_invocation",
                policy.InvocationBounds.MaxAttemptsPerInvocation);
            writer.WriteNumber(
                "max_chained_invocations_per_session",
                policy.InvocationBounds.MaxChainedInvocationsPerSession);
            writer.WriteNumber("max_tool_iterations", policy.InvocationBounds.MaxToolIterations);
            writer.WriteEndObject();

            WriteStreamingPublicationBounds(writer, policy.StreamingPublicationBounds);

            writer.WriteBoolean("no_action_permitted", policy.NoActionPermitted);
            writer.WriteBoolean("agent_initiated_opening_permitted", policy.AgentInitiatedOpeningPermitted);
            writer.WriteBoolean("agent_initiated_closing_permitted", policy.AgentInitiatedClosingPermitted);

            writer.WritePropertyName("permitted_decision_types");
            writer.WriteStartArray();
            foreach (var decisionType in policy.PermittedDecisionTypes.Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(decisionType);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("permitted_non_timer_triggers");
            writer.WriteStartArray();
            foreach (var trigger in policy.PermittedNonTimerTriggers
                         .OrderBy(static trigger => trigger.TriggerFamily, StringComparer.Ordinal)
                         .ThenBy(static trigger => trigger.TriggerType, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("trigger_family", trigger.TriggerFamily);
                writer.WriteString("trigger_type", trigger.TriggerType);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            if (policy.TimerLane is null)
            {
                writer.WriteNull("timer_lane");
            }
            else
            {
                WriteTimerLane(writer, policy.TimerLane);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteStreamingPublicationBounds(
        Utf8JsonWriter writer,
        StreamingPublicationBounds bounds)
    {
        writer.WriteStartObject("streaming_publication_bounds");
        writer.WriteNumber("max_assembled_response_utf8_bytes", bounds.MaxAssembledResponseUtf8Bytes);
        writer.WriteNumber("max_fragment_count_per_message", bounds.MaxFragmentCountPerMessage);
        writer.WriteNumber("max_fragment_utf8_bytes", bounds.MaxFragmentUtf8Bytes);
        writer.WriteNumber("max_fragments_per_second", bounds.MaxFragmentsPerSecond);
        writer.WriteNumber("max_in_flight_streams_per_session", bounds.MaxInFlightStreamsPerSession);
        writer.WriteEndObject();
    }

    private static void WriteTimerLane(Utf8JsonWriter writer, TimerLanePolicy timerLane)
    {
        writer.WriteStartObject("timer_lane");
        writer.WriteString("clock_basis", timerLane.ClockBasis);
        writer.WriteString("default_delay", timerLane.DefaultDelay.WireValue);
        writer.WriteString("max_requested_delay", timerLane.MaxRequestedDelay.WireValue);
        writer.WriteString("min_requested_delay", timerLane.MinRequestedDelay.WireValue);

        writer.WriteStartObject("budgets");
        writer.WriteNumber("cooldown_seconds", timerLane.Budgets.CooldownSeconds);
        writer.WriteNumber(
            "duplicate_suppression_window_seconds",
            timerLane.Budgets.DuplicateSuppressionWindowSeconds);
        writer.WriteNumber(
            "max_accepted_replacements_per_session",
            timerLane.Budgets.MaxAcceptedReplacementsPerSession);
        writer.WriteNumber(
            "max_concurrent_replacements",
            timerLane.Budgets.MaxConcurrentReplacements);
        writer.WriteNumber(
            "max_timer_triggered_invocations_per_session",
            timerLane.Budgets.MaxTimerTriggeredInvocationsPerSession);
        writer.WriteEndObject();

        writer.WritePropertyName("permitted_decision_types");
        writer.WriteStartArray();
        foreach (var decisionType in timerLane.PermittedDecisionTypes.Order(StringComparer.Ordinal))
        {
            writer.WriteStringValue(decisionType);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("permitted_stages");
        writer.WriteStartArray();
        foreach (var stage in timerLane.PermittedStages.Order(StringComparer.Ordinal))
        {
            writer.WriteStringValue(stage);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
