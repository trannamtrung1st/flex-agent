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

    public IReadOnlyCollection<ApplicationSessionRecord> Snapshot => _sessions.Values.ToArray();

    public Task InsertAsync(ApplicationSessionRecord session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryAdd(session.ApplicationSessionId, session))
        {
            throw new InvalidOperationException("Application session already exists.");
        }

        return Task.CompletedTask;
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
        _sessions.AddOrUpdate(
            applicationSessionId,
            _ => throw new InvalidOperationException("Application session not found."),
            (_, current) => current with
            {
                CredentialDigest = null,
                RotatedAt = rotated ? terminatedAt : current.RotatedAt,
                RevokedAt = rotated ? current.RevokedAt : terminatedAt,
                TerminalReason = terminalReason,
            });
        return Task.CompletedTask;
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
        return Task.FromResult(RevokeWhere(
            session => session.IsLive && session.Identity.Matches(identity.Issuer, identity.Subject),
            revokedAt,
            terminalReason));
    }

    public Task<int> RevokeLiveByProviderSessionDigestAsync(
        string providerSessionDigest,
        DateTimeOffset revokedAt,
        string terminalReason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(RevokeWhere(
            session => session.IsLive
                && string.Equals(session.ProviderSessionDigest, providerSessionDigest, StringComparison.Ordinal),
            revokedAt,
            terminalReason));
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
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_transactions.TryRemove(stateDigest, out var transaction) || transaction.ExpiresAt <= now)
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
