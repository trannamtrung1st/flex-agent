using System.Data;
using FlexAgent.Evaluation.Application;
using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.Worker;

internal sealed class EvaluationWorkloadIdentityGateAdapter(
    IAuthenticatedWorkloadTransactionGuard guard) : IEvaluationWorkloadIdentityGate
{
    public Task<bool> IsCurrentForWorkerActorAsync(
        Guid workerActorId,
        CancellationToken cancellationToken) =>
        guard.IsCurrentForActorAsync(workerActorId, cancellationToken);

    public Task<bool> IsCurrentForWorkerActorInTransactionAsync(
        Guid workerActorId,
        IDbTransaction transaction,
        CancellationToken cancellationToken) =>
        guard.IsCurrentForActorInTransactionAsync(workerActorId, transaction, cancellationToken);
}
