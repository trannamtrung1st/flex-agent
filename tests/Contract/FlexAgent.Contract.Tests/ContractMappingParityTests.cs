using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Audit;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Contracts.Session;
using FlexAgent.Contracts.Transport;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class ContractMappingParityTests
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
    public void Exported_contract_dto_surface_matches_catalog_categories()
    {
        var exported = typeof(SessionCommandEnvelopeV1).Assembly
            .GetTypes()
            .Where(type => type is { IsPublic: true, IsAbstract: false })
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("SessionCommandEnvelopeV1", exported);
        Assert.Contains("SessionStateEventEnvelopeV1", exported);
        Assert.Contains("ResolvedExecutionManifestV1", exported);
        Assert.Contains("EvidenceLocatorV1", exported);
        Assert.Contains("AuditEventV1", exported);
        Assert.Contains("SafeErrorResponseV1", exported);
        Assert.Contains("SseSessionEventV1", exported);
        Assert.DoesNotContain(exported, name => name.Contains("Authorization", StringComparison.Ordinal));
        Assert.DoesNotContain(exported, name => name.Contains("Secret", StringComparison.Ordinal));
    }

    [Fact]
    public void Representative_dto_round_trip_validates_against_schema()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);

        var command = new SessionCommandEnvelopeV1(
            "v1",
            "session.message.send.v1",
            "cmd.synthetic.0001",
            "idem-synthetic-0001",
            new SessionLocatorV1("sess.synthetic.0001"),
            3,
            12,
            new MessageSendPayloadV1("Synthetic participant message for contract validation."));

        ValidateDto(schemas, "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json", command);

        var audit = new AuditEventV1(
            "audit-event.v1",
            "audit.synthetic.0001",
            new AuditActorV1("service", "session-resolver"),
            "org.synthetic.0001",
            "session.configuration.frozen",
            new AuditResourceRefV1("resolved_session_configuration", "rsc.synthetic.0001"),
            "succeeded",
            "freeze_complete",
            "2026-08-10T00:00:00Z",
            "corr.synthetic.0002",
            "required_durable");

        ValidateDto(schemas, "https://flex-agent.local/contracts/schemas/v1/audit/audit-event.v1.schema.json", audit);

        var error = new SafeErrorResponseV1("v1", "conflict", "corr.synthetic.0003", "reconcile", 4, 13);
        ValidateDto(schemas, "https://flex-agent.local/contracts/schemas/v1/transport/safe-error-response.v1.schema.json", error);
    }

    private void ValidateDto(IReadOnlyDictionary<string, Json.Schema.JsonSchema> schemas, string schemaId, object dto)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(dto, dto.GetType(), SerializerOptions);
        var result = _harness.ValidateInstance(schemas[schemaId], json);
        Assert.True(result.IsValid, $"{schemaId}: {JsonSerializer.Serialize(result)}");
    }
}
