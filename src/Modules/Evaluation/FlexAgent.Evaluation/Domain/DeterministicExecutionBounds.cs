using System.Text.Json;
using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class DeterministicExecutionBounds
{
    public const int MaxJsonDepth = 32;

    public const int MaxJsonPropertyCount = 128;

    public const int MaxJsonArrayLength = 128;

    public const int MaxJsonStringUtf8Bytes = 65_536;

    public static bool TryValidateCanonicalInputSize(
        ReadOnlyMemory<byte> canonicalUtf8,
        int memoryLimitBytes,
        out string field)
    {
        field = "canonical_input";
        if (canonicalUtf8.Length > memoryLimitBytes)
        {
            return false;
        }

        return true;
    }

    public static bool TryValidateElapsedLimit(
        DateTimeOffset startedAt,
        string elapsedTimeLimit,
        out string field)
    {
        field = "elapsed_time_limit";
        if (!EvaluationPositiveDuration.TryParseTotalSeconds(elapsedTimeLimit, out var limitSeconds))
        {
            return false;
        }

        var elapsedSeconds = (DateTimeOffset.UtcNow - startedAt).TotalSeconds;
        return elapsedSeconds <= limitSeconds;
    }

    public static bool TryParseBoundedJson(
        ReadOnlyMemory<byte> canonicalUtf8,
        out JsonDocument? document,
        out string field)
    {
        document = null;
        field = "canonical_input";

        try
        {
            var reader = new Utf8JsonReader(canonicalUtf8.Span, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaxJsonDepth,
            });

            var depth = 0;
            var propertyCount = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        depth++;
                        if (depth > MaxJsonDepth)
                        {
                            field = "canonical_input";
                            return false;
                        }

                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        depth--;
                        break;
                    case JsonTokenType.PropertyName:
                        propertyCount++;
                        if (propertyCount > MaxJsonPropertyCount)
                        {
                            field = "canonical_input";
                            return false;
                        }

                        break;
                    case JsonTokenType.String:
                        if (reader.HasValueSequence || reader.ValueSpan.Length > MaxJsonStringUtf8Bytes)
                        {
                            field = "canonical_input";
                            return false;
                        }

                        break;
                }
            }

            document = JsonDocument.Parse(
                canonicalUtf8,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaxJsonDepth,
                });
            return ValidateStructuralLimits(document.RootElement, ref field);
        }
        catch (JsonException)
        {
            field = "canonical_input";
            return false;
        }
    }

    private static bool ValidateStructuralLimits(JsonElement element, ref string field)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var propertyCount = 0;
                foreach (var property in element.EnumerateObject())
                {
                    propertyCount++;
                    if (propertyCount > MaxJsonPropertyCount)
                    {
                        return false;
                    }

                    if (!ValidateStructuralLimits(property.Value, ref field))
                    {
                        return false;
                    }
                }

                return true;
            case JsonValueKind.Array:
                if (element.GetArrayLength() > MaxJsonArrayLength)
                {
                    return false;
                }

                foreach (var item in element.EnumerateArray())
                {
                    if (!ValidateStructuralLimits(item, ref field))
                    {
                        return false;
                    }
                }

                return true;
            case JsonValueKind.String:
                if (element.GetRawText().Length - 2 > MaxJsonStringUtf8Bytes)
                {
                    return false;
                }

                return true;
            default:
                return true;
        }
    }
}
