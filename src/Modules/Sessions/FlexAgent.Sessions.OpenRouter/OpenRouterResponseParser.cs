using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

internal sealed record OpenRouterTerminalFacts(
    string ReturnedModel,
    string SelectedProvider,
    int Attempt,
    int? InputTokens,
    int? OutputTokens);

internal sealed class OpenRouterTransportLimitExceededException : Exception;

internal static class OpenRouterResponseParser
{
    public static bool IsResponseCacheHit(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return HasHit(response.Headers) || HasHit(response.Content.Headers);
    }

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

        if (!root.TryGetProperty("usage", out var usage)
            || usage.ValueKind != JsonValueKind.Object
            || !usage.TryGetProperty("prompt_tokens", out var prompt)
            || !prompt.TryGetInt32(out var promptTokens)
            || !usage.TryGetProperty("completion_tokens", out var completion)
            || !completion.TryGetInt32(out var completionTokens))
        {
            return false;
        }

        facts = new OpenRouterTerminalFacts(expectedModel, selected, attempt, promptTokens, completionTokens);
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

    private static bool HasHit(HttpHeaders headers) =>
        headers.TryGetValues(OpenRouterAdapterContracts.ResponseCacheStatusHeader, out var values)
        && values.Any(value => string.Equals(value, "HIT", StringComparison.OrdinalIgnoreCase));
}

internal sealed class BoundedReadStream(Stream inner, int maxUtf8Bytes) : Stream
{
    private int _remaining = maxUtf8Bytes;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_remaining <= 0)
        {
            throw new OpenRouterTransportLimitExceededException();
        }

        var read = await inner.ReadAsync(buffer[..Math.Min(buffer.Length, _remaining)], cancellationToken);
        _remaining -= read;
        if (read == 0 && buffer.Length > 0 && _remaining <= 0)
        {
            throw new OpenRouterTransportLimitExceededException();
        }

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

internal static class OpenRouterSseParser
{
    public static async IAsyncEnumerable<string> ReadDataPayloadsAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        var line = new MemoryStream();
        var payload = new MemoryStream();
        var dataOpen = false;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var current = buffer[i];
                if (current == (byte)'\n')
                {
                    var flushed = CompleteLine(line, payload, ref dataOpen);
                    if (flushed is not null)
                    {
                        yield return flushed;
                    }

                    continue;
                }

                if (current == (byte)'\r')
                {
                    continue;
                }

                AppendBounded(line, current);
            }
        }

        if (line.Length > 0)
        {
            var flushed = CompleteLine(line, payload, ref dataOpen);
            if (flushed is not null)
            {
                yield return flushed;
            }
        }

        if (dataOpen)
        {
            yield return Encoding.UTF8.GetString(payload.ToArray());
        }
    }

    private static string? CompleteLine(MemoryStream line, MemoryStream payload, ref bool dataOpen)
    {
        var bytes = line.ToArray();
        line.SetLength(0);
        if (bytes.Length == 0)
        {
            if (!dataOpen)
            {
                return null;
            }

            dataOpen = false;
            var completed = Encoding.UTF8.GetString(payload.ToArray());
            payload.SetLength(0);
            return completed;
        }

        if (bytes[0] == (byte)':')
        {
            return null;
        }

        if (StartsWith(bytes, "event:"u8))
        {
            return null;
        }

        if (!StartsWith(bytes, "data:"u8))
        {
            return null;
        }

        var data = bytes.AsSpan(5);
        if (data.Length > 0 && data[0] == (byte)' ')
        {
            data = data[1..];
        }

        if (payload.Length > 0)
        {
            AppendBounded(payload, (byte)'\n');
        }

        AppendBounded(payload, data);
        dataOpen = true;
        return null;
    }

    private static void AppendBounded(MemoryStream destination, byte value) =>
        AppendBounded(destination, [value]);

    private static void AppendBounded(MemoryStream destination, ReadOnlySpan<byte> bytes)
    {
        if (destination.Length + bytes.Length > OpenRouterAdapterContracts.MaxSseEventUtf8Bytes)
        {
            throw new OpenRouterTransportLimitExceededException();
        }

        destination.Write(bytes);
    }

    private static bool StartsWith(ReadOnlySpan<byte> line, ReadOnlySpan<byte> prefix) =>
        line.Length >= prefix.Length && line[..prefix.Length].SequenceEqual(prefix);
}
