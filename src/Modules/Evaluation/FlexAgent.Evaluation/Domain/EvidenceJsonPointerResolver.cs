using System.Text.Json;

namespace FlexAgent.Evaluation.Domain;

public static class EvidenceJsonPointerResolver
{
    public static bool TryResolve(
        ReadOnlyMemory<byte> projectionUtf8,
        string jsonPointer,
        out string? resolvedJson)
    {
        resolvedJson = null;
        if (string.IsNullOrEmpty(jsonPointer) || jsonPointer[0] != '/')
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(projectionUtf8);
            var element = document.RootElement;
            foreach (var segment in jsonPointer[1..].Split('/'))
            {
                var unescaped = UnescapeReferenceToken(segment);
                if (!TryTraverse(ref element, unescaped))
                {
                    return false;
                }
            }

            resolvedJson = element.GetRawText();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryTraverse(ref JsonElement element, string referenceToken)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element.TryGetProperty(referenceToken, out element);
            case JsonValueKind.Array:
                if (!TryParseArrayIndex(referenceToken, out var index)
                    || index < 0
                    || index >= element.GetArrayLength())
                {
                    return false;
                }

                element = element[index];
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseArrayIndex(string referenceToken, out int index) =>
        int.TryParse(referenceToken, out index);

    private static string UnescapeReferenceToken(string segment) =>
        segment.Replace("~1", "/", StringComparison.Ordinal)
            .Replace("~0", "~", StringComparison.Ordinal);
}
