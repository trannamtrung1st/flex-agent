using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Sessions.Domain;

internal static class RuntimePolicyEffectiveValuesDigestComputer
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    internal static string Compute(RuntimePolicyEffectiveValues values)
    {
        var json = BuildCanonicalPayload(values);
        return CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(json), Limits);
    }

    private static string BuildCanonicalPayload(RuntimePolicyEffectiveValues values)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("decision_contract_version", values.DecisionContractVersion);
            writer.WriteString("decision_validation_policy_version", values.DecisionValidationPolicyVersion);
            writer.WriteString("invocation_contract_version", values.InvocationContractVersion);

            writer.WritePropertyName("decision_schema_bindings");
            writer.WriteStartArray();
            foreach (var binding in (values.DecisionSchemaBindings ?? [])
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
            foreach (var capability in (values.ExplicitlyDisabledCapabilities ?? [])
                         .Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(capability);
            }

            writer.WriteEndArray();

            if (values.InvocationBounds is not null)
            {
                writer.WriteStartObject("invocation_bounds");
                writer.WriteNumber("cooldown_seconds", values.InvocationBounds.CooldownSeconds);
                writer.WriteNumber(
                    "duplicate_suppression_window_seconds",
                    values.InvocationBounds.DuplicateSuppressionWindowSeconds);
                writer.WriteNumber(
                    "max_attempts_per_invocation",
                    values.InvocationBounds.MaxAttemptsPerInvocation);
                writer.WriteNumber(
                    "max_chained_invocations_per_session",
                    values.InvocationBounds.MaxChainedInvocationsPerSession);
                writer.WriteNumber("max_tool_iterations", values.InvocationBounds.MaxToolIterations);
                writer.WriteEndObject();
            }

            writer.WriteBoolean("no_action_permitted", values.NoActionPermitted!.Value);
            writer.WriteBoolean(
                "agent_initiated_opening_permitted",
                values.AgentInitiatedOpeningPermitted!.Value);
            writer.WriteBoolean(
                "agent_initiated_closing_permitted",
                values.AgentInitiatedClosingPermitted!.Value);

            writer.WritePropertyName("permitted_decision_types");
            writer.WriteStartArray();
            foreach (var decisionType in (values.PermittedDecisionTypes ?? []).Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(decisionType);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("permitted_non_timer_triggers");
            writer.WriteStartArray();
            foreach (var trigger in (values.PermittedNonTimerTriggers ?? [])
                         .OrderBy(static trigger => trigger.TriggerFamily, StringComparer.Ordinal)
                         .ThenBy(static trigger => trigger.TriggerType, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("trigger_family", trigger.TriggerFamily);
                writer.WriteString("trigger_type", trigger.TriggerType);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            if (values.TimerLane is null)
            {
                writer.WriteNull("timer_lane");
            }
            else
            {
                WriteTimerLane(writer, values.TimerLane);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteTimerLane(Utf8JsonWriter writer, TimerLanePolicyValues timerLane)
    {
        writer.WriteStartObject("timer_lane");
        writer.WriteBoolean("enabled", timerLane.Enabled);
        writer.WriteString("clock_basis", timerLane.ClockBasis);
        writer.WriteString("default_delay", timerLane.DefaultDelay);
        writer.WriteString("max_requested_delay", timerLane.MaxRequestedDelay);
        writer.WriteString("min_requested_delay", timerLane.MinRequestedDelay);

        if (timerLane.Budgets is not null)
        {
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
        }

        writer.WritePropertyName("permitted_decision_types");
        writer.WriteStartArray();
        foreach (var decisionType in (timerLane.PermittedDecisionTypes ?? []).Order(StringComparer.Ordinal))
        {
            writer.WriteStringValue(decisionType);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("permitted_stages");
        writer.WriteStartArray();
        foreach (var stage in (timerLane.PermittedStages ?? []).Order(StringComparer.Ordinal))
        {
            writer.WriteStringValue(stage);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
