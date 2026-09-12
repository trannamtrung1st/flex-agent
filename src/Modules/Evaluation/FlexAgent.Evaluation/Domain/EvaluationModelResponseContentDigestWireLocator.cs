using System.Text;
using System.Text.Json;

namespace FlexAgent.Evaluation.Domain;

internal static class EvaluationModelResponseContentDigestWireLocator
{
    internal readonly record struct DigestValueLocation(int ValueStartUtf8, int ValueLength);

    internal static bool TryLocate(
        ReadOnlySpan<byte> wireUtf8,
        out DigestValueLocation location,
        out string? digestValue)
    {
        location = default;
        digestValue = null;

        if (wireUtf8.IsEmpty)
        {
            return false;
        }

        try
        {
            var reader = new Utf8JsonReader(
                wireUtf8,
                new JsonReaderOptions
                {
                    CommentHandling = JsonCommentHandling.Disallow,
                    AllowTrailingCommas = false,
                });

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                if (!reader.ValueTextEquals("response_ref"u8))
                {
                    reader.Skip();
                    continue;
                }

                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    return false;
                }

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        continue;
                    }

                    if (!reader.ValueTextEquals("content_digest"u8))
                    {
                        reader.Skip();
                        continue;
                    }

                    if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                    {
                        return false;
                    }

                    if (reader.HasValueSequence || reader.ValueSpan.Length != 64)
                    {
                        return false;
                    }

                    digestValue = Encoding.UTF8.GetString(reader.ValueSpan);
                    if (!EvaluationIdentity.IsSha256Hex(digestValue))
                    {
                        return false;
                    }

                    location = new DigestValueLocation((int)reader.TokenStartIndex + 1, 64);
                    return true;
                }

                return false;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static bool TryBuildZeroFilledPreimage(ReadOnlySpan<byte> wireUtf8, out byte[] preimageUtf8)
    {
        preimageUtf8 = Array.Empty<byte>();
        if (!TryLocate(wireUtf8, out var location, out var digestValue)
            || digestValue is null)
        {
            return false;
        }

        preimageUtf8 = wireUtf8.ToArray();
        if (!string.Equals(digestValue, new string('0', 64), StringComparison.Ordinal))
        {
            preimageUtf8.AsSpan(location.ValueStartUtf8, location.ValueLength).Fill((byte)'0');
        }

        return true;
    }

    internal static bool TryReplaceDigestValue(
        ReadOnlySpan<byte> wireUtf8,
        string currentDigest,
        string nextDigest,
        out byte[] updatedWireUtf8)
    {
        updatedWireUtf8 = Array.Empty<byte>();
        if (currentDigest.Length != 64
            || nextDigest.Length != 64
            || !TryLocate(wireUtf8, out var location, out var embeddedDigest)
            || embeddedDigest is null
            || !string.Equals(embeddedDigest, currentDigest, StringComparison.Ordinal))
        {
            return false;
        }

        updatedWireUtf8 = wireUtf8.ToArray();
        if (!Encoding.UTF8.TryGetBytes(nextDigest, updatedWireUtf8.AsSpan(location.ValueStartUtf8, 64), out var written)
            || written != 64)
        {
            updatedWireUtf8 = Array.Empty<byte>();
            return false;
        }

        return true;
    }
}
