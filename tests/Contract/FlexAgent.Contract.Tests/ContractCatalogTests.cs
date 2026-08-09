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
        Assert.Equal(7, _catalog.RepresentativeSchemas.Count);
        Assert.Equal(4, _catalog.DigestSchemas.Count);
        Assert.All(_catalog.RepresentativeSchemas, entry =>
            Assert.StartsWith(_catalog.IdNamespace, entry.SchemaId, StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_schemas_have_reference_closure_and_explicit_dialect()
    {
        ContractSchemaRegistry.AssertReferenceClosure(ContractsRoot, _catalog);
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        Assert.Equal(12, schemas.Count);
    }

    [Theory]
    [InlineData("fixtures/schema/v1/session/command-envelope/valid-message-send.json", "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json")]
    [InlineData("fixtures/schema/v1/session/state-event-envelope/valid-message-accepted.json", "https://flex-agent.local/contracts/schemas/v1/session/state-event-envelope.v1.schema.json")]
    [InlineData("fixtures/schema/v1/manifest/resolved-execution-manifest/valid-active.json", "https://flex-agent.local/contracts/schemas/v1/manifest/resolved-execution-manifest.v1.schema.json")]
    [InlineData("fixtures/schema/v1/evidence/evidence-locator/valid-transcript-whole-item.json", "https://flex-agent.local/contracts/schemas/v1/evidence/evidence-locator.v1.schema.json")]
    [InlineData("fixtures/schema/v1/audit/audit-event/valid-freeze.json", "https://flex-agent.local/contracts/schemas/v1/audit/audit-event.v1.schema.json")]
    [InlineData("fixtures/schema/v1/transport/safe-error-response/valid-conflict.json", "https://flex-agent.local/contracts/schemas/v1/transport/safe-error-response.v1.schema.json")]
    [InlineData("fixtures/schema/v1/transport/sse-event/valid-fragment.json", "https://flex-agent.local/contracts/schemas/v1/transport/sse-event.v1.schema.json")]
    public void Representative_valid_fixtures_validate(string relativeFixturePath, string schemaId)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[schemaId];
        var instanceBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, relativeFixturePath));
        var result = _harness.ValidateInstance(schema, instanceBytes);
        Assert.True(result.IsValid, JsonSerializer.Serialize(result));
    }

    [Fact]
    public void Representative_invalid_fixture_rejects_unknown_fields()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas["https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json"];
        var instanceBytes = File.ReadAllBytes(Path.Combine(
            ContractsRoot,
            "fixtures/schema/v1/session/command-envelope/invalid-unknown-field.json"));
        var result = _harness.ValidateInstance(schema, instanceBytes);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void OpenAPI_projection_exists_and_excludes_internal_only_fields()
    {
        var openApiPath = Path.Combine(ContractsRoot, _catalog.Projections.OpenApi);
        var content = File.ReadAllText(openApiPath);
        Assert.Contains("openapi: 3.1.0", content, StringComparison.Ordinal);
        Assert.DoesNotContain("internal_authorization_evidence", content, StringComparison.Ordinal);
        Assert.DoesNotContain("organization_scope_proof", content, StringComparison.Ordinal);
    }
}
