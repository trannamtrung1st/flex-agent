using System.Text.Json;

namespace FlexAgent.Contract.Tests.Harness;

internal sealed record ContractCatalog(
    string CatalogVersion,
    string SchemaDialect,
    string IdNamespace,
    IReadOnlyList<RepresentativeSchemaEntry> RepresentativeSchemas,
    IReadOnlyList<DigestSchemaEntry> DigestSchemas,
    ProjectionPaths Projections);

internal sealed record RepresentativeSchemaEntry(string Category, string SchemaId, string FixtureDir);

internal sealed record DigestSchemaEntry(string ProcedureId, string SchemaId, string FixtureDir);

internal sealed record ProjectionPaths(string OpenApi, string CsharpDtoRoot, string TypescriptRoot);

internal static class ContractCatalogLoader
{
    public static ContractCatalog Load(string contractsRoot)
    {
        var manifestPath = Path.Combine(contractsRoot, "catalog.manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var root = document.RootElement;

        var representative = root.GetProperty("representative_schemas").EnumerateArray()
            .Select(entry => new RepresentativeSchemaEntry(
                entry.GetProperty("category").GetString()!,
                entry.GetProperty("schema_id").GetString()!,
                entry.GetProperty("fixture_dir").GetString()!))
            .ToArray();

        var digest = root.GetProperty("digest_schemas").EnumerateArray()
            .Select(entry => new DigestSchemaEntry(
                entry.GetProperty("procedure_id").GetString()!,
                entry.GetProperty("schema_id").GetString()!,
                entry.GetProperty("fixture_dir").GetString()!))
            .ToArray();

        var projections = root.GetProperty("projections");
        return new ContractCatalog(
            root.GetProperty("catalog_version").GetString()!,
            root.GetProperty("schema_dialect").GetString()!,
            root.GetProperty("id_namespace").GetString()!,
            representative,
            digest,
            new ProjectionPaths(
                projections.GetProperty("openapi").GetString()!,
                projections.GetProperty("csharp_dto_root").GetString()!,
                projections.GetProperty("typescript_root").GetString()!));
    }
}
