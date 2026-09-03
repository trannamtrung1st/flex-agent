using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Sessions.Domain;

/// <summary>
/// Derives stable correlation identifiers from hosted Session command IDs.
/// </summary>
public static class HostedSessionCommandCorrelation
{
    /// <summary>
    /// Command IDs for expiry-driven lifecycle transitions. Complete uses a
    /// <c>.complete</c> suffix so audit/outbox correlation stays distinct from
    /// BeginCompleting for both Worker sweeps and request-triggered expiry.
    /// </summary>
    public static string ExpiryCommandId(Guid sessionId, string transition) =>
        transition == SessionLifecycleTransitions.Complete
            ? $"sessioncommand.expiry.{sessionId:N}.complete"
            : $"sessioncommand.expiry.{sessionId:N}";

    public static Guid ForCommandId(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            throw new ArgumentException("Command ID is required.", nameof(commandId));
        }

        if (Guid.TryParse(commandId, out var parsed))
        {
            return parsed;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("flex-agent.session.command-correlation:" + commandId));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16));
    }
}
