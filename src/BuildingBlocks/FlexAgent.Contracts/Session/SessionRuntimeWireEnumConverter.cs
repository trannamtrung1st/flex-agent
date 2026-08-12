using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Session;

internal sealed class SessionRuntimeWireEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public SessionRuntimeWireEnumConverter()
        : base(allowIntegerValues: false)
    {
    }
}
