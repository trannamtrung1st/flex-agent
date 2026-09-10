using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record AdmittedEvaluationRequestAuthority(
    Guid RequestId,
    Guid InvocationAttemptId,
    EvaluationOwnership Ownership,
    string FrozenInputDigest,
    ExactSourceIdentity ProcedureRef,
    string EvaluatorRegistryVersion);

public sealed record VerifiedFrozenProcedureExecutionContext(
    AdmittedEvaluationRequestAuthority Authority,
    EvaluationProcedureV1 Procedure);

public interface IEvaluationRequestAuthorityStore
{
    Task<AdmittedEvaluationRequestAuthority?> TryLoadAsync(
        EvaluationOwnership ownership,
        Guid requestId,
        Guid invocationAttemptId,
        CancellationToken cancellationToken);
}
