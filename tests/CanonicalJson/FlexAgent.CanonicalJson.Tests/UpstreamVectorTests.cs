using System.Text;
using FlexAgent.CanonicalJson;
using Org.Webpki.JsonCanonicalizer;

namespace FlexAgent.CanonicalJson.Tests;

public sealed class UpstreamVectorTests
{
    private static readonly CanonicalJsonLimits TestLimits = new(
        maxUtf8Bytes: 1_048_576,
        maxNestingDepth: 128,
        maxObjectProperties: 10_000,
        maxArrayElements: 10_000);

    public static IEnumerable<object[]> ObjectVectors()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "UpstreamVectors");
        foreach (var inputPath in Directory.GetFiles(Path.Combine(root, "input"), "*.json"))
        {
            var name = Path.GetFileName(inputPath);
            if (name == "arrays.json")
            {
                continue;
            }

            yield return [name];
        }
    }

    [Theory]
    [MemberData(nameof(ObjectVectors))]
    public void Official_upstream_object_vectors_match_expected_utf8(string vectorName)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "UpstreamVectors");
        var input = File.ReadAllBytes(Path.Combine(root, "input", vectorName));
        var expected = File.ReadAllBytes(Path.Combine(root, "output", vectorName));

        var actual = CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits);

        Assert.Equal(Encoding.UTF8.GetString(expected), Encoding.UTF8.GetString(actual));
    }

    [Fact]
    public void Upstream_reference_implementation_matches_expected_for_object_vectors()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "UpstreamVectors");
        foreach (var inputPath in Directory.GetFiles(Path.Combine(root, "input"), "*.json"))
        {
            if (Path.GetFileName(inputPath) == "arrays.json")
            {
                continue;
            }

            var input = File.ReadAllText(inputPath);
            var expected = File.ReadAllBytes(inputPath.Replace("/input/", "/output/"));
            var actual = new JsonCanonicalizer(input).GetEncodedUTF8();
            Assert.Equal(Encoding.UTF8.GetString(expected), Encoding.UTF8.GetString(actual));
        }
    }
}
