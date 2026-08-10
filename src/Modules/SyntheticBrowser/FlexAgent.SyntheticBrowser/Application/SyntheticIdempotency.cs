using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.Contracts.Browser;
using FlexAgent.SyntheticBrowser.Domain;

namespace FlexAgent.SyntheticBrowser.Application;

public sealed class SyntheticIdempotencyRecord
{
    public required string RequestDigest { get; init; }
    public required BrowserCommandResultV1 Result { get; init; }
}

internal static class SyntheticIdempotency
{
    internal static string BuildScopeKey(SyntheticSessionRecord session, BrowserCommandEnvelopeV1 command) =>
        string.Join('|',
            session.SessionId,
            session.ActorStage,
            command.IdempotencyKey,
            command.CommandType,
            command.ResourceId ?? string.Empty);

    internal static string BuildRequestDigest(BrowserCommandEnvelopeV1 command)
    {
        var payload = command.Payload is null
            ? string.Empty
            : JsonSerializer.Serialize(
                command.Payload.OrderBy(static pair => pair.Key, StringComparer.Ordinal),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        var material = string.Join('\n',
            command.SchemaVersion,
            command.CommandType,
            command.ResourceId ?? string.Empty,
            command.ExpectedVersion?.ToString() ?? string.Empty,
            payload);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash);
    }

    internal static BrowserCommandResultV1? TryReplay(
        IDictionary<string, SyntheticIdempotencyRecord> records,
        string scopeKey,
        string requestDigest)
    {
        if (!records.TryGetValue(scopeKey, out var existing))
        {
            return null;
        }

        if (!string.Equals(existing.RequestDigest, requestDigest, StringComparison.Ordinal))
        {
            return new BrowserCommandResultV1(
                BrowserSchemaVersion.V1,
                "conflict",
                Guid.NewGuid().ToString("N"),
                null,
                null,
                "reconcile",
                "Idempotency key was reused with a different request.");
        }

        return existing.Result;
    }

    internal static void Remember(
        IDictionary<string, SyntheticIdempotencyRecord> records,
        string scopeKey,
        string requestDigest,
        BrowserCommandResultV1 result)
    {
        if (result.Outcome is not ("succeeded" or "uncertain"))
        {
            return;
        }

        records[scopeKey] = new SyntheticIdempotencyRecord
        {
            RequestDigest = requestDigest,
            Result = result,
        };
    }
}
