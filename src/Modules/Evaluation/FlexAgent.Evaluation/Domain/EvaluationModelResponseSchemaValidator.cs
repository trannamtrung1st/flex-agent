using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using Json.Schema;

namespace FlexAgent.Evaluation.Domain;

internal static class EvaluationModelResponseSchemaValidator
{
    internal const string SchemaId =
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-model-response.v1.schema.json";

    private const string PrimitivesSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/common/primitives.v1.schema.json";

    private const string ResponseResourceName =
        "FlexAgent.Contracts.evaluation-model-response.v1.schema.json";

    private const string PrimitivesResourceName =
        "FlexAgent.Contracts.primitives.v1.schema.json";

    private static readonly Dialect StrictDraft202012 = Dialect.Draft202012.With([], allowUnknownKeywords: false);
    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    internal static bool IsSchemaValid(ReadOnlySpan<byte> utf8Json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json.ToArray());
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            var result = Schema.Value.Evaluate(
                document.RootElement,
                new EvaluationOptions { RequireFormatValidation = true });
            return result.IsValid;
        }
    }

    private static JsonSchema LoadSchema()
    {
        var registry = new SchemaRegistry { Fetch = FetchEmbedded };
        using var primitivesDocument = JsonDocument.Parse(ReadEmbedded(PrimitivesResourceName));
        JsonSchema.Build(
            primitivesDocument.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });

        using var responseDocument = JsonDocument.Parse(ReadEmbedded(ResponseResourceName));
        return JsonSchema.Build(
            responseDocument.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });
    }

    private static IBaseDocument FetchEmbedded(Uri uri, SchemaRegistry registry)
    {
        var schemaId = uri.GetLeftPart(UriPartial.Path);
        var resourceName = schemaId switch
        {
            SchemaId => ResponseResourceName,
            PrimitivesSchemaId => PrimitivesResourceName,
            _ => throw new InvalidOperationException($"Unexpected schema fetch: {uri}"),
        };

        using var document = JsonDocument.Parse(ReadEmbedded(resourceName));
        return JsonSchema.Build(
            document.RootElement.Clone(),
            new BuildOptions
            {
                Dialect = StrictDraft202012,
                SchemaRegistry = registry,
            });
    }

    private static byte[] ReadEmbedded(string resourceName)
    {
        var assembly = typeof(EvaluationModelResponseV1).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded schema resource '{resourceName}'. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
