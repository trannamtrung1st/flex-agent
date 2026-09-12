using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationModelResponseDocumentBinder
{
    private const string ContentDigestProperty = "\"content_digest\":\"";

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
        wireUtf8 = ReplaceContentDigestValue(wireUtf8, placeholderDigest, digest);

        var verifiedDigest = ProtectedModelResponseWireBytesDigest.Compute(wireUtf8);
        if (!string.Equals(verifiedDigest, digest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Model response wire-content digest did not bind.");
        }

        return (wireUtf8, new ProtectedPayloadRefV1(protectedRef, digest));
    }

    internal static string? TryExtractContentDigest(ReadOnlySpan<byte> wireUtf8)
    {
        var wireText = System.Text.Encoding.UTF8.GetString(wireUtf8);
        var markerIndex = wireText.LastIndexOf(ContentDigestProperty, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var valueStart = markerIndex + ContentDigestProperty.Length;
        if (valueStart + 64 > wireText.Length)
        {
            return null;
        }

        return wireText.Substring(valueStart, 64);
    }

    internal static byte[] ReplaceContentDigestValue(
        ReadOnlySpan<byte> wireUtf8,
        string currentDigest,
        string nextDigest)
    {
        if (currentDigest.Length != 64 || nextDigest.Length != 64)
        {
            throw new ArgumentException("Model response content digests must be 64-character SHA-256 hex values.");
        }

        var wireText = System.Text.Encoding.UTF8.GetString(wireUtf8);
        var markerIndex = wireText.LastIndexOf(ContentDigestProperty, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException("Model response wire document is missing content_digest.");
        }

        var valueStart = markerIndex + ContentDigestProperty.Length;
        if (valueStart + 64 > wireText.Length
            || !string.Equals(wireText.Substring(valueStart, 64), currentDigest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Model response wire document content_digest did not match.");
        }

        return System.Text.Encoding.UTF8.GetBytes(
            string.Concat(
                wireText.AsSpan(0, valueStart),
                nextDigest,
                wireText.AsSpan(valueStart + 64)));
    }
}
