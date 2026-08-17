using System.Collections.Concurrent;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class MemoryTrustedSessionBindingSource : ITrustedSessionBindingSource
{
    private readonly ConcurrentDictionary<(Guid OrganizationId, Guid SessionId), TrustedSessionBinding> _bindings =
        new();

    public void Register(TrustedSessionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _bindings[(binding.Ownership.OrganizationId, binding.Ownership.SessionId)] = binding;
    }

    public Task<TrustedSessionBinding?> GetAsync(
        SessionOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        if (!_bindings.TryGetValue((ownership.OrganizationId, ownership.SessionId), out var binding)
            || binding.Ownership != ownership)
        {
            return Task.FromResult<TrustedSessionBinding?>(null);
        }

        return Task.FromResult<TrustedSessionBinding?>(binding);
    }

    public Task<TrustedSessionBinding?> GetForOrganizationSessionAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!_bindings.TryGetValue((organizationId, sessionId), out var binding))
        {
            return Task.FromResult<TrustedSessionBinding?>(null);
        }

        return Task.FromResult<TrustedSessionBinding?>(binding);
    }
}
