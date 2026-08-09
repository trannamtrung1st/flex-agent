using System.Text;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class Draft202012SchemaHarnessTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");
    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);

    [Fact]
    public void Smoke_schema_builds_with_explicit_draft_2020_12_dialect()
    {
        var schemaBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, "fixtures", "schema", "draft202012-smoke.schema.json"));
        var schema = _harness.BuildSchema(schemaBytes);
        var result = _harness.ValidateInstance(schema, """{"id":"abc"}"""u8.ToArray());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Missing_dialect_fails_closed()
    {
        var schemaBytes = Encoding.UTF8.GetBytes("""{"type":"object"}""");
        var ex = Assert.Throws<SchemaCompatibilityException>(() => _harness.BuildSchema(schemaBytes));
        Assert.Equal(SchemaCompatibilityFailure.MissingDialect, ex.Failure);
    }

    [Fact]
    public void Unexpected_dialect_fails_closed()
    {
        var schemaBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, "fixtures", "schema", "wrong-dialect.schema.json"));
        var ex = Assert.Throws<SchemaCompatibilityException>(() => _harness.BuildSchema(schemaBytes));
        Assert.Equal(SchemaCompatibilityFailure.UnexpectedDialect, ex.Failure);
    }

    [Fact]
    public void Unsupported_keyword_fails_closed()
    {
        var schemaBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, "fixtures", "schema", "unsupported-keyword.schema.json"));
        var ex = Assert.Throws<SchemaCompatibilityException>(() => _harness.BuildSchema(schemaBytes));
        Assert.Equal(SchemaCompatibilityFailure.UnsupportedKeyword, ex.Failure);
    }

    [Fact]
    public void Schema_compatibility_errors_do_not_echo_sensitive_marker()
    {
        var secretMarker = $"echo-guard-{Guid.NewGuid():N}";
        var schemaBytes = Encoding.UTF8.GetBytes(
            $$"""
            {
              "$schema":"https://json-schema.org/draft/2020-12/schema",
              "type":"object",
              "{{secretMarker}}": true
            }
            """);
        var ex = Assert.Throws<SchemaCompatibilityException>(() => _harness.BuildSchema(schemaBytes));
        Assert.DoesNotContain(secretMarker, ex.Message, StringComparison.Ordinal);
    }
}
