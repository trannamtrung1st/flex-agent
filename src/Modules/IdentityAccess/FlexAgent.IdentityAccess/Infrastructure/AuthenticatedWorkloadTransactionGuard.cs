using System.Data;
using FlexAgent.IdentityAccess.Application;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class AuthenticatedWorkloadTransactionGuard(
    IAuthenticatedWorkloadContextSource? source) : IAuthenticatedWorkloadTransactionGuard
{
    public Task<bool> IsCurrentForActorAsync(
        Guid serviceActorId,
        CancellationToken cancellationToken) =>
        IsCurrentForActorCoreAsync(serviceActorId, transaction: null, cancellationToken);

    public Task<bool> IsCurrentForActorInTransactionAsync(
        Guid serviceActorId,
        IDbTransaction transaction,
        CancellationToken cancellationToken) =>
        IsCurrentForActorCoreAsync(serviceActorId, transaction, cancellationToken);

    private async Task<bool> IsCurrentForActorCoreAsync(
        Guid serviceActorId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (serviceActorId == Guid.Empty)
        {
            return false;
        }

        if (source is null)
        {
            return true;
        }

        var context = await source.TryGetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (context is null
            || context.ServiceActorId != serviceActorId
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

        return transaction is NpgsqlTransaction npgsqlTransaction
            && await PostgresServicePrincipalBindingCoordinator.MatchesCurrentInTransactionAsync(
                context.BindingId,
                context.BindingVersion,
                context.ServiceActorId,
                npgsqlTransaction,
                cancellationToken).ConfigureAwait(false);
    }
}
