using System.Security.Cryptography;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.CanonicalJson.Tests;

public sealed class ProvenanceTests
{
    private static string ProvenanceRoot =>
        Path.Combine(AppContext.BaseDirectory, "provenance");

    [Fact]
    public void Upstream_manifest_matches_vendored_file_hashes()
    {
        var manifestPath = Path.Combine(ProvenanceRoot, "upstream-manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var root = document.RootElement;
        var projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "src", "BuildingBlocks", "FlexAgent.CanonicalJson"));

        Assert.Equal("19d51d7fe467d4706a3ff08adf8a748f29fc21e0", root.GetProperty("upstreamCommit").GetString());

        foreach (var fileEntry in root.GetProperty("files").EnumerateObject())
        {
            var relativePath = fileEntry.Name;
            var expectedHash = fileEntry.Value.GetProperty("sha256").GetString();
            var filePath = Path.Combine(projectRoot, relativePath);
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(filePath))).ToLowerInvariant();
            Assert.Equal(expectedHash, actualHash);
        }
    }

    [Fact]
    public void Notice_documents_upstream_commit_and_copied_files()
    {
        var noticePath = Path.Combine(ProvenanceRoot, "NOTICE.md");
        var notice = File.ReadAllText(noticePath);
        Assert.Contains("19d51d7fe467d4706a3ff08adf8a748f29fc21e0", notice, StringComparison.Ordinal);
        Assert.Contains("JsonCanonicalizer.cs", notice, StringComparison.Ordinal);
        Assert.Contains("Apache License 2.0", notice, StringComparison.Ordinal);
    }
}
