using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using Xunit;

namespace FlexAgent.Evaluation.Tests.Domain;

internal static class EvaluationProcedureTestFixtures
{
    private static readonly string ContractsRoot = FindContractsRoot();

    internal static EvaluationProcedureV1 LoadP0TextSynthetic()
    {
        var path = Path.Combine(
            ContractsRoot,
            "fixtures",
            "jcs",
            "evaluation-procedure-jcs-sha256-v1",
            "p0-text-synthetic",
            "fixture.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var canonical = JsonSerializer.SerializeToUtf8Bytes(document.RootElement.GetProperty("digest_document"));
        Assert.True(
            EvaluationProcedureDocumentParser.TryParse(canonical, out var procedure, out var failure),
            failure);
        return procedure!;
    }

    private static string FindContractsRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "contracts", "catalog.manifest.json");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate contracts root.");
    }
}
