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
}
