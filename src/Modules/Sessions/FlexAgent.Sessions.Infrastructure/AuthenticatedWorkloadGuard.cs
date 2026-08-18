using FlexAgent.IdentityAccess.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

internal static class AuthenticatedWorkloadGuard
{
    public static async Task<bool> IsCurrentForActorAsync(
        IAuthenticatedWorkloadContextSource? source,
        TrustedRuntimeActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (source is null)
        {
            return true;
        }

        var context = await source.TryGetCurrentAsync(cancellationToken).ConfigureAwait(false);
        return context is not null
            && context.ServiceActorId == actor.ActorId
            && context.IsProofValidAt(DateTimeOffset.UtcNow);
    }
}
