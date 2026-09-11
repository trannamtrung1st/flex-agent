using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Contracts.Evaluation;

public static class EvaluationModelResponseDocumentWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public static byte[] WriteCanonicalUtf8(EvaluationModelResponseV1 response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return JsonSerializer.SerializeToUtf8Bytes(response, SerializerOptions);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
        options.Converters.Add(new EvaluationModelResponseScoreJsonConverter());
        return options;
    }

    private sealed class EvaluationModelResponseScoreJsonConverter : JsonConverter<object?>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.Number when reader.TryGetInt32(out var integer) => integer,
                JsonTokenType.String => reader.GetString(),
                _ => throw new JsonException("Unsupported score token."),
            };

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case int integer:
                    writer.WriteNumberValue(integer);
                    break;
                case string text:
                    writer.WriteStringValue(text);
                    break;
                default:
                    throw new JsonException($"Unsupported score type '{value.GetType().Name}'.");
            }
        }
    }
}
