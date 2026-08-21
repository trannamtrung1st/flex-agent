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
    ExactSourceRef ReviewRelease);

public sealed record SaveAssessmentDraftCommand(
    AssessmentActorContext Actor,
    Guid ActivityId,
    long ExpectedRevisionNumber,
    AssessmentDraftContent Content);

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
    Guid ExpectedRevisionId,
    long ExpectedRevisionNumber,
    string IdempotencyKey,
    string CommandDigest,
    string OutcomeCode,
    Guid? BaselineId,
    string? BaselineDigest,
    string? CohortState);

public interface IAssessmentActivationAttemptStore
{
    Task<AssessmentActivationAttempt?> FindAsync(
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
}

public interface IAssessmentSourceCatalog
{
    Task<IReadOnlyList<TrustedSourceDescriptor>> LoadExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
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

public interface IAssessmentBaselineStore
{
    Task InsertAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        ActivationBaselineDocument document,
        string contentDigest,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);
}
