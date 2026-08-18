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
        ArgumentNullException.ThrowIfNull(actor);
        if (source is null)
        {
            return true;
        }

        var context = await source.TryGetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (context is null
            || context.ServiceActorId != actor.ActorId
            || !context.IsProofValidAt(DateTimeOffset.UtcNow))
        {
            return false;
        }

        if (string.Equals(
            context.Profile,
            WorkloadIdentityProfiles.SyntheticConfiguredActor,
            StringComparison.Ordinal))
        {
            return true;
        }

        return transaction is not null
            && await PostgresServicePrincipalBindingCoordinator.MatchesCurrentInTransactionAsync(
                context.BindingId,
                context.BindingVersion,
                context.ServiceActorId,
                transaction,
                cancellationToken).ConfigureAwait(false);
    }
}
