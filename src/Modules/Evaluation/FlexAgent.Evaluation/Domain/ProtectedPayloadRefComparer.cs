using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Evaluation.Domain;

public static class ProtectedPayloadRefComparer
{
    public static bool Matches(ProtectedPayloadRefV1 left, ProtectedPayloadRefV1 right) =>
        string.Equals(left.ProtectedRef, right.ProtectedRef, StringComparison.Ordinal)
        && string.Equals(left.ContentDigest, right.ContentDigest, StringComparison.Ordinal);
}
