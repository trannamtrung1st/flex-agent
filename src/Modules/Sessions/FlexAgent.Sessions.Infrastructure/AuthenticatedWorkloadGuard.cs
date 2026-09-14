using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Sessions.Infrastructure;

internal static class AuthenticatedWorkloadGuard
{
    public static async Task<bool> IsCurrentForActorAsync(
        IAuthenticatedWorkloadContextSource? source,
        TrustedRuntimeActor actor,
        CancellationToken cancellationToken,
        NpgsqlTransaction? transaction = null)
    {
        var guard = new AuthenticatedWorkloadTransactionGuard(source);
        if (transaction is null)
        {
            return await guard.IsCurrentForActorAsync(actor.ActorId, cancellationToken)
                .ConfigureAwait(false);
        }

        return await guard.IsCurrentForActorInTransactionAsync(
                actor.ActorId,
                transaction,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
