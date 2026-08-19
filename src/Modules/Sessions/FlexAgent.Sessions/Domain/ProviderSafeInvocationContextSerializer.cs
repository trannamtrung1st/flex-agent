using System.Text.Json;

namespace FlexAgent.Sessions.Domain;

public static class ProviderSafeInvocationContextSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string Serialize(string invocationId, InvocationContext? context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invocationId);
        var payload = new ProviderSafeInvocationContext(
            invocationId,
            context?.PolicyDigest ?? string.Empty,
            context?.ConfigurationDigest ?? string.Empty,
            context?.VisibleTranscript.Select(item => new ProviderSafeTranscriptItem(
                item.AuthorType,
                item.MessageId,
                item.TurnId,
                item.ContentRef.ProtectedRef,
                item.ExactUtf8Text)).ToArray() ?? [],
            context?.SubmissionRefs.Select(item => item.ProtectedRef).ToArray() ?? [],
            context?.KnowledgeRefs.Select(item => item.ProtectedRef).ToArray() ?? [],
            context?.MemoryReadRefs.Select(item => item.ProtectedRef).ToArray() ?? []);
        return JsonSerializer.Serialize(payload, Options);
    }

    private sealed record ProviderSafeInvocationContext(
        string InvocationId,
        string PolicyDigest,
        string ConfigurationDigest,
        IReadOnlyList<ProviderSafeTranscriptItem> Transcript,
        IReadOnlyList<string> SubmissionRefs,
        IReadOnlyList<string> KnowledgeRefs,
        IReadOnlyList<string> MemoryReadRefs);

    private sealed record ProviderSafeTranscriptItem(
        string AuthorType,
        string MessageId,
        string? TurnId,
        string ContentRef,
        string? ExactUtf8Text);
}
