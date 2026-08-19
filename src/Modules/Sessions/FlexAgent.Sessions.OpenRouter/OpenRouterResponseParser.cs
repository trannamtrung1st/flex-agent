using System.Text;
using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

internal sealed record OpenRouterTerminalFacts(
    string ReturnedModel,
    string SelectedProvider,
    int Attempt,
    int? InputTokens,
    int? OutputTokens,
    bool CacheHit);

internal static class OpenRouterResponseParser
{
    public static bool TryReadControlContent(JsonElement root, out string? content)
    {
        content = null;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() != 1)
        {
            return false;
        }

        var choice = choices[0];
        if (choice.TryGetProperty("message", out var message)
            && message.TryGetProperty("content", out var messageContent)
            && messageContent.ValueKind == JsonValueKind.String)
        {
            content = messageContent.GetString();
            return !string.IsNullOrEmpty(content);
        }

        return false;
    }

    public static bool TryReadDelta(JsonElement root, out string? delta)
    {
        delta = null;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            return false;
        }

        var choice = choices[0];
        if (!choice.TryGetProperty("delta", out var deltaElement)
            || !deltaElement.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        delta = content.GetString();
        return !string.IsNullOrEmpty(delta);
    }

    public static bool TryReadTerminalFacts(
        JsonElement root,
        string expectedModel,
        string expectedProvider,
        out OpenRouterTerminalFacts? facts,
        out string failureReason)
    {
        facts = null;
        failureReason = ExecutionFailureReasons.ProviderUnavailable;
        if (!root.TryGetProperty("openrouter_metadata", out var metadata)
            || metadata.ValueKind != JsonValueKind.Object)
        {
            failureReason = ExecutionFailureReasons.ProviderUnavailable;
            return false;
        }

        if (!root.TryGetProperty("model", out var modelElement)
            || modelElement.ValueKind != JsonValueKind.String
            || !string.Equals(modelElement.GetString(), expectedModel, StringComparison.Ordinal))
        {
            return false;
        }

        if (!metadata.TryGetProperty("attempt", out var attemptElement)
            || !attemptElement.TryGetInt32(out var attempt)
            || attempt != 1)
        {
            return false;
        }

        if (!TryReadSelectedProvider(metadata, expectedProvider, out var selected))
        {
            return false;
        }

        var cacheHit = IsCacheHit(root);
        if (cacheHit)
        {
            return false;
        }

        if (!root.TryGetProperty("usage", out var usage)
            || usage.ValueKind != JsonValueKind.Object
            || !usage.TryGetProperty("prompt_tokens", out var prompt)
            || !prompt.TryGetInt32(out var promptTokens)
            || !usage.TryGetProperty("completion_tokens", out var completion)
            || !completion.TryGetInt32(out var completionTokens))
        {
            return false;
        }

        facts = new OpenRouterTerminalFacts(expectedModel, selected, attempt, promptTokens, completionTokens, cacheHit);
        return true;
    }

    public static bool TryReadSelectedProvider(JsonElement root, out string selected)
    {
        selected = string.Empty;
        if (!root.TryGetProperty("openrouter_metadata", out var metadata)
            || metadata.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return TryReadSelectedProvider(metadata, expectedProvider: null, out selected);
    }

    private static bool TryReadSelectedProvider(JsonElement metadata, string? expectedProvider, out string selected)
    {
        selected = string.Empty;
        if (!metadata.TryGetProperty("endpoints", out var endpoints)
            || endpoints.ValueKind != JsonValueKind.Object
            || !endpoints.TryGetProperty("available", out var available)
            || available.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        string? found = null;
        foreach (var endpoint in available.EnumerateArray())
        {
            if (endpoint.ValueKind != JsonValueKind.Object
                || !endpoint.TryGetProperty("selected", out var selectedElement)
                || selectedElement.ValueKind != JsonValueKind.True)
            {
                continue;
            }

            if (!endpoint.TryGetProperty("provider", out var provider)
                || provider.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            if (found is not null)
            {
                return false;
            }

            found = provider.GetString();
        }

        if (string.IsNullOrWhiteSpace(found)
            || (expectedProvider is not null
                && !string.Equals(found, expectedProvider, StringComparison.Ordinal)))
        {
            return false;
        }

        selected = found;
        return true;
    }

    private static bool IsCacheHit(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (usage.TryGetProperty("prompt_tokens_details", out var details)
            && details.ValueKind == JsonValueKind.Object
            && details.TryGetProperty("cached_tokens", out var cached)
            && cached.TryGetInt32(out var cachedTokens))
        {
            return cachedTokens > 0;
        }

        return false;
    }
}

internal static class OpenRouterSseParser
{
    public static async IAsyncEnumerable<string> ReadDataPayloadsAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var builder = new StringBuilder();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                if (builder.Length > 0)
                {
                    yield return builder.ToString();
                }

                yield break;
            }

            if (line.StartsWith(':') || line.StartsWith("event:", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                var payload = line.Length == 5 ? string.Empty : line[5..].TrimStart();
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(payload);
                continue;
            }

            if (line.Length == 0 && builder.Length > 0)
            {
                var payload = builder.ToString();
                builder.Clear();
                yield return payload;
            }
        }
    }
}
