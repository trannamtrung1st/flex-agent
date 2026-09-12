using System.Security.Cryptography;

namespace FlexAgent.Evaluation.Domain;

public static class ProtectedModelResponseWireBytesDigest
{
    public const string DigestProcedureId = "evaluation-model-response-wire-content-digest-sha256-v1";

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

        if (!TryCompute(wireUtf8, out var actualDigest))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "response_ref");
        }

        if (!string.Equals(actualDigest, expectedContentDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<string>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "response_ref");
        }

        return EvaluationDecision<string>.Ok(actualDigest);
    }

    internal static string Compute(ReadOnlySpan<byte> wireUtf8) =>
        TryCompute(wireUtf8, out var digest)
            ? digest
            : throw new InvalidOperationException("Model response wire-content digest could not be computed.");

    internal static bool TryCompute(ReadOnlySpan<byte> wireUtf8, out string digest)
    {
        digest = string.Empty;
        if (!EvaluationModelResponseContentDigestWireLocator.TryBuildZeroFilledPreimage(
                wireUtf8,
                out var preimageUtf8))
        {
            return false;
        }

        digest = Convert.ToHexString(SHA256.HashData(preimageUtf8)).ToLowerInvariant();
        return true;
    }
}
