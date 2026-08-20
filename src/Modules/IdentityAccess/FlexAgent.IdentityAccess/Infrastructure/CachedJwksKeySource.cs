using System.Security.Cryptography;
using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class CachedJwksKeySource(
    HttpClient httpClient,
    TimeProvider clock,
    TimeSpan cacheLifetime,
    TimeSpan? forcedRefreshCooldown = null) : IJwksKeySource, IDisposable
{
    private readonly TimeSpan _forcedRefreshCooldown = forcedRefreshCooldown ?? TimeSpan.FromSeconds(5);
    private readonly object _gate = new();
    private readonly Dictionary<string, Task<IReadOnlyDictionary<string, RSA>?>> _refreshInFlight =
        new(StringComparer.Ordinal);
    private string? _cachedUri;
    private DateTimeOffset _cachedUntil;
    private string? _forcedRefreshUri;
    private DateTimeOffset _forcedRefreshAvailableAt;
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
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.GetUtcNow();
        Task<IReadOnlyDictionary<string, RSA>?> pending;
        lock (_gate)
        {
            if (TryGetFreshCache(jwksUri, requiredKid, now, out var cached))
            {
                return cached;
            }

            var forcedUnknownKid = HasLiveCache(jwksUri, now)
                && !string.IsNullOrWhiteSpace(requiredKid)
                && _cachedKeys is not null
                && !_cachedKeys.ContainsKey(requiredKid);
            if (forcedUnknownKid && ForcedRefreshOnCooldown(jwksUri, now))
            {
                return _cachedKeys;
            }

            if (_refreshInFlight.TryGetValue(jwksUri, out var inFlight))
            {
                pending = inFlight;
            }
            else
            {
                pending = RefreshAsync(jwksUri, forcedUnknownKid, now);
                _refreshInFlight[jwksUri] = pending;
            }
        }

        try
        {
            return await pending.ConfigureAwait(false);
        }
        finally
        {
            lock (_gate)
            {
                if (_refreshInFlight.TryGetValue(jwksUri, out var current)
                    && ReferenceEquals(current, pending))
                {
                    _refreshInFlight.Remove(jwksUri);
                }
            }
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

    private bool TryGetFreshCache(
        string jwksUri,
        string? requiredKid,
        DateTimeOffset now,
        out IReadOnlyDictionary<string, RSA>? cached)
    {
        cached = null;
        if (!HasLiveCache(jwksUri, now) || _cachedKeys is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(requiredKid) || _cachedKeys.ContainsKey(requiredKid))
        {
            cached = _cachedKeys;
            return true;
        }

        return false;
    }

    private bool HasLiveCache(string jwksUri, DateTimeOffset now) =>
        _cachedKeys is not null
        && string.Equals(_cachedUri, jwksUri, StringComparison.Ordinal)
        && now < _cachedUntil;

    private bool ForcedRefreshOnCooldown(string jwksUri, DateTimeOffset now) =>
        string.Equals(_forcedRefreshUri, jwksUri, StringComparison.Ordinal)
        && now < _forcedRefreshAvailableAt;

    private async Task<IReadOnlyDictionary<string, RSA>?> RefreshAsync(
        string jwksUri,
        bool forcedUnknownKid,
        DateTimeOffset requestedAt)
    {
        using var response = await httpClient.GetAsync(jwksUri, CancellationToken.None).ConfigureAwait(false);
        IReadOnlyDictionary<string, RSA>? parsed = null;
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
            parsed = JwksRsaKeyParser.TryParse(json);
        }

        lock (_gate)
        {
            if (forcedUnknownKid)
            {
                _forcedRefreshUri = jwksUri;
                _forcedRefreshAvailableAt = clock.GetUtcNow() + _forcedRefreshCooldown;
            }

            if (parsed is null)
            {
                return HasLiveCache(jwksUri, requestedAt) ? _cachedKeys : null;
            }

            if (!ReferenceEquals(_cachedKeys, parsed))
            {
                DisposeKeys(_cachedKeys);
            }

            _cachedUri = jwksUri;
            _cachedKeys = parsed;
            _cachedUntil = requestedAt + cacheLifetime;
            return _cachedKeys;
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
