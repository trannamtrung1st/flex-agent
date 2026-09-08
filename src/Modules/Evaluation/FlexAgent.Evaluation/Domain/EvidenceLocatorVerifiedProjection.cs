using System.Text.Json;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceLocatorVerifiedProjection
{
    public static JsonElement CreateWholeItemLocation(string itemId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("location_type", "whole_item");
            writer.WriteString("item_id", itemId);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    public static EvaluationDecision<JsonElement> TryBuildEffectiveLocator(
        JsonElement locator,
        JsonElement effectiveLocation,
        string verifiedPrecision,
        string verificationState)
    {
        if (string.IsNullOrWhiteSpace(verifiedPrecision)
            || string.IsNullOrWhiteSpace(verificationState))
        {
            return EvaluationDecision<JsonElement>.Fail(EvaluationFailureCodes.InvalidField);
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in locator.EnumerateObject())
            {
                if (property.NameEquals("location"))
                {
                    writer.WritePropertyName("location");
                    effectiveLocation.WriteTo(writer);
                    continue;
                }

                if (property.NameEquals("precision"))
                {
                    writer.WriteString("precision", verifiedPrecision);
                    continue;
                }

                if (property.NameEquals("integrity"))
                {
                    writer.WritePropertyName("integrity");
                    writer.WriteStartObject();
                    foreach (var integrityProperty in property.Value.EnumerateObject())
                    {
                        if (integrityProperty.NameEquals("verification_state"))
                        {
                            writer.WriteString("verification_state", verificationState);
                            continue;
                        }

                        integrityProperty.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return EvaluationDecision<JsonElement>.Ok(document.RootElement.Clone());
    }
}
