using System.Collections.Concurrent;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class MemoryHumanIdentityBindingStore : IHumanIdentityBindingStore
{
    private readonly ConcurrentDictionary<string, HumanIdentityBinding> _bindings = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, (bool Exists, bool Disabled)> _actors = new();
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _organizations = new();

    public void RegisterActor(Guid actorId, bool disabled = false) =>
        _actors[actorId] = (true, disabled);

    public void DisableActor(Guid actorId)
    {
        _actors.AddOrUpdate(actorId, (true, true), (_, current) => (current.Exists, true));
    }

    public void GrantOrganization(Guid actorId, Guid organizationId)
    {
        _organizations.AddOrUpdate(
            actorId,
            _ => [organizationId],
            (_, current) =>
            {
                lock (current)
                {
                    current.Add(organizationId);
                    return current;
                }
            });
    }

    public Task<HumanIdentityBinding?> FindByIdentityAsync(
        ExactIssuerSubject identity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _bindings.TryGetValue(Key(identity), out var binding);
        return Task.FromResult(binding);
    }

    public Task<IReadOnlyList<Guid>> ListEligibleOrganizationIdsAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_organizations.TryGetValue(actorId, out var organizations))
        {
            return Task.FromResult<IReadOnlyList<Guid>>([]);
        }

        lock (organizations)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(organizations.ToArray());
        }
    }

    public Task<(bool Exists, bool Disabled)> GetActorStateAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_actors.TryGetValue(actorId, out var state) ? state : (false, false));
    }

    public Task<string?> TryProvisionAsync(
        HumanIdentityBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        cancellationToken.ThrowIfCancellationRequested();
        var key = Key(binding.Identity);
        if (_bindings.TryGetValue(key, out var existing))
        {
            return Task.FromResult<string?>(
                existing.ActorId == binding.ActorId
                    ? HumanAuthenticationReasonCodes.UnknownSubject
                    : HumanAuthenticationReasonCodes.ReboundIdentity);
        }

        _bindings[key] = binding;
        RegisterActor(binding.ActorId);
        return Task.FromResult<string?>(null);
    }

    public Task DisableByIdentityAsync(
        ExactIssuerSubject identity,
        DateTimeOffset disabledAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = Key(identity);
        if (_bindings.TryGetValue(key, out var binding))
        {
            _bindings[key] = binding with { DisabledAt = disabledAt };
            DisableActor(binding.ActorId);
        }

        return Task.CompletedTask;
    }

    private static string Key(ExactIssuerSubject identity) => $"{identity.Issuer}\n{identity.Subject}";
}

public sealed class MemoryApplicationSessionStore : IApplicationSessionStore
{
    private readonly ConcurrentDictionary<Guid, ApplicationSessionRecord> _sessions = new();
    private readonly HashSet<string> _revokedProviderSessions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumedLogoutTokens = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public IReadOnlyCollection<ApplicationSessionRecord> Snapshot => _sessions.Values.ToArray();

    public Task InsertAsync(ApplicationSessionRecord session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryInsertCore(session, cancellationToken))
        {
            throw new InvalidOperationException("Application session already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryInsertLiveSessionAsync(
        ApplicationSessionRecord session,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TryInsertCore(session, cancellationToken));

    private bool TryInsertCore(ApplicationSessionRecord session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (HasSuccessor(session.PredecessorSessionId)
                || ProviderSessionIsRevoked(session.ProviderSessionDigest)
                || !_sessions.TryAdd(session.ApplicationSessionId, session))
            {
                return false;
            }
        }

        return true;
    }

    public Task<ApplicationSessionRecord?> FindLiveByCredentialDigestAsync(
        string credentialDigest,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var match = _sessions.Values.SingleOrDefault(session =>
            session.IsLive && string.Equals(session.CredentialDigest, credentialDigest, StringComparison.Ordinal));
        return Task.FromResult(match);
    }

    public Task<ApplicationSessionRecord?> GetByIdAsync(
        Guid applicationSessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessions.TryGetValue(applicationSessionId, out var session);
        return Task.FromResult(session);
    }

