using System.Text.Json;
using Json.Schema;

namespace FlexAgent.Contract.Tests.Harness;

internal sealed class Draft202012SchemaHarness
{
    public const string RequiredDialectUri = SchemaKeywordProfile.Draft202012SchemaUri;

    private static readonly Dialect StrictDraft202012 = Dialect.Draft202012.With([], allowUnknownKeywords: false);

    private readonly IReadOnlySet<string> _allowedKeywords;
    private readonly SchemaRegistry _schemaRegistry;

    public Draft202012SchemaHarness(IReadOnlySet<string> allowedKeywords, string? contractsRoot = null, string? idNamespace = null)
    {
        _allowedKeywords = allowedKeywords;
        _schemaRegistry = new SchemaRegistry();
        if (contractsRoot is not null && idNamespace is not null)
        {
            _schemaRegistry.Fetch = (uri, registry) => FetchLocalSchema(contractsRoot, idNamespace, uri, registry, _allowedKeywords);
        }
    }

    public JsonSchema BuildSchema(ReadOnlySpan<byte> schemaUtf8, Uri? baseUri = null)
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
                document.RootElement.Clone(),
                new BuildOptions
                {
                    Dialect = StrictDraft202012,
                    SchemaRegistry = _schemaRegistry,
                });
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

    private static IBaseDocument? FetchLocalSchema(
        string contractsRoot,
        string idNamespace,
        Uri uri,
        SchemaRegistry registry,
        IReadOnlySet<string> allowedKeywords)
    {
        var schemaId = uri.GetLeftPart(UriPartial.Path);
        var path = ContractSchemaRegistry.ResolveSchemaPath(contractsRoot, schemaId, idNamespace);
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        AssertDialect(document.RootElement);
        SchemaKeywordProfile.AssertOnlyAllowedKeywords(document.RootElement, allowedKeywords);
        return JsonSchema.Build(
            document.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });
    }
}
