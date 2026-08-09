using System.Text.Json;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class SchemaKeywordProfileTests
{
    [Fact]
    public void Disallowed_structural_keyword_fails_even_when_recursed()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "$schema",
            "type",
            "required",
        };

        using var schema = JsonDocument.Parse(
            """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "type": "object",
              "properties": {
                "id": { "type": "string" }
              }
            }
            """);

        var ex = Assert.Throws<SchemaCompatibilityException>(() =>
            SchemaKeywordProfile.AssertOnlyAllowedKeywords(schema.RootElement, allowed));

        Assert.Equal(SchemaCompatibilityFailure.UnsupportedKeyword, ex.Failure);
    }
}
