using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationDeterministicGuid
{
    public static Guid CreateVersion5(Guid namespaceId, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var namespaceBytes = namespaceId.ToByteArray();
        Array.Reverse(namespaceBytes, 0, 4);
        Array.Reverse(namespaceBytes, 4, 2);
        Array.Reverse(namespaceBytes, 6, 2);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var hash = SHA1.HashData([..namespaceBytes, ..nameBytes]);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(new ReadOnlySpan<byte>(hash, 0, 16));
    }
}
