namespace FlexAgent.Evaluation.Domain;

public sealed record EvaluationRequest(
    Guid RequestId,
    string RequestKind,
    FrozenInputIdentity FrozenInput,
    string IdempotencyKey,
    string DelegationRef,
    string State,
    Guid? PredecessorEvaluationId,
    string? ReplacementReason)
{
    public static EvaluationDecision<EvaluationRequest> TryCreate(
        Guid requestId,
        string requestKind,
        FrozenInputIdentity frozenInput,
        string idempotencyKey,
        string delegationRef,
        string state,
        Guid? predecessorEvaluationId = null,
        string? replacementReason = null)
    {
        if (requestId == Guid.Empty
            || requestKind is not (EvaluationRequestKinds.Initial or EvaluationRequestKinds.Replacement)
            || string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Length is < 8 or > 128
            || !EvaluationIdentity.IsStableId(delegationRef)
            || state is not (
                EvaluationRequestStates.Queued
                or EvaluationRequestStates.Running
                or EvaluationRequestStates.Validating
                or EvaluationRequestStates.Completing
                or EvaluationRequestStates.Completed
                or EvaluationRequestStates.FailedRetryable
                or EvaluationRequestStates.FailedReviewRequired
                or EvaluationRequestStates.Cancelled))
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (requestKind == EvaluationRequestKinds.Initial
            && (predecessorEvaluationId is not null || !string.IsNullOrWhiteSpace(replacementReason)))
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.InvalidField, "predecessor");
        }

        if (requestKind == EvaluationRequestKinds.Replacement
            && (predecessorEvaluationId is null
                || predecessorEvaluationId == Guid.Empty
                || !EvaluationIdentity.IsStableId(replacementReason)))
        {
            return EvaluationDecision<EvaluationRequest>.Fail(EvaluationFailureCodes.InvalidField, "replacement");
        }

        return EvaluationDecision<EvaluationRequest>.Ok(
            new EvaluationRequest(
                requestId,
                requestKind,
                frozenInput,
                idempotencyKey,
                delegationRef,
                state,
                predecessorEvaluationId,
                replacementReason));
    }
}

public sealed record InvocationAttempt(
    Guid AttemptId,
    Guid RequestId,
    int AttemptOrdinal,
    string State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc)
{
    public static EvaluationDecision<InvocationAttempt> TryCreate(
        Guid attemptId,
        Guid requestId,
        int attemptOrdinal,
        string state,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? finishedAtUtc)
    {
        if (attemptId == Guid.Empty
            || requestId == Guid.Empty
            || attemptOrdinal < 1
            || state is not (
                EvaluationRequestStates.Running
                or EvaluationRequestStates.Validating
                or EvaluationRequestStates.Completing
                or EvaluationRequestStates.FailedRetryable
                or EvaluationRequestStates.FailedReviewRequired
                or EvaluationRequestStates.Cancelled
                or EvaluationRequestStates.Completed)
            || !EvaluationIdentity.IsUtc(startedAtUtc)
            || (finishedAtUtc is not null && !EvaluationIdentity.IsUtc(finishedAtUtc.Value))
            || (finishedAtUtc is not null && finishedAtUtc.Value < startedAtUtc))
        {
            return EvaluationDecision<InvocationAttempt>.Fail(EvaluationFailureCodes.InvalidField);
        }

        return EvaluationDecision<InvocationAttempt>.Ok(
            new InvocationAttempt(attemptId, requestId, attemptOrdinal, state, startedAtUtc, finishedAtUtc));
    }
}
