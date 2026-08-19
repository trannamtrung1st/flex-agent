using System.Text;
using System.Text.Json;

namespace FlexAgent.Sessions.OpenRouter;

public static class OpenRouterTransportSchema
{
    public const string ResourceName = "FlexAgent.Sessions.OpenRouter.agent-decision.v2.openrouter.schema.json";

    public static JsonElement CloneSchemaElement()
    {
        using var document = JsonDocument.Parse(ReadUtf8());
        return document.RootElement.Clone();
    }

    public static byte[] ReadUtf8()
    {
        var assembly = typeof(OpenRouterTransportSchema).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing embedded OpenRouter schema '{ResourceName}'.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static string ReadUtf8Text() => Encoding.UTF8.GetString(ReadUtf8());
}
