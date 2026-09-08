namespace FlexAgent.Evaluation.Domain;

public sealed record EvaluationAgentFragmentMaterial(
    int FragmentOrdinal,
    string ContentDigest,
    ReadOnlyMemory<byte> ExactUtf8);

public static class EvidenceAgentTranscriptAssembler
{
    public static EvaluationDecision<ReadOnlyMemory<byte>> TryAssembleExactUtf8(
        IReadOnlyList<EvaluationAgentFragmentMaterial> fragments,
        string expectedContentDigest)
    {
        if (!EvaluationIdentity.IsSha256Hex(expectedContentDigest)
            || fragments.Count is < 1 or > 256)
        {
            return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var ordered = fragments.OrderBy(fragment => fragment.FragmentOrdinal).ToArray();
        for (var index = 0; index < ordered.Length; index++)
        {
            if (ordered[index].FragmentOrdinal != index + 1)
            {
                return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "fragments");
            }

            if (!string.Equals(
                    EvidenceTextSourceNormalizer.DigestUtf8(ordered[index].ExactUtf8),
                    ordered[index].ContentDigest,
                    StringComparison.Ordinal))
            {
                return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "fragments");
            }
        }

        var totalLength = ordered.Sum(fragment => fragment.ExactUtf8.Length);
        var assembled = new byte[totalLength];
        var offset = 0;
        foreach (var fragment in ordered)
        {
            fragment.ExactUtf8.CopyTo(assembled.AsMemory(offset));
            offset += fragment.ExactUtf8.Length;
        }

        if (!string.Equals(
                EvidenceTextSourceNormalizer.DigestUtf8(assembled),
                expectedContentDigest,
                StringComparison.Ordinal))
        {
            return EvaluationDecision<ReadOnlyMemory<byte>>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "content_digest");
        }

        return EvaluationDecision<ReadOnlyMemory<byte>>.Ok(assembled);
    }
}
