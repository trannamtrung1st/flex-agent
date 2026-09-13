using System.Text;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceJsonPointerResolverTests
{
    [Fact]
    public void Traversal_through_scalar_reference_token_fails_without_throwing()
    {
        const string projectionJson = """{"score":42}""";

        Assert.False(Resolve(projectionJson, "/score/value", out _));
    }

    [Fact]
    public void Numeric_object_property_is_resolved_by_exact_name()
    {
        const string projectionJson = """{"0":"value"}""";

        Assert.True(Resolve(projectionJson, "/0", out var resolvedJson));
        Assert.Equal("\"value\"", resolvedJson);
    }

    [Fact]
    public void Invalid_array_index_fails()
    {
        const string projectionJson = """{"items":[1,2]}""";

        Assert.False(Resolve(projectionJson, "/items/99", out _));
        Assert.False(Resolve(projectionJson, "/items/-1", out _));
        Assert.False(Resolve(projectionJson, "/items/not-an-index", out _));
    }

    [Fact]
    public void Empty_reference_tokens_resolve_object_properties()
    {
        const string projectionJson = """{"":{"nested":7}}""";

        Assert.True(Resolve(projectionJson, "//nested", out var resolvedJson));
        Assert.Equal("7", resolvedJson);
    }

    [Fact]
    public void Root_empty_reference_token_discloses_only_the_empty_named_member()
    {
        const string projectionJson =
            """
            {
              "":"allowed",
              "sibling":"hidden"
            }
            """;

        Assert.True(Resolve(projectionJson, "/", out var resolvedJson));
        Assert.Equal("\"allowed\"", resolvedJson);
        Assert.DoesNotContain("hidden", resolvedJson, StringComparison.Ordinal);
        Assert.DoesNotContain("sibling", resolvedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_reference_token_on_array_fails()
    {
        const string projectionJson = """{"items":[1,2]}""";

        Assert.False(Resolve(projectionJson, "/items//0", out _));
    }

    [Fact]
    public void Traversal_through_scalar_at_root_fails()
    {
        const string projectionJson = "42";

        Assert.False(Resolve(projectionJson, "/value", out _));
    }

    private static bool Resolve(string projectionJson, string jsonPointer, out string? resolvedJson)
    {
        return EvidenceJsonPointerResolver.TryResolve(
            Encoding.UTF8.GetBytes(projectionJson),
            jsonPointer,
            out resolvedJson);
    }
}
