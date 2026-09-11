using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelExecutionAuthorityVerifier
{
    public static EvaluationDecision<EvaluationModelExecutionContext> TryValidateBoundContext(
        EvaluationModelExecutionContext context,
        string evaluatorMode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(evaluatorMode);

        if (context.RequestId == Guid.Empty
            || context.InvocationAttemptId == Guid.Empty
            || context.EvaluationId == Guid.Empty
            || string.IsNullOrWhiteSpace(context.RequestStableId)
            || string.IsNullOrWhiteSpace(context.InvocationAttemptStableId))
        {
            return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "request");
        }

        if (!EvaluationStableOwnershipReferenceFactory.SessionOwnershipRefMatches(
                context.Ownership,
                context.OwnershipScope))
        {
            return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                EvaluationFailureCodes.IncompleteOwnership,
                "ownership");
        }

        if (!string.Equals(
                context.RequestStableId,
                EvaluationStableOwnershipReferenceFactory.StableRequestId(context.RequestId),
                StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "request_id");
        }

        if (!string.Equals(
                context.InvocationAttemptStableId,
                EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(context.InvocationAttemptId),
                StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "invocation_attempt_id");
        }

        if (string.Equals(evaluatorMode, EvaluatorModes.AgentAssisted, StringComparison.Ordinal))
        {
            if (context.DeterministicInvocationId is not { } deterministicInvocationId
                || deterministicInvocationId == Guid.Empty
                || string.IsNullOrWhiteSpace(context.DeterministicInvocationStableId))
            {
                return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_invocation_id");
            }

            if (!string.Equals(
                    context.DeterministicInvocationStableId,
                    EvaluationStableOwnershipReferenceFactory.StableDeterministicInvocationId(
                        deterministicInvocationId),
                    StringComparison.Ordinal))
            {
                return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_invocation_id");
            }
        }
        else if (context.DeterministicInvocationId is not null
                 || !string.IsNullOrWhiteSpace(context.DeterministicInvocationStableId))
        {
            return EvaluationDecision<EvaluationModelExecutionContext>.Fail(
                EvaluationFailureCodes.DeterministicConflict,
                "deterministic_invocation_id");
        }

        return EvaluationDecision<EvaluationModelExecutionContext>.Ok(context);
    }

    public static async Task<EvaluationDecision<AdmittedEvaluationRequestAuthority>> TryReloadAuthorityAsync(
        EvaluationModelExecutionContext context,
        IEvaluationRequestAuthorityStore authorityStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorityStore);

        var authority = await authorityStore.TryLoadAsync(
            context.OwnershipScope,
            context.RequestId,
            context.InvocationAttemptId,
            cancellationToken);
        if (authority is null)
        {
            return EvaluationDecision<AdmittedEvaluationRequestAuthority>.Fail(
                EvaluationFailureCodes.InvalidField,
                "request");
        }

        if (authority.RequestId != context.RequestId
            || authority.InvocationAttemptId != context.InvocationAttemptId
            || !OwnershipMatches(authority.Ownership, context.OwnershipScope))
        {
            return EvaluationDecision<AdmittedEvaluationRequestAuthority>.Fail(
                EvaluationFailureCodes.IncompleteOwnership,
                "ownership");
        }

        return EvaluationDecision<AdmittedEvaluationRequestAuthority>.Ok(authority);
    }

    private static bool OwnershipMatches(EvaluationOwnership authority, EvaluationOwnership scope) =>
        authority.OrganizationId == scope.OrganizationId
        && authority.ActivityId == scope.ActivityId
        && authority.ParticipantId == scope.ParticipantId
        && authority.AttemptId == scope.AttemptId
        && authority.SessionId == scope.SessionId;
}
