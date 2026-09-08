using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceTextSourceNormalizerTests
{
    [Fact]
    public void Byte_range_extracts_and_validates_excerpt_digest()
    {
        var source = "alpha\nbeta\ngamma"u8.ToArray();
        var excerpt = source[6..11];
        var digest = EvidenceTextSourceNormalizer.DigestUtf8(excerpt);

        var result = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(source, 6, 11, digest);

        Assert.True(result.Succeeded);
        Assert.Equal("beta\n"u8.ToArray(), result.Value.ToArray());
    }

    [Fact]
    public void Off_by_one_byte_range_fails_excerpt_digest()
    {
        var source = "abcdef"u8.ToArray();
        var wrongDigest = EvidenceTextSourceNormalizer.DigestUtf8("abcd"u8.ToArray());

        var result = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(source, 0, 3, wrongDigest);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
    }

    [Fact]
    public void Line_range_uses_line_split_v1()
    {
        var source = "one\ntwo\nthree"u8.ToArray();

        var result = EvidenceTextSourceNormalizer.TryExtractLineRange(
            source,
            startLineInclusive: 2,
            endLineInclusive: 3,
            EvaluationEvidenceSourceIdentity.LineSplitProcedureVersion);

        Assert.True(result.Succeeded);
        Assert.Equal("two\nthree"u8.ToArray(), result.Value.ToArray());
    }

    [Fact]
    public void Byte_range_rejects_mid_two_byte_scalar()
    {
        var source = "aéb"u8.ToArray();

        var result = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(
            source,
            startInclusive: 2,
            endExclusive: 3,
            EvidenceTextSourceNormalizer.DigestUtf8(source[2..3]));

        Assert.False(result.Succeeded);
        Assert.Equal("location.utf8_boundary", result.Field);
    }

    [Fact]
    public void Byte_range_rejects_mid_three_byte_scalar()
    {
        var source = "a€b"u8.ToArray();

        var result = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(
            source,
            startInclusive: 2,
            endExclusive: 3,
            EvidenceTextSourceNormalizer.DigestUtf8(source[2..3]));

        Assert.False(result.Succeeded);
        Assert.Equal("location.utf8_boundary", result.Field);
    }

    [Fact]
    public void Byte_range_rejects_mid_four_byte_scalar()
    {
        var source = "a😀b"u8.ToArray();

        var result = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(
            source,
            startInclusive: 2,
            endExclusive: 3,
            EvidenceTextSourceNormalizer.DigestUtf8(source[2..3]));

        Assert.False(result.Succeeded);
        Assert.Equal("location.utf8_boundary", result.Field);
    }

    [Fact]
    public void Byte_range_accepts_whole_scalar_boundaries()
    {
        var source = "aéb"u8.ToArray();
        var excerpt = source[1..3];
        var digest = EvidenceTextSourceNormalizer.DigestUtf8(excerpt);

        var result = EvidenceTextSourceNormalizer.TryExtractUtf8ByteRange(source, 1, 3, digest);

        Assert.True(result.Succeeded);
        Assert.Equal(excerpt.ToArray(), result.Value.ToArray());
    }
}
