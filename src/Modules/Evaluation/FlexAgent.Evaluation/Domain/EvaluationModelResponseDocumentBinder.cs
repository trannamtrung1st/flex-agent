using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationModelResponseDocumentBinder
{
    public static (byte[] WireUtf8, ProtectedPayloadRefV1 ResponseRef) Bind(EvaluationModelResponseV1 response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var provisionalUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(response);
        var digest = ProtectedModelResponseContentDigest.TryComputePayloadDigest(provisionalUtf8)
            ?? throw new InvalidOperationException("Model response payload digest could not be computed.");
        var boundRef = new ProtectedPayloadRefV1(response.ResponseRef.ProtectedRef, digest);
        var boundResponse = response with { ResponseRef = boundRef };
        var wireUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(boundResponse);
        return (wireUtf8, boundRef);
    }
}
