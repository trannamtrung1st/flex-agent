using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Infrastructure;

public static class EvaluationInfrastructure
{
    public const bool ProcessingEnabled = false;
}

public sealed class DisabledEvaluationAdmission
{
    public EvaluationDecision<EvaluationRequest> Admit(
        FrozenInputIdentity frozenInput,
        string idempotencyKey,
        string delegationRef)
    {
        _ = frozenInput;
        _ = idempotencyKey;
        _ = delegationRef;
        return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.ProcessingDisabled);
    }
}
