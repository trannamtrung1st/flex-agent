using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace FlexAgent.Sessions.Application;

public readonly record struct HostedSessionCommandEnvelope(
    string CommandType,
    string CommandId,
    string IdempotencyKey,
    int ExpectedSessionVersion,
    string? MessageText,
    string? TerminateReasonCode);

public static class HostedSessionCommandEnvelopeValidator
{
    internal const string SchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json";

    private const string PrimitivesSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/common/primitives.v1.schema.json";

    private const string EnvelopeResourceName = "FlexAgent.Sessions.Contracts.command-envelope.v1.schema.json";
    private const string PrimitivesResourceName = "FlexAgent.Sessions.Contracts.primitives.v1.schema.json";
    private const string HttpLocatorPlaceholder = "sess.http.locator";

    private static readonly Dialect StrictDraft202012 = Dialect.Draft202012.With([], allowUnknownKeywords: false);
    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    public static bool IsCanonicalSchemaValid(ReadOnlySpan<byte> utf8Json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json.ToArray());
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            return Evaluate(document.RootElement);
        }
    }

    public static bool TryRead(JsonElement root, Guid routeSessionId, out HostedSessionCommandEnvelope envelope)
    {
        envelope = default;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("session_locator", out var locator)
            || !locator.TryGetProperty("session_id", out var locatorId)
            || locatorId.GetString() is not { } locatorSession
            || !Guid.TryParse(locatorSession, out var bodySession)
            || bodySession != routeSessionId)
        {
            return false;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(root.GetRawText());
        }
        catch (JsonException)
        {
            return false;
        }

        if (node is not JsonObject)
        {
            return false;
        }

        if (node["session_locator"] is JsonObject locatorObject)
        {
            locatorObject["session_id"] = HttpLocatorPlaceholder;
        }

        using var normalized = JsonDocument.Parse(node.ToJsonString());
        if (!Evaluate(normalized.RootElement))
        {
            return false;
        }

        if (!root.TryGetProperty("command_type", out var typeEl)
            || typeEl.GetString() is not { } commandType
            || !root.TryGetProperty("command_id", out var idEl)
            || idEl.GetString() is not { } commandId
            || !root.TryGetProperty("idempotency_key", out var idemEl)
            || idemEl.GetString() is not { } idempotency
            || !root.TryGetProperty("expected_session_version", out var versionEl)
            || !versionEl.TryGetInt32(out var expectedVersion))
        {
            return false;
        }

        string? messageText = null;
        string? reason = null;
        if (commandType == "session.message.send.v1"
            && root.TryGetProperty("payload", out var sendPayload)
            && sendPayload.TryGetProperty("message_text", out var text))
        {
            messageText = text.GetString();
        }
        else if (commandType == "session.terminate.v1"
            && root.TryGetProperty("payload", out var terminatePayload)
            && terminatePayload.TryGetProperty("reason_code", out var reasonEl))
        {
            reason = reasonEl.GetString();
        }

        envelope = new HostedSessionCommandEnvelope(
            commandType,
            commandId,
            idempotency,
            expectedVersion,
            messageText,
            reason);
        return true;
    }

    private static bool Evaluate(JsonElement root)
    {
        var result = Schema.Value.Evaluate(
            root,
            new EvaluationOptions { RequireFormatValidation = true });
        return result.IsValid;
    }

    private static JsonSchema LoadSchema()
    {
        var registry = new SchemaRegistry { Fetch = FetchEmbedded };
        using var primitivesDocument = JsonDocument.Parse(ReadEmbedded(PrimitivesResourceName));
        JsonSchema.Build(
            primitivesDocument.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });

        using var envelopeDocument = JsonDocument.Parse(ReadEmbedded(EnvelopeResourceName));
        return JsonSchema.Build(
            envelopeDocument.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });
    }

    private static IBaseDocument FetchEmbedded(Uri uri, SchemaRegistry registry)
    {
        var schemaId = uri.GetLeftPart(UriPartial.Path);
        var resourceName = schemaId switch
        {
            SchemaId => EnvelopeResourceName,
            PrimitivesSchemaId => PrimitivesResourceName,
            _ => throw new InvalidOperationException($"Unexpected schema fetch: {uri}"),
        };

        using var document = JsonDocument.Parse(ReadEmbedded(resourceName));
        return JsonSchema.Build(
            document.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });
    }

    private static byte[] ReadEmbedded(string resourceName)
    {
        var assembly = typeof(HostedSessionCommandEnvelopeValidator).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded schema resource '{resourceName}'. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
