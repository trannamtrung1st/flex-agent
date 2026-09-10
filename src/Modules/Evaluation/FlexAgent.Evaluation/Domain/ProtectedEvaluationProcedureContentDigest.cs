using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public static class ProtectedEvaluationProcedureContentDigest
{
    public static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    public static EvaluationDecision<string> TryVerify(
        ReadOnlyMemory<byte> canonicalUtf8,
        string expectedContentDigest)
    {
        if (!EvaluationIdentity.IsSha256Hex(expectedContentDigest))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "procedure");
        }

        try
        {
            var recomputed = CanonicalJsonProcessor.CanonicalizeSha256Hex(canonicalUtf8.Span, Limits);
            if (!string.Equals(recomputed, expectedContentDigest, StringComparison.Ordinal))
            {
                return EvaluationDecision<string>.Fail(
                    EvaluationFailureCodes.CitationIntegrity,
                    "procedure_content_digest");
            }

            return EvaluationDecision<string>.Ok(recomputed);
        }
        catch (CanonicalJsonException)
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "procedure");
        }
    }
}
