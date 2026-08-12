using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Session;

internal sealed class SessionRuntimeUnionJsonConverter<T> : JsonConverter<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"Deserializing {typeof(T).Name} through the union interface is not supported. Deserialize the concrete branch type.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
