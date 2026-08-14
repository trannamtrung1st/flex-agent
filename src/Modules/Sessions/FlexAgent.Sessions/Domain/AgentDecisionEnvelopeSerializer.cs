using System.Text.Json;

namespace FlexAgent.Sessions.Domain;

internal static class AgentDecisionEnvelopeSerializer
{
    internal static byte[] ToUtf8Json(EnvelopeRecommendation envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("schema_version", "v2");
            writer.WriteString("agent_decision_id", envelope.DecisionId);
            writer.WriteString("agent_invocation_id", envelope.InvocationId);
            writer.WriteString("produced_at", FormatUtc(envelope.ProducedAt));
            writer.WriteString("disposition", envelope.Disposition);
            writer.WritePropertyName("outputs");
            writer.WriteStartArray();
            foreach (var output in envelope.Outputs)
            {
                WriteOutput(writer, output);
            }

            writer.WriteEndArray();
            writer.WritePropertyName("requested_actions");
            writer.WriteStartArray();
            foreach (var action in envelope.RequestedActions)
            {
                WriteAction(writer, action);
            }

            writer.WriteEndArray();
            if (string.Equals(envelope.Disposition, DecisionDispositions.NoAction, StringComparison.Ordinal)
                && envelope.NoActionReasonCategory is not null)
            {
                writer.WritePropertyName("no_action");
                writer.WriteStartObject();
                writer.WriteString("reason_category", envelope.NoActionReasonCategory);
                writer.WriteEndObject();
            }

            WritePayloadRef(writer, envelope.PayloadRef, omitWhenNull: true);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static void WriteOutput(Utf8JsonWriter writer, OutputRecommendation output)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", output.Kind);
        writer.WriteString("local_ref", output.LocalRef);
        WriteOptional(writer, "communication_purpose", output.CommunicationPurpose);
        WriteOptional(writer, "turn_id", output.TurnId);
        WriteOptional(writer, "response_slot_id", output.ResponseSlotId);
        WriteOptional(writer, "agent_output_id", output.ModelAgentOutputId);
        WriteOptional(writer, "audience", output.ModelAudience);
        if (output.References is { Count: > 0 })
        {
            writer.WritePropertyName("references");
            writer.WriteStartArray();
            foreach (var reference in output.References)
            {
                writer.WriteStartObject();
                writer.WriteString("relation", reference.Relation);
                writer.WriteString("local_ref", reference.LocalRef);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        WritePayloadRef(writer, output.PayloadRef, omitWhenNull: true);
        writer.WriteEndObject();
    }

    private static void WriteAction(Utf8JsonWriter writer, RequestedActionRecommendation action)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", action.Kind);
        writer.WriteString("local_ref", action.LocalRef);
        WriteOptional(writer, "relative_delay", action.RelativeDelay);
        WriteOptional(writer, "expected_schedule_revision", action.ExpectedScheduleRevision);
        writer.WriteEndObject();
    }

    private static void WritePayloadRef(Utf8JsonWriter writer, ProtectedContentRef? payloadRef, bool omitWhenNull)
    {
        if (payloadRef is null)
        {
            if (!omitWhenNull)
            {
                writer.WriteNull("payload_ref");
            }

            return;
        }

        writer.WritePropertyName("payload_ref");
        writer.WriteStartObject();
        writer.WriteString("protected_ref", payloadRef.ProtectedRef);
        writer.WriteString("content_digest", payloadRef.ContentDigest);
        writer.WriteEndObject();
    }

    private static void WriteOptional(Utf8JsonWriter writer, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteString(name, value);
        }
    }

    private static string FormatUtc(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return utc.Ticks % TimeSpan.TicksPerSecond == 0
            ? utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
            : utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");
    }
}
