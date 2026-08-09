using System.Text.Json;

namespace FlexAgent.Contract.Tests.Harness;

internal static class SchemaKeywordProfile
{
    public const string Draft202012SchemaUri = "https://json-schema.org/draft/2020-12/schema";

    private static readonly HashSet<string> SchemaMapKeywords = new(StringComparer.Ordinal)
    {
        "properties",
        "patternProperties",
        "dependentSchemas",
        "$defs",
    };

    private static readonly HashSet<string> SchemaArrayKeywords = new(StringComparer.Ordinal)
    {
        "prefixItems",
        "allOf",
        "anyOf",
        "oneOf",
    };

    private static readonly HashSet<string> SchemaObjectKeywords = new(StringComparer.Ordinal)
    {
        "items",
        "contains",
        "additionalProperties",
        "propertyNames",
        "not",
        "if",
        "then",
        "else",
    };

    public static IReadOnlySet<string> LoadAllowedKeywords(string profilePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(profilePath));
        var keywords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in document.RootElement.GetProperty("allowedKeywords").EnumerateArray())
        {
            keywords.Add(element.GetString() ?? throw new InvalidOperationException("Invalid keyword profile."));
        }

        return keywords;
    }

    public static void AssertOnlyAllowedKeywords(JsonElement schema, IReadOnlySet<string> allowedKeywords)
    {
        if (schema.TryGetProperty("$schema", out var schemaKeyword)
            && schemaKeyword.ValueKind == JsonValueKind.String
            && schemaKeyword.GetString() != Draft202012SchemaUri)
        {
            throw new SchemaCompatibilityException(SchemaCompatibilityFailure.UnexpectedDialect);
        }

        WalkSchemaObject(schema, allowedKeywords);
    }

    private static void WalkSchemaObject(JsonElement schemaObject, IReadOnlySet<string> allowedKeywords)
    {
        if (schemaObject.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in schemaObject.EnumerateObject())
        {
            if (!allowedKeywords.Contains(property.Name))
            {
                throw new SchemaCompatibilityException(SchemaCompatibilityFailure.UnsupportedKeyword);
            }

            if (SchemaMapKeywords.Contains(property.Name))
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var nestedProperty in property.Value.EnumerateObject())
                    {
                        WalkSchemaObject(nestedProperty.Value, allowedKeywords);
                    }
                }

                continue;
            }

            if (SchemaArrayKeywords.Contains(property.Name))
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        WalkSchemaObject(item, allowedKeywords);
                    }
                }

                continue;
            }

            if (SchemaObjectKeywords.Contains(property.Name))
            {
                WalkSchemaObject(property.Value, allowedKeywords);
            }
        }
    }
}
