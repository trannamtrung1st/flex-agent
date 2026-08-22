using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed record AssessmentActorContext(
    TrustedActor Actor,
    OrganizationScope Organization,
    string Relationship,
    AuthenticationStrength Strength,
    Guid CorrelationId,
    string SourceChannel);

public sealed record CreateAssessmentDraftCommand(
    AssessmentActorContext Actor,
    string Title,
    TaskBinding Task,
    TimingRules Timing,
    ExactSourceRef OrganizationPolicy,
    ExactSourceRef Agent,
    ExactSourceRef Harness,
    ExactSourceRef Workflow,
    ExactSourceRef AdaptiveFollowUp,
    ExactSourceRef Rubric,
    ExactSourceRef ModelDeployment,
    IReadOnlyList<ExactSourceRef> Knowledge,
    ExactSourceRef CapabilityProfile,
    ExactSourceRef ReviewRelease,
    string Environment);

public sealed record SaveAssessmentDraftCommand(
    AssessmentActorContext Actor,
    Guid ActivityId,
    long ExpectedRevisionNumber,
    AssessmentDraftContent Content,
    string Environment);

public sealed record CheckReadinessQuery(
    AssessmentActorContext Actor,
    Guid ActivityId,
    string Environment);

public sealed record ActivateCohortCommand(
    AssessmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    Guid ExpectedRevisionId,
    long ExpectedRevisionNumber,
    string IdempotencyKey,
    string TrustedCommandDigest,
    string Environment);

public sealed record ReconcileActivationQuery(
    AssessmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    string IdempotencyKey);

public sealed record ActivationOutcome(
    bool Succeeded,
    string OutcomeCode,
    Guid? ActivityId,
    Guid? CohortId,
    Guid? BaselineId,
    string? BaselineDigest,
    string? CohortState);

public interface IAssessmentActivationCoordinator
{
    Task<ActivationOutcome> ActivateAsync(
        ActivateCohortCommand command,
        CancellationToken cancellationToken = default);

    Task<ActivationOutcome> ReconcileAsync(
        ReconcileActivationQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record AssessmentActivationAttempt(
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    Guid AttemptId,
    Guid RequestedRevisionId,
    long RequestedRevisionNumber,
    Guid? AuthoritativeRevisionId,
    long? AuthoritativeRevisionNumber,
    string IdempotencyKey,
    string CommandDigest,
    string OutcomeCode,
    Guid? BaselineId,
    string? BaselineDigest,
    string? CohortState,
    Guid ActorId,
    Guid CorrelationId,
    string ActorType,
    string SourceChannel,
    Guid? AuthoritativeCohortId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    AuthorizationDecision? Authorization = null);

public interface IAssessmentActivationAttemptStore
{
    Task AcquireIdempotencyLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);

    Task<AssessmentActivationAttempt?> FindAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);

    Task<AssessmentActivationAttempt?> FindSuccessfulAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);

    Task InsertAsync(
        AssessmentActivationAttempt attempt,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);

    Task InsertRequestAuditAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        string outcome,
        string? reasonCode,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken,
        AuthorizationDecision? authorization = null);

    Task<string> BindCommandDigestAsync(
        Guid organizationId,
        Guid activityId,
        Guid requestedCohortId,
        string idempotencyKey,
        string commandDigest,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);
}

public sealed record AssessmentActorAuthorization(
    string Relationship,
    IReadOnlyList<string> PermittedActions);

public interface IAssessmentRelationshipResolver
{
    Task<AssessmentActorAuthorization> ResolveAsync(
        Guid actorId,
        Guid organizationId,
        CancellationToken cancellationToken);
}

public interface IAssessmentDraftHandler
{
    Task<AssessmentDecision<ActivityDraft>> CreateAsync(
        CreateAssessmentDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<AssessmentDecision<ActivityDraft>> SaveAsync(
        SaveAssessmentDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<AssessmentDecision<ReadinessResult>> CheckReadinessAsync(
        CheckReadinessQuery query,
        CancellationToken cancellationToken = default);

    Task<AssessmentDecision<IReadOnlyList<TrustedSourceDescriptor>>> ListSourceOptionsAsync(
        AssessmentActorContext actor,
        string environment,
        CancellationToken cancellationToken = default);

    Task<AssessmentDecision<IReadOnlyList<ActivityDraft>>> ListActivitiesAsync(
        AssessmentActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AssessmentDecision<ActivityDraft>> GetActivityAsync(
        AssessmentActorContext actor,
        Guid activityId,
        CancellationToken cancellationToken = default);
}

public static class AssessmentDraftProjection
{
    public static IReadOnlyList<string> PermittedActions(
        IReadOnlyList<string> grantedActions,
        bool hasActivatedCohort)
    {
        if (hasActivatedCohort)
        {
            return grantedActions.Contains("assessment.enrollment.assign", StringComparer.Ordinal)
                ? ["assign_participants"]
                : [];
        }

        var actions = new List<string>();
        if (grantedActions.Contains(AssessmentAuthorizationActions.SaveActivity, StringComparer.Ordinal))
        {
            actions.Add("save_draft");
        }

        if (grantedActions.Contains(AssessmentAuthorizationActions.CheckReadiness, StringComparer.Ordinal))
        {
            actions.Add("check_readiness");
        }

        if (grantedActions.Contains(AssessmentAuthorizationActions.ActivateCohort, StringComparer.Ordinal))
        {
            actions.Add("activate_cohort");
        }

        return actions;
    }
}

public interface IAssessmentSourceCatalog
{
    Task<IReadOnlyList<TrustedSourceDescriptor>> LoadExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustedSourceDescriptor>> ListSelectableAsync(
        Guid organizationId,
        string environment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustedSourceDescriptor>> LoadSelectableExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        string environment,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IAssessmentDevelopmentSourceSeeder
{
    void EnsureOrganization(Guid organizationId);
}

public interface IAssessmentSourceTransactionPort
{
    Task<IReadOnlyList<TrustedSourceDescriptor>> RevalidateExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustedSourceDescriptor>> LoadSelectableExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        string environment,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IAssessmentAuthorizationPort
{
    Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken = default);

    Task<AuthorizationDecision> ReauthorizeAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IAssessmentActivationTransaction
{
    bool AuditAccepted { get; set; }

    bool OutboxAccepted { get; set; }

    object? PersistenceContext { get; }
}

public interface IAssessmentActivationUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<IAssessmentActivationTransaction, Task<T>> action,
        CancellationToken cancellationToken = default);
}

public interface IActivationBaselineDigester
{
    AssessmentDecision<string> Digest(ActivationBaselineDocument document);
}

public interface IAssessmentClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemAssessmentClock : IAssessmentClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed record PersistedActivationBaseline(string ContentDigest, ActivationBaselineDocument Document);

public sealed record ActivatedCohortBindingSnapshot(
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    Guid BaselineId,
    string BaselineDigest,
    string CohortState,
    Guid TaskSourceId,
    Guid TaskVersionId,
    string TaskContentDigest,
    string ActivityTitle,
    string TaskTitle,
    string TimeZoneId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset DeadlineUtc,
    bool VerificationDegraded);

public interface IActivatedCohortBindingReader
{
    Task<ActivatedCohortBindingSnapshot?> GetActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        object? commitTransaction,
        CancellationToken cancellationToken = default);
}

public interface IAssessmentBaselineStore
{
    Task<PersistedActivationBaseline?> FindBoundAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken);

    Task InsertAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        ActivationBaselineDocument document,
        string contentDigest,
        IAssessmentActivationTransaction transaction,
        AssessmentActorContext actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken,
        AuthorizationDecision? authorization = null);
}
