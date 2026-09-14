using System.Data;

namespace FlexAgent.IdentityAccess.Application;

public interface IAuthenticatedWorkloadTransactionGuard
{
    Task<bool> IsCurrentForActorAsync(
        Guid serviceActorId,
        CancellationToken cancellationToken);

    Task<bool> IsCurrentForActorInTransactionAsync(
        Guid serviceActorId,
        IDbTransaction transaction,
        CancellationToken cancellationToken);
}
