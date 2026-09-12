using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public static class ProtectedModelResponseContentDigest
{
    public static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 262_144,
        maxNestingDepth: 64,
        maxObjectProperties: 128,
        maxArrayElements: 128);

    public static EvaluationDecision<string> TryVerify(
        ReadOnlySpan<byte> wireUtf8,
        string expectedContentDigest)
    {
        if (!EvaluationIdentity.IsSha256Hex(expectedContentDigest))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "response_ref");
        }

        var computed = TryComputePayloadDigest(wireUtf8);
        if (computed is null)
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "model_response");
        }

        if (!string.Equals(computed, expectedContentDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "response_ref");
        }

        return EvaluationDecision<string>.Ok(computed);
    }

    internal static string? TryComputePayloadDigest(ReadOnlySpan<byte> wireUtf8)
    {
        try
        {
            using var document = JsonDocument.Parse(wireUtf8.ToArray());
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, "response_ref", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    property.Value.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            return CanonicalJsonProcessor.CanonicalizeSha256Hex(stream.ToArray(), Limits);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (CanonicalJsonException)
        {
            return null;
        }
    }
}
