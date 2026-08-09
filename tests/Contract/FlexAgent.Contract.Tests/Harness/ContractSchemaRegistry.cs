using System.Text.Json;
using Json.Schema;

namespace FlexAgent.Contract.Tests.Harness;

internal static class ContractSchemaRegistry
{
    public static string ResolveSchemaPath(string contractsRoot, string schemaId, string idNamespace)
    {
        if (!schemaId.StartsWith(idNamespace, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Schema id is outside namespace: {schemaId}");
        }

        var relative = schemaId[idNamespace.Length..];
        if (relative.StartsWith("schemas/", StringComparison.Ordinal))
        {
            relative = relative["schemas/".Length..];
        }

        return Path.Combine(contractsRoot, "schemas", relative);
    }

    public static IReadOnlyDictionary<string, JsonSchema> BuildCatalogSchemas(
        string contractsRoot,
        ContractCatalog catalog,
        IReadOnlySet<string> allowedKeywords)
    {
        var schemaIds = catalog.RepresentativeSchemas.Select(entry => entry.SchemaId)
            .Concat(catalog.DigestSchemas.Select(entry => entry.SchemaId))
            .Append("https://flex-agent.local/contracts/schemas/v1/common/primitives.v1.schema.json")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var schemas = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        foreach (var schemaId in schemaIds)
        {
            var path = ResolveSchemaPath(contractsRoot, schemaId, catalog.IdNamespace);
            var harness = new Draft202012SchemaHarness(allowedKeywords, contractsRoot, catalog.IdNamespace);
            schemas[schemaId] = harness.BuildSchema(File.ReadAllBytes(path));
        }

        return schemas;
    }

    public static void AssertReferenceClosure(string contractsRoot, ContractCatalog catalog)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();

        void Enqueue(string schemaId)
        {
            if (visited.Add(schemaId))
            {
                pending.Enqueue(schemaId);
            }
        }

        foreach (var entry in catalog.RepresentativeSchemas)
        {
            Enqueue(entry.SchemaId);
        }

        foreach (var entry in catalog.DigestSchemas)
        {
            Enqueue(entry.SchemaId);
        }

        Enqueue("https://flex-agent.local/contracts/schemas/v1/common/primitives.v1.schema.json");

        while (pending.Count > 0)
        {
            var schemaId = pending.Dequeue();
            var path = ResolveSchemaPath(contractsRoot, schemaId, catalog.IdNamespace);
            Assert.True(File.Exists(path), $"Missing schema file for {schemaId}");

            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            CollectReferences(document.RootElement, catalog.IdNamespace, Enqueue);
        }
    }

    private static void CollectReferences(JsonElement element, string idNamespace, Action<string> enqueue)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == "$ref" && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var reference = property.Value.GetString()!;
                        if (reference.StartsWith(idNamespace, StringComparison.Ordinal)
                            && !reference.Contains('#', StringComparison.Ordinal))
                        {
                            enqueue(reference);
                        }
                    }
                    else
                    {
                        CollectReferences(property.Value, idNamespace, enqueue);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectReferences(item, idNamespace, enqueue);
                }

                break;
        }
    }
}
