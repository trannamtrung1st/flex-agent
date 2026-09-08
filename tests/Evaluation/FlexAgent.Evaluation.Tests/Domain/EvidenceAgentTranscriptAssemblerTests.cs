using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceAgentTranscriptAssemblerTests
{
    [Fact]
    public void Assembles_contiguous_fragments_and_verifies_content_digest()
    {
        var first = "Hello "u8.ToArray();
        var second = "world"u8.ToArray();
        var assembledText = "Hello world"u8.ToArray();
        var digest = EvidenceTextSourceNormalizer.DigestUtf8(assembledText);

        var result = EvidenceAgentTranscriptAssembler.TryAssembleExactUtf8(
        [
            new EvaluationAgentFragmentMaterial(
                1,
                EvidenceTextSourceNormalizer.DigestUtf8(first),
                first),
            new EvaluationAgentFragmentMaterial(
                2,
                EvidenceTextSourceNormalizer.DigestUtf8(second),
                second),
        ],
        digest);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(assembledText, result.Value!.ToArray());
    }

    [Fact]
    public void Non_contiguous_fragment_ordinals_fail()
    {
        var fragment = "Hello"u8.ToArray();

        var result = EvidenceAgentTranscriptAssembler.TryAssembleExactUtf8(
        [
            new EvaluationAgentFragmentMaterial(
                2,
                EvidenceTextSourceNormalizer.DigestUtf8(fragment),
                fragment),
        ],
        EvidenceTextSourceNormalizer.DigestUtf8(fragment));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("fragments", result.Field);
    }

    [Fact]
    public void Assembled_digest_mismatch_fails()
    {
        var fragment = "Hello"u8.ToArray();

        var result = EvidenceAgentTranscriptAssembler.TryAssembleExactUtf8(
        [
            new EvaluationAgentFragmentMaterial(
                1,
                EvidenceTextSourceNormalizer.DigestUtf8(fragment),
                fragment),
        ],
        new string('0', 64));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("content_digest", result.Field);
    }
}
