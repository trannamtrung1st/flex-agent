using System.Security.Cryptography;
using System.Text;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationDeterministicFactProjectorTests
{
    [Fact]
    public void TryCreate_binds_deterministic_fact_source_identity_to_output_digest()
    {
        var attemptId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var outputUtf8 = """{"schema":"eval.output.word-count.v1","value":2,"within_range":true}"""u8.ToArray();
        var digest = Convert.ToHexString(SHA256.HashData(outputUtf8)).ToLowerInvariant();

        var result = EvaluationDeterministicFactProjector.TryCreate(attemptId, outputUtf8, digest);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal($"det.{attemptId:N}", result.Value!.SourceId);
        Assert.Equal($"rev.{digest}", result.Value.SourceVersion);
        Assert.Equal(digest, result.Value.ContentDigest);
    }

    [Fact]
    public void TryCreate_rejects_digest_mismatch()
    {
        var attemptId = Guid.CreateVersion7();
        var outputUtf8 = Encoding.UTF8.GetBytes("""{"value":1}""");

        var result = EvaluationDeterministicFactProjector.TryCreate(
            attemptId,
            outputUtf8,
            new string('a', 64));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("content_digest", result.Field);
    }
}