    public Task TerminateLiveAsync(
        Guid applicationSessionId,
        DateTimeOffset terminatedAt,
        string terminalReason,
        bool rotated,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_sessions.TryGetValue(applicationSessionId, out var current) && current.IsLive)
            {
                _sessions[applicationSessionId] = Terminate(current, terminatedAt, terminalReason, rotated);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryRotateAsync(
        Guid predecessorSessionId,
        DateTimeOffset terminatedAt,
        string terminalReason,
        ApplicationSessionRecord successor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(successor);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_sessions.TryGetValue(predecessorSessionId, out var current)
                || !current.IsLive
                || HasSuccessor(predecessorSessionId)
                || successor.PredecessorSessionId != predecessorSessionId
                || ProviderSessionIsRevoked(successor.ProviderSessionDigest)
                || !_sessions.TryAdd(successor.ApplicationSessionId, successor))
            {
                return Task.FromResult(false);
            }

            _sessions[predecessorSessionId] = Terminate(current, terminatedAt, terminalReason, rotated: true);
            return Task.FromResult(true);
        }
    }

    public Task TouchActivityAsync(
        Guid applicationSessionId,
        ApplicationSessionLifetime lifetime,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessions.AddOrUpdate(
            applicationSessionId,
            _ => throw new InvalidOperationException("Application session not found."),
            (_, current) => current with { Lifetime = lifetime });
        return Task.CompletedTask;
    }

    public Task<int> RevokeLiveByIdentityAsync(
        ExactIssuerSubject identity,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(RevokeWhere(
                session => session.IsLive && session.Identity.Matches(identity.Issuer, identity.Subject),
                revokedAt,
                terminalReason));
        }
    }

    public Task<int> RevokeLiveByProviderSessionDigestAsync(
        string providerSessionDigest,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _revokedProviderSessions.Add(providerSessionDigest);
            return Task.FromResult(RevokeWhere(
                session => session.IsLive
                    && string.Equals(session.ProviderSessionDigest, providerSessionDigest, StringComparison.Ordinal),
                revokedAt,
                terminalReason));
        }
    }

    public Task<ForcedLogoutApplyResult> TryApplyForcedLogoutAsync(
        string issuer,
        string jwtId,
        string? providerSessionDigest,
        ExactIssuerSubject? identity,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtId);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var key = $"{issuer}\n{jwtId}";
            if (!_consumedLogoutTokens.Add(key))
            {
                return Task.FromResult(ForcedLogoutApplyResult.Duplicate());
            }

            var count = 0;
            if (!string.IsNullOrWhiteSpace(providerSessionDigest))
            {
                _revokedProviderSessions.Add(providerSessionDigest);
                count = RevokeWhere(
                    session => session.IsLive
                        && string.Equals(
                            session.ProviderSessionDigest,
                            providerSessionDigest,
                            StringComparison.Ordinal),
                    revokedAt,
                    terminalReason);
            }
            else if (identity is not null)
            {
                count = RevokeWhere(
                    session => session.IsLive && session.Identity.Matches(identity.Issuer, identity.Subject),
                    revokedAt,
                    terminalReason);
            }

            return Task.FromResult(ForcedLogoutApplyResult.Applied(count));
        }
    }

    private int RevokeWhere(
        Func<ApplicationSessionRecord, bool> predicate,
        DateTimeOffset revokedAt,
        string terminalReason)
    {
        var count = 0;
        foreach (var session in _sessions.Values.Where(predicate).ToArray())
        {
            _sessions[session.ApplicationSessionId] = session with
            {
                CredentialDigest = null,
                RevokedAt = revokedAt,
                TerminalReason = terminalReason,
            };
            count++;
        }

        return count;
    }

    private bool HasSuccessor(Guid? predecessorSessionId) =>
        predecessorSessionId is Guid predecessor
        && _sessions.Values.Any(session => session.PredecessorSessionId == predecessor);

    private bool ProviderSessionIsRevoked(string? providerSessionDigest) =>
        !string.IsNullOrWhiteSpace(providerSessionDigest)
        && _revokedProviderSessions.Contains(providerSessionDigest);

    private static ApplicationSessionRecord Terminate(
        ApplicationSessionRecord current,
        DateTimeOffset terminatedAt,
        string terminalReason,
        bool rotated) =>
        current with
        {
            CredentialDigest = null,
            RotatedAt = rotated ? terminatedAt : current.RotatedAt,
            RevokedAt = rotated ? current.RevokedAt : terminatedAt,
            TerminalReason = terminalReason,
        };
}

public sealed class MemoryLogoutTokenReplayStore : ILogoutTokenReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _consumed = new(StringComparer.Ordinal);

    public Task<bool> TryConsumeAsync(
        string issuer,
        string jwtId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtId);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_consumed.TryAdd($"{issuer}\n{jwtId}", consumedAt));
    }
}

public sealed class MemoryOidcLoginTransactionStore : IOidcLoginTransactionStore
{
    private readonly ConcurrentDictionary<string, OidcLoginTransaction> _transactions = new(StringComparer.Ordinal);

    public Task CreateAsync(OidcLoginTransaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_transactions.TryAdd(transaction.StateDigest, transaction))
        {
            throw new InvalidOperationException("OIDC transaction already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<OidcLoginTransaction?> ConsumeAsync(
        string stateDigest,
        string correlationDigest,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_transactions.TryGetValue(stateDigest, out var transaction)
            || transaction.ExpiresAt <= now
            || !string.Equals(transaction.CorrelationDigest, correlationDigest, StringComparison.Ordinal)
            || !_transactions.TryRemove(stateDigest, out transaction))
        {
            return Task.FromResult<OidcLoginTransaction?>(null);
        }

        return Task.FromResult<OidcLoginTransaction?>(transaction);
    }
}

public sealed class MemoryAuthenticationSecurityEventWriter : IAuthenticationSecurityEventWriter
{
    private readonly ConcurrentBag<AuthenticationSecurityEvent> _events = [];

    public IReadOnlyCollection<AuthenticationSecurityEvent> Events => _events.ToArray();

    public Task WriteAsync(
        AuthenticationSecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(securityEvent);
        cancellationToken.ThrowIfCancellationRequested();
        _events.Add(securityEvent);
        return Task.CompletedTask;
    }
}
