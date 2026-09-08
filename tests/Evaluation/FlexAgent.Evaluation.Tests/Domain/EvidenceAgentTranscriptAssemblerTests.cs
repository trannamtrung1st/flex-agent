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
                10,
                EvidenceTextSourceNormalizer.DigestUtf8(first),
                first),
            new EvaluationAgentFragmentMaterial(
                2,
                11,
                EvidenceTextSourceNormalizer.DigestUtf8(second),
                second),
        ],
        digest,
        terminalCutoffSequence: 42);

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
                10,
                EvidenceTextSourceNormalizer.DigestUtf8(fragment),
                fragment),
        ],
        EvidenceTextSourceNormalizer.DigestUtf8(fragment),
        terminalCutoffSequence: 42);

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
                10,
                EvidenceTextSourceNormalizer.DigestUtf8(fragment),
                fragment),
        ],
        new string('0', 64),
        terminalCutoffSequence: 42);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("content_digest", result.Field);
    }

    [Fact]
    public void Post_cutoff_fragment_sequence_is_rejected()
    {
        var first = "Hello "u8.ToArray();
        var second = "world"u8.ToArray();
        var digest = EvidenceTextSourceNormalizer.DigestUtf8("Hello world"u8.ToArray());

        var result = EvidenceAgentTranscriptAssembler.TryAssembleExactUtf8(
        [
            new EvaluationAgentFragmentMaterial(
                1,
                39,
                EvidenceTextSourceNormalizer.DigestUtf8(first),
                first),
            new EvaluationAgentFragmentMaterial(
                2,
                43,
                EvidenceTextSourceNormalizer.DigestUtf8(second),
                second),
        ],
        digest,
        terminalCutoffSequence: 42);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("fragments.session_sequence", result.Field);
    }

    [Fact]
    public void Fragment_sequence_at_terminal_cutoff_is_permitted()
    {
        var fragment = "exact cutoff"u8.ToArray();
        var digest = EvidenceTextSourceNormalizer.DigestUtf8(fragment);

        var result = EvidenceAgentTranscriptAssembler.TryAssembleExactUtf8(
        [
            new EvaluationAgentFragmentMaterial(
                1,
                42,
                digest,
                fragment),
        ],
        digest,
        terminalCutoffSequence: 42);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(fragment, result.Value!.ToArray());
    }
}
