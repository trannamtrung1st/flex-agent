using System.Security.Cryptography;

namespace FlexAgent.Evaluation.Domain;

public static class ProtectedModelResponseWireBytesDigest
{
    public const string DigestProcedureId = "evaluation-model-response-wire-content-digest-sha256-v1";

    private static readonly string ZeroDigest = new('0', 64);

    public static EvaluationDecision<string> TryVerify(
        ReadOnlySpan<byte> wireUtf8,
        string expectedContentDigest)
    {
        if (wireUtf8.IsEmpty
            || !EvaluationIdentity.IsSha256Hex(expectedContentDigest))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "response_ref");
        }

        var actualDigest = Compute(wireUtf8);
        if (!string.Equals(actualDigest, expectedContentDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "response_ref");
        }

        return EvaluationDecision<string>.Ok(actualDigest);
    }

    internal static string Compute(ReadOnlySpan<byte> wireUtf8)
    {
        var embeddedDigest = EvaluationModelResponseDocumentBinder.TryExtractContentDigest(wireUtf8);
        if (embeddedDigest is null)
        {
            throw new InvalidOperationException("Model response wire document is missing content_digest.");
        }

        var preimageUtf8 = string.Equals(embeddedDigest, ZeroDigest, StringComparison.Ordinal)
            ? wireUtf8.ToArray()
            : EvaluationModelResponseDocumentBinder.ReplaceContentDigestValue(
                wireUtf8,
                embeddedDigest,
                ZeroDigest);

        return Convert.ToHexString(SHA256.HashData(preimageUtf8)).ToLowerInvariant();
    }
}
