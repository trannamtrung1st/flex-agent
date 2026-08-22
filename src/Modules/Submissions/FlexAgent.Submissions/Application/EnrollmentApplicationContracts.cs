using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed record EnrollmentActorContext(
    TrustedActor Actor,
    OrganizationScope Organization,
    string Relationship,
    AuthenticationStrength Strength,
    Guid CorrelationId,
    string SourceChannel,
    IReadOnlyList<string> GrantedActions,
    Guid ApplicationSessionId);

public sealed record AssignEnrollmentCommand(
    EnrollmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    Guid ParticipantActorId,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record EnrollmentLifecycleCommand(
    EnrollmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    Guid EnrollmentId,
    string OperationKind,
    string ReasonCode,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record EnrollmentMutationOutcome(
    bool Succeeded,
    string OutcomeCode,
    Guid? EnrollmentId,
    string? Status,
    long? Revision,
    string? Visibility,
    IReadOnlyList<string> PermittedActions);

public sealed record EnrollmentCandidate(
    Guid ActorId,
    string DisplayLabel);

public sealed record EnrollmentSummary(
    Guid EnrollmentId,
    Guid ParticipantActorId,
    string DisplayLabel,
    string Status,
    long Revision,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Visibility,
    IReadOnlyList<string> PermittedActions);

public sealed record EnrollmentDetail(
    EnrollmentSummary Summary,
    IReadOnlyList<EnrollmentHistoryItem> History);

public sealed record EnrollmentHistoryItem(
    long Sequence,
    string PriorStatus,
    string NewStatus,
    string ReasonCode,
    DateTimeOffset OccurredAtUtc);

public sealed record AssignmentSummary(
    Guid EnrollmentId,
    string Status,
    string Visibility,
    string? ActivityTitle,
    string? TaskTitle,
    string? TimeZoneId,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? DeadlineUtc,
    bool SummaryAvailable,
    IReadOnlyList<string> PermittedActions);

public sealed record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

public sealed record ActivatedCohortBinding(
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
    Guid LifecyclePolicyId,
    int LifecyclePolicyVersion,
    bool VerificationDegraded);

public interface IEnrollmentCoordinator
{
    Task<EnrollmentMutationOutcome> AssignAsync(
        AssignEnrollmentCommand command,
        CancellationToken cancellationToken = default);

    Task<EnrollmentMutationOutcome> MutateAsync(
        EnrollmentLifecycleCommand command,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentQueryService
{
    Task<EnrollmentDecision<CursorPage<EnrollmentCandidate>>> ListCandidatesAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        string? prefix,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<EnrollmentDecision<CursorPage<EnrollmentSummary>>> ListEnrollmentsAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<EnrollmentDecision<EnrollmentDetail>> GetEnrollmentAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<EnrollmentDecision<CursorPage<AssignmentSummary>>> ListMyWorkAsync(
        EnrollmentActorContext actor,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<EnrollmentDecision<AssignmentSummary>> GetMyWorkAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentSessionPort
{
    Task<bool> RevalidateLiveAsync(
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmLiveAsync(
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentAuthorizationPort
{
    Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken = default);

    Task<AuthorizationDecision> ReauthorizeAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IActivatedCohortPort
{
    Task<ActivatedCohortBinding?> FindActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken = default);

    Task<ActivatedCohortBinding?> RevalidateAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentCandidatePort
{
    Task<CursorPage<EnrollmentCandidate>> ListEligibleAsync(
        Guid organizationId,
        string? prefix,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<EnrollmentCandidate?> RevalidateEligibleAsync(
        Guid organizationId,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<string?> DisplayLabelAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentStore
{
    Task<Enrollment?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken);

    Task<Enrollment?> FindLiveForParticipantAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantActorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task InsertAsync(
        Enrollment enrollment,
        EnrollmentEvent enrollmentEvent,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Enrollment enrollment,
        EnrollmentEvent enrollmentEvent,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EnrollmentHistoryItem>> ListHistoryAsync(
        Guid organizationId,
        Guid enrollmentId,
        CancellationToken cancellationToken);

    Task<CursorPage<Enrollment>> ListForCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        DateTimeOffset? afterTime,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken);

    Task<CursorPage<Enrollment>> ListCurrentForParticipantAsync(
        Guid organizationId,
        Guid participantActorId,
        DateTimeOffset? afterTime,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken);
}

public interface IEnrollmentOperationStore
{
    Task AcquireLockAsync(
        Guid organizationId,
        Guid actorId,
        string operationKind,
        Guid resourceId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task AcquireLiveParticipantLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid participantActorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task<EnrollmentOperation?> FindAsync(
        Guid organizationId,
        Guid actorId,
        string operationKind,
        Guid resourceId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task InsertAsync(
        EnrollmentOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);
}

public interface IEnrollmentAuditPort
{
    Task WriteRequiredDurableAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        string outcome,
        string? reasonCode,
        AuthorizationDecision? authorization,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task WriteAvailabilityAsync(
        Enrollment enrollment,
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);
}

public interface IEnrollmentTransaction
{
    bool AuditAccepted { get; set; }

    bool OutboxAccepted { get; set; }

    object CommitHandle { get; }
}

public interface IEnrollmentUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        EnrollmentActorContext actor,
        Func<IEnrollmentTransaction, Task<T>> action,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemEnrollmentClock : IEnrollmentClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
