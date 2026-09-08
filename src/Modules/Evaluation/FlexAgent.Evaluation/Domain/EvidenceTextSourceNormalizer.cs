using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceTextSourceNormalizer
{
    public static string DigestUtf8(ReadOnlySpan<byte> utf8) =>
        Convert.ToHexString(SHA256.HashData(utf8)).ToLowerInvariant();

    public static string DigestUtf8(ReadOnlyMemory<byte> utf8) => DigestUtf8(utf8.Span);

    public static EvaluationDecision<ReadOnlyMemory<byte>> TryExtractUtf8ByteRange(
        ReadOnlyMemory<byte> source,
        int startInclusive,
        int endExclusive,
        string expectedExcerptDigest)
    {
        if (startInclusive < 0
            || endExclusive <= startInclusive
            || endExclusive > source.Length
            || !EvaluationIdentity.IsSha256Hex(expectedExcerptDigest))
        {
            return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "location");
        }

        var excerpt = source[startInclusive..endExclusive];
        if (!string.Equals(DigestUtf8(excerpt.Span), expectedExcerptDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "location.excerpt_digest");
        }

        return EvaluationDecision<ReadOnlyMemory<byte>>.Ok(excerpt);
    }

    public static EvaluationDecision<ReadOnlyMemory<byte>> TryExtractLineRange(
        ReadOnlyMemory<byte> source,
        int startLineInclusive,
        int endLineInclusive,
        string lineSplitProcedureVersion)
    {
        if (startLineInclusive < 1
            || endLineInclusive < startLineInclusive
            || !string.Equals(
                lineSplitProcedureVersion,
                EvaluationEvidenceSourceIdentity.LineSplitProcedureVersion,
                StringComparison.Ordinal))
        {
            return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                EvaluationFailureCodes.InvalidField,
                "location.line_split_procedure_version");
        }

        var text = StrictUtf8.GetString(source.Span);
        var lines = SplitLines(text);
        if (endLineInclusive > lines.Count)
        {
            return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "location.end_line_inclusive");
        }

        var selected = string.Join('\n', lines.Skip(startLineInclusive - 1).Take(endLineInclusive - startLineInclusive + 1));
        return EvaluationDecision<ReadOnlyMemory<byte>>.Ok(Encoding.UTF8.GetBytes(selected));
    }

    internal static IReadOnlyList<string> SplitLines(string text)
    {
        if (text.Length == 0)
        {
            return [string.Empty];
        }

        var lines = new List<string>();
        var start = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n')
            {
                continue;
            }

            lines.Add(text[start..index]);
            start = index + 1;
        }

        lines.Add(text[start..]);
        return lines;
    }

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
}
