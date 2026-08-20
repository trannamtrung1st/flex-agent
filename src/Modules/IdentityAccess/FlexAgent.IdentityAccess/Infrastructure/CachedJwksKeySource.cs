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
    private readonly Dictionary<string, Task<IReadOnlyDictionary<string, RSAParameters>?>> _refreshInFlight =
        new(StringComparer.Ordinal);
    private string? _cachedUri;
    private DateTimeOffset _cachedUntil;
    private string? _forcedRefreshUri;
    private DateTimeOffset _forcedRefreshAvailableAt;
    private IReadOnlyDictionary<string, RSAParameters>? _cachedParameters;

    public Task<JwksKeySnapshot?> TryGetKeysAsync(
        string jwksUri,
        CancellationToken cancellationToken = default) =>
        TryGetKeysAsync(jwksUri, requiredKid: null, cancellationToken);

    public async Task<JwksKeySnapshot?> TryGetKeysAsync(
        string jwksUri,
        string? requiredKid,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwksUri);
        cancellationToken.ThrowIfCancellationRequested();
        var now = clock.GetUtcNow();
        Task<IReadOnlyDictionary<string, RSAParameters>?> pending;
        lock (_gate)
        {
            if (TryGetFreshCache(jwksUri, requiredKid, now, out var cached) && cached is not null)
            {
                return JwksKeySnapshot.TryFromParameters(cached);
            }

            var forcedUnknownKid = HasLiveCache(jwksUri, now)
                && !string.IsNullOrWhiteSpace(requiredKid)
                && _cachedParameters is not null
                && !_cachedParameters.ContainsKey(requiredKid);
            if (forcedUnknownKid && ForcedRefreshOnCooldown(jwksUri, now) && _cachedParameters is not null)
            {
                return JwksKeySnapshot.TryFromParameters(_cachedParameters);
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
            var parameters = await pending.ConfigureAwait(false);
            return parameters is null ? null : JwksKeySnapshot.TryFromParameters(parameters);
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
            _cachedParameters = null;
        }
    }

    private bool TryGetFreshCache(
        string jwksUri,
        string? requiredKid,
        DateTimeOffset now,
        out IReadOnlyDictionary<string, RSAParameters>? cached)
    {
        cached = null;
        if (!HasLiveCache(jwksUri, now) || _cachedParameters is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(requiredKid) || _cachedParameters.ContainsKey(requiredKid))
        {
            cached = _cachedParameters;
            return true;
        }

        return false;
    }

    private bool HasLiveCache(string jwksUri, DateTimeOffset now) =>
        _cachedParameters is not null
        && string.Equals(_cachedUri, jwksUri, StringComparison.Ordinal)
        && now < _cachedUntil;

    private bool ForcedRefreshOnCooldown(string jwksUri, DateTimeOffset now) =>
        string.Equals(_forcedRefreshUri, jwksUri, StringComparison.Ordinal)
        && now < _forcedRefreshAvailableAt;

    private async Task<IReadOnlyDictionary<string, RSAParameters>?> RefreshAsync(
        string jwksUri,
        bool forcedUnknownKid,
        DateTimeOffset requestedAt)
    {
        using var response = await httpClient.GetAsync(jwksUri, CancellationToken.None).ConfigureAwait(false);
        IReadOnlyDictionary<string, RSAParameters>? parsed = null;
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
            parsed = JwksRsaKeyParser.TryParseParameters(json);
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
                return HasLiveCache(jwksUri, requestedAt) ? _cachedParameters : null;
            }

            _cachedUri = jwksUri;
            _cachedParameters = parsed;
            _cachedUntil = requestedAt + cacheLifetime;
            return _cachedParameters;
        }
    }
}
