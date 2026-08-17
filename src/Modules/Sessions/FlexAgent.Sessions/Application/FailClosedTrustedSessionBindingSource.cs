using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class FailClosedTrustedSessionBindingSource : ITrustedSessionBindingSource
{
    public static FailClosedTrustedSessionBindingSource Instance { get; } = new();

    public Task<TrustedSessionBinding?> GetAsync(
        SessionOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        return Task.FromResult<TrustedSessionBinding?>(null);
    }

    public Task<TrustedSessionBinding?> GetForOrganizationSessionAsync(
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        _ = organizationId;
        _ = sessionId;
        return Task.FromResult<TrustedSessionBinding?>(null);
    }
}
