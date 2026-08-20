using System.Text.Json;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

internal static class OpenRouterRequestFactory
{
    public static HttpRequestMessage CreateControl(
        InstalledModelDeploymentProfile profile,
        OpenRouterInstalledConfiguration configuration,
        string invocationId,
        InvocationContext? context,
        string secret)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", profile.ResolvedModelVersion);
            writer.WriteNumber("max_tokens", profile.MaxOutputTokens);
            writer.WriteBoolean("stream", false);
            WriteMessages(writer, invocationId, context, control: true);
            WriteProvider(writer, configuration.ProviderSlug);
            WriteReasoning(writer, configuration.RequestPolicy);
            writer.WritePropertyName("response_format");
            writer.WriteStartObject();
            writer.WriteString("type", "json_schema");
            writer.WritePropertyName("json_schema");
            writer.WriteStartObject();
            writer.WriteString("name", "agent_decision_v2");
            writer.WriteBoolean("strict", true);
            writer.WritePropertyName("schema");
            OpenRouterTransportSchema.CloneSchemaElement().WriteTo(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return CreateMessage(stream.ToArray(), secret);
    }

    public static HttpRequestMessage CreateContent(
        InstalledModelDeploymentProfile profile,
        OpenRouterInstalledConfiguration configuration,
        string invocationId,
        InvocationContext? context,
        string secret)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", profile.ResolvedModelVersion);
            writer.WriteNumber("max_tokens", profile.MaxOutputTokens);
            writer.WriteBoolean("stream", true);
            WriteMessages(writer, invocationId, context, control: false);
            WriteProvider(writer, configuration.ProviderSlug);
            WriteReasoning(writer, configuration.RequestPolicy);
            writer.WriteEndObject();
        }

        return CreateMessage(stream.ToArray(), secret);
    }

    public static HttpRequestMessage CreateDiscovery(string secret)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", OpenRouterAdapterContracts.DiscoveryModel);
            writer.WriteNumber("max_tokens", 16);
            writer.WriteBoolean("stream", false);
            writer.WritePropertyName("messages");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("role", "user");
            writer.WriteString("content", "synthetic.openrouter.discovery");
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("provider");
            writer.WriteStartObject();
            writer.WriteBoolean("allow_fallbacks", false);
            writer.WriteBoolean("require_parameters", true);
            writer.WriteString("data_collection", "allow");
            writer.WriteBoolean("zdr", false);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return CreateMessage(stream.ToArray(), secret);
    }

    private static HttpRequestMessage CreateMessage(byte[] utf8, string secret)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OpenRouterDestination.ChatCompletionsUri)
        {
            Content = new ByteArrayContent(utf8),
        };
        request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + secret);
        request.Headers.TryAddWithoutValidation("X-OpenRouter-Metadata", "enabled");
        request.Headers.TryAddWithoutValidation("X-OpenRouter-Cache", "false");
        return request;
    }

    private static void WriteMessages(
        Utf8JsonWriter writer,
        string invocationId,
        InvocationContext? context,
        bool control)
    {
        writer.WritePropertyName("messages");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WriteString("role", "system");
        writer.WriteString(
            "content",
            control
                ? OpenRouterAdapterContracts.ControlSystemPrompt(invocationId)
                : "Return only participant-visible message text.");
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WriteString("role", "user");
        writer.WriteString("content", ProviderSafeInvocationContextSerializer.Serialize(invocationId, context));
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void WriteProvider(Utf8JsonWriter writer, string providerSlug)
    {
        writer.WritePropertyName("provider");
        writer.WriteStartObject();
        writer.WritePropertyName("only");
        writer.WriteStartArray();
        writer.WriteStringValue(providerSlug);
        writer.WriteEndArray();
        writer.WriteBoolean("allow_fallbacks", false);
        writer.WriteBoolean("require_parameters", true);
        writer.WriteString("data_collection", "allow");
        writer.WriteBoolean("zdr", false);
        writer.WriteEndObject();
    }

    private static void WriteReasoning(Utf8JsonWriter writer, OpenRouterRequestPolicy policy)
    {
        if (policy.ReasoningEffort is null || !policy.ReasoningExcluded)
        {
            return;
        }

        writer.WritePropertyName("reasoning");
        writer.WriteStartObject();
        writer.WriteString("effort", policy.ReasoningEffort);
        writer.WriteBoolean("exclude", policy.ReasoningExcluded);
        writer.WriteEndObject();
    }
}
