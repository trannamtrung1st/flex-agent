using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Transport;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class SyntheticSseConformanceTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(ContractsRoot);

    [Fact]
    public void Synthetic_adapter_fragment_and_complete_shapes_validate_against_sse_schema()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        const string schemaId = "https://flex-agent.local/contracts/schemas/v1/transport/sse-event.v1.schema.json";

        var fragment = new SseSessionEventV1(
            "v1",
            "session.agent.fragment.v1",
            "sess.synthetic.001",
            "14",
            "2026-08-10T00:00:02Z",
            new SseSessionEventPayloadV1(
                "Agent response fragment",
                1,
                "msg.synthetic.agent.001",
                "Thank you for your response. "));

        var complete = new SseSessionEventV1(
            "v1",
            "session.agent.complete.v1",
            "sess.synthetic.001",
            "15",
            "2026-08-10T00:00:03Z",
            new SseSessionEventPayloadV1(
                "Agent response complete",
                null,
                "msg.synthetic.agent.001",
                null,
                null,
                null,
                null,
                null,
                "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                1));

        foreach (var evt in new[] { fragment, complete })
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(evt, SerializerOptions);
            var result = _harness.ValidateInstance(schemas[schemaId], json);
            Assert.True(result.IsValid, $"{evt.EventType}: {JsonSerializer.Serialize(result)}");
        }
    }
}
