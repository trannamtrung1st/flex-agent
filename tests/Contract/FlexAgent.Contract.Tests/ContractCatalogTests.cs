using System.Text.Json;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class ContractCatalogTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");
    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "contracts"));

    [Fact]
    public void Catalog_declares_draft_2020_12_and_complete_representative_set()
    {
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", _catalog.SchemaDialect);
        Assert.Equal(13, _catalog.RepresentativeSchemas.Count);
        Assert.Equal(4, _catalog.DigestSchemas.Count);
        Assert.All(_catalog.RepresentativeSchemas, entry =>
            Assert.StartsWith(_catalog.IdNamespace, entry.SchemaId, StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_schemas_have_reference_closure_and_explicit_dialect()
    {
        ContractSchemaRegistry.AssertReferenceClosure(ContractsRoot, _catalog);
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        Assert.Equal(18, schemas.Count);
    }

    [Theory]
    [MemberData(nameof(ValidFixtureCases))]
    public void Representative_valid_fixtures_validate(string relativeFixturePath, string schemaId)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[schemaId];
        var instanceBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, relativeFixturePath));
        var result = _harness.ValidateInstance(schema, instanceBytes);
        Assert.True(result.IsValid, JsonSerializer.Serialize(result));
    }

    [Theory]
    [MemberData(nameof(InvalidFixtureCases))]
    public void Representative_invalid_fixtures_reject(string relativeFixturePath, string schemaId)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[schemaId];
        var instanceBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, relativeFixturePath));
        var result = _harness.ValidateInstance(schema, instanceBytes);
        Assert.False(result.IsValid, relativeFixturePath);
    }

    [Fact]
    public void OpenAPI_projection_exists_and_excludes_internal_only_fields()
    {
        var openApiPath = Path.Combine(ContractsRoot, _catalog.Projections.OpenApi);
        var content = File.ReadAllText(openApiPath);
        Assert.Contains("openapi: 3.1.0", content, StringComparison.Ordinal);
        Assert.DoesNotContain("internal_authorization_evidence", content, StringComparison.Ordinal);
        Assert.DoesNotContain("organization_scope_proof", content, StringComparison.Ordinal);
        Assert.Contains("SessionMessageSendCommandV1", content, StringComparison.Ordinal);
        Assert.Contains("EvidenceLocatorV1", content, StringComparison.Ordinal);
        Assert.Contains("PositiveInt64WireString", content, StringComparison.Ordinal);
    }

    public static TheoryData<string, string> ValidFixtureCases => DiscoverFixtures("valid-");

    public static TheoryData<string, string> InvalidFixtureCases => DiscoverFixtures("invalid-");

    private static TheoryData<string, string> DiscoverFixtures(string prefix)
    {
        var fixturesRoot = Path.Combine(ContractsRoot, "fixtures", "schema", "v1");
        var data = new TheoryData<string, string>();
        foreach (var file in Directory.EnumerateFiles(fixturesRoot, $"{prefix}*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(ContractsRoot, file).Replace('\\', '/');
            var schemaId = ResolveSchemaId(relative);
            data.Add(relative, schemaId);
        }

        return data;
    }

    private static string ResolveSchemaId(string relativeFixturePath)
    {
        var segments = relativeFixturePath.Split('/');
        var category = segments[^2];
        return category switch
        {
            "command-envelope" => "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json",
            "state-event-envelope" => "https://flex-agent.local/contracts/schemas/v1/session/state-event-envelope.v1.schema.json",
            "resolved-execution-manifest" => "https://flex-agent.local/contracts/schemas/v1/manifest/resolved-execution-manifest.v1.schema.json",
            "evidence-locator" => "https://flex-agent.local/contracts/schemas/v1/evidence/evidence-locator.v1.schema.json",
            "audit-event" => "https://flex-agent.local/contracts/schemas/v1/audit/audit-event.v1.schema.json",
            "safe-error-response" => "https://flex-agent.local/contracts/schemas/v1/transport/safe-error-response.v1.schema.json",
            "sse-event" => "https://flex-agent.local/contracts/schemas/v1/transport/sse-event.v1.schema.json",
            "trusted-trigger" => "https://flex-agent.local/contracts/schemas/v1/session/trusted-trigger.v1.schema.json",
            "agent-invocation" => "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation.v1.schema.json",
            "agent-invocation-execution-attempt" => "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-attempt.v1.schema.json",
            "agent-decision" => "https://flex-agent.local/contracts/schemas/v1/session/agent-decision.v1.schema.json",
            "decision-validation-effect" => "https://flex-agent.local/contracts/schemas/v1/session/decision-validation-effect.v1.schema.json",
            "timer-schedule-revision" => "https://flex-agent.local/contracts/schemas/v1/session/timer-schedule-revision.v1.schema.json",
            _ => throw new InvalidOperationException($"Unknown fixture category: {category}"),
        };
    }
}
