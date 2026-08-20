using System.Security.Cryptography;
using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class CachedJwksKeySource(
    HttpClient httpClient,
    TimeProvider clock,
    TimeSpan cacheLifetime) : IJwksKeySource, IDisposable
{
    private readonly object _gate = new();
    private string? _cachedUri;
    private DateTimeOffset _cachedUntil;
    private IReadOnlyDictionary<string, RSA>? _cachedKeys;

    public Task<IReadOnlyDictionary<string, RSA>?> TryGetKeysAsync(
        string jwksUri,
        CancellationToken cancellationToken = default) =>
        TryGetKeysAsync(jwksUri, requiredKid: null, cancellationToken);

    public async Task<IReadOnlyDictionary<string, RSA>?> TryGetKeysAsync(
        string jwksUri,
        string? requiredKid,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwksUri);
        var now = clock.GetUtcNow();
        lock (_gate)
        {
            if (_cachedKeys is not null
                && string.Equals(_cachedUri, jwksUri, StringComparison.Ordinal)
                && now < _cachedUntil
                && (string.IsNullOrWhiteSpace(requiredKid) || _cachedKeys.ContainsKey(requiredKid)))
            {
                return _cachedKeys;
            }
        }

        using var response = await httpClient.GetAsync(jwksUri, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            lock (_gate)
            {
                return _cachedKeys is not null
                    && string.Equals(_cachedUri, jwksUri, StringComparison.Ordinal)
                    && now < _cachedUntil
                    ? _cachedKeys
                    : null;
            }
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var parsed = JwksRsaKeyParser.TryParse(json);
        if (parsed is null)
        {
            return null;
        }

        lock (_gate)
        {
            _cachedUri = jwksUri;
            _cachedKeys = parsed;
            _cachedUntil = now + cacheLifetime;
            return _cachedKeys;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            DisposeKeys(_cachedKeys);
            _cachedKeys = null;
        }
    }

    private static void DisposeKeys(IReadOnlyDictionary<string, RSA>? keys)
    {
        if (keys is null)
        {
            return;
        }

        foreach (var key in keys.Values)
        {
            key.Dispose();
        }
    }
}
