using System.Text.Json;
using FlexAgent.CanonicalJson;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class JcsFixtureConformanceTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");

    public static IEnumerable<object[]> SuccessFixtures()
    {
        foreach (var procedureDir in Directory.EnumerateDirectories(Path.Combine(ContractsRoot, "fixtures", "jcs")))
        {
            foreach (var caseDir in Directory.EnumerateDirectories(procedureDir))
            {
                var fixturePath = Path.Combine(caseDir, "fixture.json");
                if (!File.Exists(fixturePath))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(File.ReadAllBytes(fixturePath));
                if (document.RootElement.GetProperty("outcome").GetString() == "success")
                {
                    yield return [fixturePath];
                }
            }
        }
    }

    public static IEnumerable<object[]> FailureFixtures()
    {
        foreach (var procedureDir in Directory.EnumerateDirectories(Path.Combine(ContractsRoot, "fixtures", "jcs")))
        {
            foreach (var caseDir in Directory.EnumerateDirectories(procedureDir))
            {
                var fixturePath = Path.Combine(caseDir, "fixture.json");
                if (!File.Exists(fixturePath))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(File.ReadAllBytes(fixturePath));
                if (document.RootElement.GetProperty("outcome").GetString() == "failure")
                {
                    yield return [fixturePath];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(SuccessFixtures))]
    public void Success_fixture_matches_expected_canonical_bytes_and_sha256(string fixturePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(fixturePath));
        var root = document.RootElement;
        var limits = ReadLimits(root.GetProperty("limits"));
        var digestDocument = JsonSerializer.Serialize(root.GetProperty("digest_document"));
        var digestUtf8 = JsonSerializer.SerializeToUtf8Bytes(root.GetProperty("digest_document"));

        var canonicalBytes = CanonicalJsonProcessor.CanonicalizeUtf8(digestUtf8, limits);
        var sha256 = CanonicalJsonProcessor.CanonicalizeSha256Hex(digestUtf8, limits);

        var expectedCanonicalHex = root.GetProperty("expected_canonical_utf8_hex").GetString()!;
        var expectedSha256 = root.GetProperty("expected_sha256_hex").GetString()!;

        Assert.Equal(expectedCanonicalHex, Convert.ToHexString(canonicalBytes).ToLowerInvariant());
        Assert.Equal(expectedSha256, sha256);
        Assert.NotEqual(digestDocument, System.Text.Encoding.UTF8.GetString(canonicalBytes));
    }

    [Theory]
    [MemberData(nameof(FailureFixtures))]
    public void Failure_fixture_fails_closed(string fixturePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(fixturePath));
        var root = document.RootElement;
        var limits = ReadLimits(root.GetProperty("limits"));

        byte[] inputUtf8;
        if (root.TryGetProperty("raw_digest_document_utf8_hex", out var rawHex))
        {
            inputUtf8 = Convert.FromHexString(rawHex.GetString()!);
        }
        else
        {
            inputUtf8 = JsonSerializer.SerializeToUtf8Bytes(root.GetProperty("digest_document"));
        }

        Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(inputUtf8, limits));
    }

    private static CanonicalJsonLimits ReadLimits(JsonElement limitsElement) =>
        new(
            limitsElement.GetProperty("maxUtf8Bytes").GetInt32(),
            limitsElement.GetProperty("maxNestingDepth").GetInt32(),
            limitsElement.GetProperty("maxPropertyCount").GetInt32(),
            limitsElement.GetProperty("maxArrayLength").GetInt32());
}
