using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Org.Webpki.JsonCanonicalizer;

namespace FlexAgent.CanonicalJson;

/// <summary>
/// Application-owned boundary around the vendored RFC 8785 canonicalizer.
/// </summary>
public static class CanonicalJsonProcessor
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static byte[] CanonicalizeUtf8(ReadOnlySpan<byte> utf8Json, CanonicalJsonLimits limits)
    {
        ValidateInput(utf8Json, limits);
        try
        {
            var canonicalizer = new JsonCanonicalizer(utf8Json.ToArray());
            return canonicalizer.GetEncodedUTF8();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.UpstreamCanonicalizationFailed);
        }
    }

    public static string CanonicalizeSha256Hex(ReadOnlySpan<byte> utf8Json, CanonicalJsonLimits limits)
    {
        var canonicalBytes = CanonicalizeUtf8(utf8Json, limits);
        return Convert.ToHexString(SHA256.HashData(canonicalBytes)).ToLowerInvariant();
    }

    internal static void ValidateInput(ReadOnlySpan<byte> utf8Json, CanonicalJsonLimits limits)
    {
        if (utf8Json.Length > limits.MaxUtf8Bytes)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.InputTooLarge);
        }

        try
        {
            _ = StrictUtf8.GetString(utf8Json);
        }
        catch (DecoderFallbackException)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.InvalidUtf8);
        }

        if (!IsTopLevelObject(utf8Json))
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.TopLevelNotObject);
        }

        ValidateStructure(utf8Json, limits);
    }

    private static bool IsTopLevelObject(ReadOnlySpan<byte> utf8Json)
    {
        foreach (var b in utf8Json)
        {
            if (b is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r')
            {
                continue;
            }

            return b == (byte)'{';
        }

        return false;
    }

    private static void ValidateStructure(ReadOnlySpan<byte> utf8Json, CanonicalJsonLimits limits)
    {
        Utf8JsonReader reader;
        try
        {
            reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = limits.MaxNestingDepth,
            });
        }
        catch (JsonException)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
        }

        var objectPropertyNames = new Stack<HashSet<string>>();
        var objectPropertyCount = 0;
        var containerKinds = new Stack<bool>();
        var arrayElementCounts = new Stack<int>();

        while (true)
        {
            try
            {
                if (!reader.Read())
                {
                    break;
                }
            }
            catch (JsonException ex) when (ex.Message.Contains("maximum configured depth", StringComparison.Ordinal))
            {
                throw new CanonicalJsonException(CanonicalJsonFailureCategory.NestingDepthExceeded);
            }
            catch (JsonException)
            {
                throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    IncrementArrayElementCount(containerKinds, arrayElementCounts, limits);
                    if (containerKinds.Count + 1 > limits.MaxNestingDepth)
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.NestingDepthExceeded);
                    }

                    containerKinds.Push(false);
                    objectPropertyNames.Push(new HashSet<string>(StringComparer.Ordinal));
                    objectPropertyCount = 0;
                    break;

                case JsonTokenType.EndObject:
                    objectPropertyNames.Pop();
                    containerKinds.Pop();
                    break;

                case JsonTokenType.StartArray:
                    IncrementArrayElementCount(containerKinds, arrayElementCounts, limits);
                    if (containerKinds.Count + 1 > limits.MaxNestingDepth)
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.NestingDepthExceeded);
                    }

                    containerKinds.Push(true);
                    arrayElementCounts.Push(0);
                    break;

                case JsonTokenType.EndArray:
                    containerKinds.Pop();
                    arrayElementCounts.Pop();
                    break;

                case JsonTokenType.PropertyName:
                {
                    string propertyName;
                    try
                    {
                        propertyName = reader.GetString()
                            ?? throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.InvalidUnicode);
                    }

                    if (!ContainsOnlyValidUnicodeScalars(propertyName))
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.InvalidUnicode);
                    }

                    var names = objectPropertyNames.Peek();
                    if (!names.Add(propertyName))
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.DuplicateProperty);
                    }

                    objectPropertyCount++;
                    if (objectPropertyCount > limits.MaxObjectProperties)
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.PropertyCountExceeded);
                    }

                    break;
                }

                case JsonTokenType.String:
                {
                    string value;
                    try
                    {
                        value = reader.GetString()
                            ?? throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.InvalidUnicode);
                    }

                    if (!ContainsOnlyValidUnicodeScalars(value))
                    {
                        throw new CanonicalJsonException(CanonicalJsonFailureCategory.InvalidUnicode);
                    }

                    IncrementArrayElementCount(containerKinds, arrayElementCounts, limits);
                    break;
                }

                case JsonTokenType.Number:
                    ValidateNumberToken(reader);
                    IncrementArrayElementCount(containerKinds, arrayElementCounts, limits);
                    break;

                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    IncrementArrayElementCount(containerKinds, arrayElementCounts, limits);
                    break;

                case JsonTokenType.None:
                    break;

                default:
                    throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
            }
        }

        if (containerKinds.Count != 0)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
        }
    }

    private static void IncrementArrayElementCount(
        Stack<bool> containerKinds,
        Stack<int> arrayElementCounts,
        CanonicalJsonLimits limits)
    {
        if (containerKinds.Count == 0 || !containerKinds.Peek())
        {
            return;
        }

        var count = arrayElementCounts.Pop() + 1;
        if (count > limits.MaxArrayElements)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.ArrayLengthExceeded);
        }

        arrayElementCounts.Push(count);
    }

    private static void ValidateNumberToken(Utf8JsonReader reader)
    {
        if (!reader.TryGetDouble(out var value))
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.MalformedJson);
        }

        if (double.IsNegative(value) && value == 0d)
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.NegativeZero);
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new CanonicalJsonException(CanonicalJsonFailureCategory.NonFiniteNumber);
        }
    }

    private static bool ContainsOnlyValidUnicodeScalars(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsSurrogate(value[i]))
            {
                if (i + 1 >= value.Length || !char.IsSurrogatePair(value[i], value[i + 1]))
                {
                    return false;
                }

                i++;
            }
        }

        return true;
    }
}
