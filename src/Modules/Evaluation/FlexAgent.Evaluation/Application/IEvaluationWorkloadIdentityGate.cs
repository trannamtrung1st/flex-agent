using System.Data;

namespace FlexAgent.Evaluation.Application;

public interface IEvaluationWorkloadIdentityGate
{
    Task<bool> IsCurrentForWorkerActorAsync(
        Guid workerActorId,
        CancellationToken cancellationToken);

    Task<bool> IsCurrentForWorkerActorInTransactionAsync(
        Guid workerActorId,
        IDbTransaction transaction,
        CancellationToken cancellationToken);
}
