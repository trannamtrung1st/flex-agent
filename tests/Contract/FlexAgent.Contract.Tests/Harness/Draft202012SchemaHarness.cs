using System.Text.Json;
using Json.Schema;

namespace FlexAgent.Contract.Tests.Harness;

internal sealed class Draft202012SchemaHarness
{
    public const string RequiredDialectUri = SchemaKeywordProfile.Draft202012SchemaUri;

    private static readonly Dialect StrictDraft202012 = Dialect.Draft202012.With([], allowUnknownKeywords: false);

    private readonly IReadOnlySet<string> _allowedKeywords;

    public Draft202012SchemaHarness(IReadOnlySet<string> allowedKeywords)
    {
        _allowedKeywords = allowedKeywords;
    }

    public JsonSchema BuildSchema(ReadOnlySpan<byte> schemaUtf8)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(schemaUtf8.ToArray());
        }
        catch (JsonException)
        {
            throw new SchemaCompatibilityException(SchemaCompatibilityFailure.SchemaBuildFailed);
        }

        AssertDialect(document.RootElement);
        SchemaKeywordProfile.AssertOnlyAllowedKeywords(document.RootElement, _allowedKeywords);

        try
        {
            return JsonSchema.Build(
                document.RootElement,
                new BuildOptions { Dialect = StrictDraft202012 });
        }
        catch (Exception)
        {
            throw new SchemaCompatibilityException(SchemaCompatibilityFailure.SchemaBuildFailed);
        }
    }

    public EvaluationResults ValidateInstance(JsonSchema schema, ReadOnlySpan<byte> instanceUtf8)
    {
        JsonDocument instance;
        try
        {
            instance = JsonDocument.Parse(instanceUtf8.ToArray());
        }
        catch (JsonException)
        {
            throw new SchemaCompatibilityException(SchemaCompatibilityFailure.ValidationFailed);
        }

        using (instance)
        {
            return schema.Evaluate(instance.RootElement);
        }
    }

    private static void AssertDialect(JsonElement schema)
    {
        if (!schema.TryGetProperty("$schema", out var schemaKeyword)
            || schemaKeyword.ValueKind != JsonValueKind.String)
        {
            throw new SchemaCompatibilityException(SchemaCompatibilityFailure.MissingDialect);
        }

        if (schemaKeyword.GetString() != RequiredDialectUri)
        {
            throw new SchemaCompatibilityException(SchemaCompatibilityFailure.UnexpectedDialect);
        }
    }
}
