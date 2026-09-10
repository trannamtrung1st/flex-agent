using System.Security.Cryptography;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationDeterministicFactProjector
{
    public static EvaluationDecision<EvaluationSafeFactProjection> TryCreate(
        Guid deterministicAttemptId,
        ReadOnlyMemory<byte> outputUtf8,
        string expectedContentDigest)
    {
        if (deterministicAttemptId == Guid.Empty
            || outputUtf8.IsEmpty
            || !EvaluationIdentity.IsSha256Hex(expectedContentDigest))
        {
            return EvaluationDecision<EvaluationSafeFactProjection>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var actualDigest = Convert.ToHexString(SHA256.HashData(outputUtf8.Span)).ToLowerInvariant();
        if (!string.Equals(actualDigest, expectedContentDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluationSafeFactProjection>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                "content_digest");
        }

        return EvaluationDecision<EvaluationSafeFactProjection>.Ok(
            new EvaluationSafeFactProjection(
                EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(deterministicAttemptId),
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(expectedContentDigest),
                expectedContentDigest,
                outputUtf8));
    }
}
