using System.Security.Cryptography;

namespace FlexAgent.IdentityAccess.Application;

public sealed class JwksKeySnapshot : IDisposable
{
    private readonly Dictionary<string, RSA> _keys;
    private readonly bool _ownsKeys;
    private bool _disposed;

    private JwksKeySnapshot(Dictionary<string, RSA> keys, bool ownsKeys)
    {
        _keys = keys;
        _ownsKeys = ownsKeys;
    }

    public IReadOnlyDictionary<string, RSA> Keys => _keys;

    public bool ContainsKey(string kid) => _keys.ContainsKey(kid);

    public static JwksKeySnapshot? TryFromParameters(IReadOnlyDictionary<string, RSAParameters> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var keys = new Dictionary<string, RSA>(StringComparer.Ordinal);
        try
        {
            foreach (var (kid, value) in parameters)
            {
                RSA? rsa = null;
                try
                {
                    rsa = RSA.Create();
                    rsa.ImportParameters(value);
                    keys[kid] = rsa;
                    rsa = null;
                }
                finally
                {
                    rsa?.Dispose();
                }
            }
        }
        catch (CryptographicException)
        {
            foreach (var key in keys.Values)
            {
                key.Dispose();
            }

            return null;
        }

        return keys.Count == 0 ? null : new JwksKeySnapshot(keys, ownsKeys: true);
    }

    public static JwksKeySnapshot Borrowed(IReadOnlyDictionary<string, RSA> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return new JwksKeySnapshot(
            keys.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            ownsKeys: false);
    }

    public void Dispose()
    {
        if (_disposed || !_ownsKeys)
        {
            return;
        }

        _disposed = true;
        foreach (var key in _keys.Values)
        {
            key.Dispose();
        }
    }
}
