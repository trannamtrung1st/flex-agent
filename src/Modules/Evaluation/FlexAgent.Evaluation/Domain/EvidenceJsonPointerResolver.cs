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
        try
        {
            using var document = JsonDocument.Parse(projectionUtf8);
            var element = document.RootElement;
            if (jsonPointer == "/")
            {
                resolvedJson = element.GetRawText();
                return true;
            }

            foreach (var segment in jsonPointer.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var unescaped = segment.Replace("~1", "/", StringComparison.Ordinal)
                    .Replace("~0", "~", StringComparison.Ordinal);
                if (int.TryParse(unescaped, out var index))
                {
                    if (element.ValueKind != JsonValueKind.Array
                        || index < 0
                        || index >= element.GetArrayLength())
                    {
                        return false;
                    }

                    element = element[index];
                    continue;
                }

                if (!element.TryGetProperty(unescaped, out element))
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
}
