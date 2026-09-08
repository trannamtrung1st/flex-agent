using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationPersistenceOutcomeCodes
{
    public const string Admitted = "evaluation.admitted";
    public const string Reconciled = "evaluation.reconciled";
    public const string IdempotencyConflict = "evaluation.idempotency_conflict";
    public const string IneligibleHandoff = "evaluation.ineligible_handoff";
    public const string BacklogLimitReached = "evaluation.backlog_limit_reached";
    public const string Denied = "evaluation.denied";
}

public static class EvaluationDelegationReference
{
    public static string Format(Guid delegationId)
    {
        if (delegationId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(delegationId));
        }

        return $"delegation.eval.{delegationId:N}";
    }
}

public sealed record AdmitEvaluationCommand(
    EvaluationRequest Request,
    Guid DelegationId,
    Guid ActorId,
    string ActorType,
    Guid CorrelationId,
    string SourceChannel,
    int OrganizationBacklogLimit,
    int MaxAttempts,
    int AttemptTimeoutSeconds,
    int BackoffSeconds);

public sealed record EvaluationAdmissionAuthority(
    string EvaluatorRegistryVersion,
    string LifecyclePolicyRef);

public sealed record EvaluationAdmissionResult(
    bool Succeeded,
    string OutcomeCode,
    Guid? RequestId,
    Guid? WorkId);

public interface IEvaluationAdmissionStore
{
    Task<EvaluationAdmissionResult> AdmitAsync(
        AdmitEvaluationCommand command,
        CancellationToken cancellationToken);
}

public sealed record EvaluationDurableWorkItem(
    Guid WorkId,
    Guid RequestId,
    Guid InvocationAttemptId,
    EvaluationOwnership Ownership,
    string FrozenInputDigest,
    Guid DelegationId,
    int AttemptCount,
    int MaxAttempts,
    int AttemptTimeoutSeconds,
    int BackoffSeconds,
    DateTimeOffset ClaimLeaseUntil);

public interface IEvaluationDurableWorkStore
{
    Task<EvaluationDurableWorkItem?> TryClaimAsync(
        Guid claimOwner,
        TimeSpan lease,
        int perOrganizationConcurrency,
        CancellationToken cancellationToken);

    Task<DateTimeOffset?> TryRenewAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        TimeSpan lease,
        CancellationToken cancellationToken);

    Task<bool> ReleaseForRetryAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        string failureCategory,
        CancellationToken cancellationToken);

    Task<bool> MarkExhaustedAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        string failureCategory,
        CancellationToken cancellationToken);

    Task<bool> MarkCompletedAsync(
        EvaluationDurableWorkItem work,
        Guid claimOwner,
        CancellationToken cancellationToken);
}
