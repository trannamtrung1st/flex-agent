using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FlexAgent.Contracts.Session;

public static class SessionRuntimeContractJson
{
    private static readonly JsonSerializerOptions WireSerializerOptions = CreateWireSerializerOptions();

    public static byte[] SerializeToUtf8Bytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, WireSerializerOptions);

    public static byte[] SerializeToUtf8Bytes<T>(T value, Type inputType) =>
        JsonSerializer.SerializeToUtf8Bytes(value, inputType, WireSerializerOptions);

    private static JsonSerializerOptions CreateWireSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.MakeReadOnly();
        return options;
    }
}
