using System.Security.Cryptography;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.CanonicalJson.Tests;

public sealed class ProvenanceTests
{
    private static string ProvenanceRoot =>
        Path.Combine(AppContext.BaseDirectory, "provenance");

    private static string RepositoryRoot =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", ".."));

    private static string CanonicalJsonProjectRoot =>
        Path.Combine(RepositoryRoot, "src", "BuildingBlocks", "FlexAgent.CanonicalJson");

    [Fact]
    public void Upstream_manifest_matches_recorded_inventory_and_hashes()
    {
        var manifestPath = Path.Combine(ProvenanceRoot, "upstream-manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var root = document.RootElement;

        Assert.Equal("19d51d7fe467d4706a3ff08adf8a748f29fc21e0", root.GetProperty("upstreamCommit").GetString());

        AssertManifestSection(
            root.GetProperty("pristineSources"),
            EnumerateProjectFiles("Upstream/Pristine"));
        AssertManifestSection(
            root.GetProperty("compiledSources"),
            EnumerateProjectFiles("Upstream/CyberphoneJsonCanonicalization")
                .Concat(["Upstream/LICENSE"])
                .OrderBy(path => path, StringComparer.Ordinal));
        AssertCompiledSourceMetadata(root.GetProperty("compiledSources"));
        AssertOfficialVectors(root.GetProperty("officialVectors"));
    }

    [Fact]
    public void Project_file_compiles_only_manifested_upstream_sources()
    {
        var projectPath = Path.Combine(CanonicalJsonProjectRoot, "FlexAgent.CanonicalJson.csproj");
        var projectXml = File.ReadAllText(projectPath);
        Assert.Contains("<Compile Remove=\"Upstream/**/*.cs\" />", projectXml, StringComparison.Ordinal);
        Assert.Contains(
            "<Compile Include=\"Upstream/CyberphoneJsonCanonicalization/**/*.cs\">",
            projectXml,
            StringComparison.Ordinal);

        var unexpectedUpstreamSources = Directory
            .EnumerateFiles(Path.Combine(CanonicalJsonProjectRoot, "Upstream"), "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(CanonicalJsonProjectRoot, path).Replace('\\', '/'))
            .Where(path => !path.StartsWith("Upstream/Pristine/", StringComparison.Ordinal)
                && !path.StartsWith("Upstream/CyberphoneJsonCanonicalization/", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(unexpectedUpstreamSources);
    }

    [Fact]
    public void Notice_documents_upstream_commit_licenses_and_local_modifications()
    {
        var noticePath = Path.Combine(ProvenanceRoot, "NOTICE.md");
        var notice = File.ReadAllText(noticePath);
        Assert.Contains("19d51d7fe467d4706a3ff08adf8a748f29fc21e0", notice, StringComparison.Ordinal);
        Assert.Contains("Upstream/Pristine/", notice, StringComparison.Ordinal);
        Assert.Contains("internal class", notice, StringComparison.Ordinal);
        Assert.Contains("BSD-3-Clause", notice, StringComparison.Ordinal);
        Assert.Contains("MPL-2.0", notice, StringComparison.Ordinal);
        Assert.Contains("Lucent permissive", notice, StringComparison.Ordinal);
        Assert.Contains("upstream-manifest.json", notice, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateProjectFiles(string relativeRoot)
    {
        var directory = Path.Combine(CanonicalJsonProjectRoot, relativeRoot.Replace('/', Path.DirectorySeparatorChar));
        return Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(CanonicalJsonProjectRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static void AssertManifestSection(JsonElement section, IEnumerable<string> expectedRelativePaths)
    {
        var manifestKeys = section.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var diskKeys = expectedRelativePaths.ToHashSet(StringComparer.Ordinal);
        Assert.True(
            manifestKeys.SetEquals(diskKeys),
            $"Manifest keys differ from disk. Missing from manifest: {string.Join(", ", diskKeys.Except(manifestKeys))}. Extra in manifest: {string.Join(", ", manifestKeys.Except(diskKeys))}.");

        foreach (var fileEntry in section.EnumerateObject())
        {
            var expectedHash = fileEntry.Value.GetProperty("sha256").GetString();
            var filePath = Path.Combine(CanonicalJsonProjectRoot, fileEntry.Name);
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(filePath))).ToLowerInvariant();
            Assert.Equal(expectedHash, actualHash);
        }
    }

    private static void AssertCompiledSourceMetadata(JsonElement compiledSources)
    {
        foreach (var fileEntry in compiledSources.EnumerateObject())
        {
            var entry = fileEntry.Value;
            Assert.True(entry.TryGetProperty("licenses", out var licenses));
            Assert.NotEmpty(licenses.EnumerateArray());

            var modifications = entry.GetProperty("localModifications").EnumerateArray()
                .Select(element => element.GetString()!)
                .ToArray();

            if (fileEntry.Name.EndsWith("/JsonCanonicalizer.cs", StringComparison.Ordinal))
            {
                Assert.Single(modifications);
                Assert.Contains("JsonCanonicalizer", modifications[0], StringComparison.Ordinal);
                Assert.DoesNotContain("NumberToJson", modifications[0], StringComparison.Ordinal);
            }
            else if (fileEntry.Name.EndsWith("/NumberToJson.cs", StringComparison.Ordinal))
            {
                Assert.Single(modifications);
                Assert.Contains("NumberToJson", modifications[0], StringComparison.Ordinal);
                Assert.DoesNotContain("JsonCanonicalizer", modifications[0], StringComparison.Ordinal);
            }
            else
            {
                Assert.Empty(modifications);
            }
        }
    }

    private static void AssertOfficialVectors(JsonElement vectors)
    {
        var fixtureRoot = Path.Combine(
            RepositoryRoot,
            "tests",
            "CanonicalJson",
            "FlexAgent.CanonicalJson.Tests",
            "Fixtures",
            "UpstreamVectors");

        var manifestNames = vectors.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray();
        var diskNames = Directory.GetFiles(Path.Combine(fixtureRoot, "input"), "*.json")
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(diskNames, manifestNames);

        foreach (var vector in vectors.EnumerateObject())
        {
            var entry = vector.Value;
            var inputPath = Path.Combine(RepositoryRoot, entry.GetProperty("localInputPath").GetString()!);
            var outputPath = Path.Combine(RepositoryRoot, entry.GetProperty("localOutputPath").GetString()!);

            Assert.Equal(
                entry.GetProperty("input").GetProperty("sha256").GetString(),
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputPath))).ToLowerInvariant());
            Assert.Equal(
                entry.GetProperty("output").GetProperty("sha256").GetString(),
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(outputPath))).ToLowerInvariant());
        }
    }
}
