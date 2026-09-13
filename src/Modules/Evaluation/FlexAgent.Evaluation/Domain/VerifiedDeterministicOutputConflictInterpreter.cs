using System.Text.Json;
using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class VerifiedDeterministicOutputConflictInterpreter
{
    public static bool TryIndicatesAgentOverrideConflict(
        EvaluationProcedureCriterionV1 criterion,
        ReadOnlySpan<byte> projectionUtf8)
    {
        ArgumentNullException.ThrowIfNull(criterion);

        var outputSchemaId = criterion.DeterministicEvaluator?.OutputSchemaId;
        if (string.IsNullOrWhiteSpace(outputSchemaId))
        {
            return false;
        }

        return outputSchemaId switch
        {
            BuiltinEvaluatorOutputSchemaIds.SchemaValidateOutputV1 =>
                TryReadSchemaValidateValidBoolean(projectionUtf8) is false,
            _ => false,
        };
    }

    internal static bool? TryReadSchemaValidateValidBoolean(ReadOnlySpan<byte> projectionUtf8)
    {
        if (projectionUtf8.IsEmpty)
        {
            return null;
        }

        try
        {
            var reader = new Utf8JsonReader(projectionUtf8);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            bool? valid = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    return null;
                }

                if (reader.ValueTextEquals("valid"u8))
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    valid = reader.TokenType switch
                    {
                        JsonTokenType.True => true,
                        JsonTokenType.False => false,
                        _ => null,
                    };
                    continue;
                }

                reader.TrySkip();
            }

            return valid;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal static class BuiltinEvaluatorOutputSchemaIds
{
    internal const string SchemaValidateOutputV1 = "eval.builtin.schema-validate.output.v1";
}
