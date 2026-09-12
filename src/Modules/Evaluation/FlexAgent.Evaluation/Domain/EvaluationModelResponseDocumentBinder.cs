using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationModelResponseDocumentBinder
{
    public static (byte[] WireUtf8, ProtectedPayloadRefV1 ResponseRef) Bind(EvaluationModelResponseV1 response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var protectedRef = response.ResponseRef.ProtectedRef;
        var placeholderDigest = new string('0', 64);
        var wireUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(
            response with
            {
                ResponseRef = new ProtectedPayloadRefV1(protectedRef, placeholderDigest),
            });

        var digest = ProtectedModelResponseWireBytesDigest.Compute(wireUtf8);
        if (!EvaluationModelResponseContentDigestWireLocator.TryReplaceDigestValue(
                wireUtf8,
                placeholderDigest,
                digest,
                out wireUtf8))
        {
            throw new InvalidOperationException("Model response wire-content digest could not be bound.");
        }

        if (!ProtectedModelResponseWireBytesDigest.TryCompute(wireUtf8, out var verifiedDigest)
            || !string.Equals(verifiedDigest, digest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Model response wire-content digest did not bind.");
        }

        return (wireUtf8, new ProtectedPayloadRefV1(protectedRef, digest));
    }
}
