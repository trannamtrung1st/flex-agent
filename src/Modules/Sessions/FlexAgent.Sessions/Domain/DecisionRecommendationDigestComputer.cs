using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Sessions.Domain;

internal static class DecisionRecommendationDigestComputer
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 8,
        maxObjectProperties: 64,
        maxArrayElements: 16);

    internal static string Compute(DecisionRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        var json = BuildCanonicalPayload(recommendation);
        return CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(json), Limits);
    }

    private static string BuildCanonicalPayload(DecisionRecommendation recommendation)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("decision_id", recommendation.DecisionId);
            writer.WriteString("decision_type", recommendation.DecisionType);
            writer.WriteString("invocation_id", recommendation.InvocationId);
            if (recommendation is EnvelopeRecommendation envelope)
            {
                WriteEnvelope(writer, envelope);
            }

            if (recommendation is EmitMessageRecommendation emit)
            {
                writer.WriteString("communication_purpose", emit.CommunicationPurpose);
            }

            if (recommendation.NextTimer is null)
            {
                writer.WriteNull("next_timer");
            }
            else
            {
                writer.WriteStartObject("next_timer");
                writer.WriteString("expected_schedule_revision", recommendation.NextTimer.ExpectedScheduleRevision);
                writer.WriteString("relative_delay", recommendation.NextTimer.RelativeDelay);
                writer.WriteEndObject();
            }

            writer.WriteString("produced_at", recommendation.ProducedAt.ToString("O"));
            if (recommendation is NoActionRecommendation noAction)
            {
                writer.WriteString("reason_category", noAction.ReasonCategory);
            }

            if (recommendation is EmitMessageRecommendation emitMessage)
            {
                if (emitMessage.ResponseSlotId is null)
                {
                    writer.WriteNull("response_slot_id");
                }
                else
                {
                    writer.WriteString("response_slot_id", emitMessage.ResponseSlotId);
                }

                if (emitMessage.TurnId is null)
                {
                    writer.WriteNull("turn_id");
                }
                else
                {
                    writer.WriteString("turn_id", emitMessage.TurnId);
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteEnvelope(Utf8JsonWriter writer, EnvelopeRecommendation envelope)
    {
        writer.WriteString("disposition", envelope.Disposition);
        if (envelope.NoActionReasonCategory is null)
        {
            writer.WriteNull("no_action_reason_category");
        }
        else
        {
            writer.WriteString("no_action_reason_category", envelope.NoActionReasonCategory);
        }

        WritePayloadRef(writer, envelope.PayloadRef);

        writer.WritePropertyName("outputs");
        writer.WriteStartArray();
        foreach (var output in envelope.Outputs)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", output.Kind);
            writer.WriteString("local_ref", output.LocalRef);
            WriteOptional(writer, "audience", output.ModelAudience);
            WriteOptional(writer, "communication_purpose", output.CommunicationPurpose);
            WriteOptional(writer, "model_agent_output_id", output.ModelAgentOutputId);
            WritePayloadRef(writer, output.PayloadRef);
            WriteReferences(writer, output.References);
            WriteOptional(writer, "response_slot_id", output.ResponseSlotId);
            WriteOptional(writer, "turn_id", output.TurnId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("requested_actions");
        writer.WriteStartArray();
        foreach (var action in envelope.RequestedActions)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", action.Kind);
            writer.WriteString("local_ref", action.LocalRef);
            WriteOptional(writer, "expected_schedule_revision", action.ExpectedScheduleRevision);
            WriteOptional(writer, "relative_delay", action.RelativeDelay);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WritePayloadRef(Utf8JsonWriter writer, ProtectedContentRef? payloadRef)
    {
        if (payloadRef is null)
        {
            writer.WriteNull("payload_ref");
            return;
        }

        writer.WritePropertyName("payload_ref");
        writer.WriteStartObject();
        writer.WriteString("content_digest", payloadRef.ContentDigest);
        writer.WriteString("protected_ref", payloadRef.ProtectedRef);
        writer.WriteEndObject();
    }

    private static void WriteReferences(Utf8JsonWriter writer, IReadOnlyList<OutputLocalReference>? references)
    {
        writer.WritePropertyName("references");
        writer.WriteStartArray();
        if (references is not null)
        {
            foreach (var reference in references)
            {
                writer.WriteStartObject();
                writer.WriteString("local_ref", reference.LocalRef);
                writer.WriteString("relation", reference.Relation);
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteOptional(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }
}
