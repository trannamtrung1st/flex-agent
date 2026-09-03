using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Sessions.Domain;

public static class HostedSessionCommandCorrelation
{
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
