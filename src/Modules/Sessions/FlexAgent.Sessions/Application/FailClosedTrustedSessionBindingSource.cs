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
}
