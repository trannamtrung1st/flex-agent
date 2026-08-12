using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Session;

public static class SessionRuntimeContractJson
{
    public static JsonSerializerOptions WireSerializerOptions { get; } = CreateWireSerializerOptions();

    public static byte[] SerializeToUtf8Bytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, WireSerializerOptions);

    public static byte[] SerializeToUtf8Bytes<T>(T value, Type inputType) =>
        JsonSerializer.SerializeToUtf8Bytes(value, inputType, WireSerializerOptions);

    private static JsonSerializerOptions CreateWireSerializerOptions() =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
}
