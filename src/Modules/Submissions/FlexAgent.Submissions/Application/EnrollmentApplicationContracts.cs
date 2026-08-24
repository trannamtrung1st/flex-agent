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
    bool VerificationDegraded,
    int AttemptLimit = 1,
    int? PerAttemptDurationSeconds = null,
    Guid FrozenPolicySourceId = default,
    Guid FrozenPolicyVersionId = default,
    string FrozenPolicyDigest = "",
    NormalizedAccommodationPolicy? FrozenAccommodationPolicy = null);

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
        Guid? afterActorId,
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

public enum EnrollmentSharedAdmissionDecision
{
    Permitted = 0,
    Exhausted = 1,
    Unavailable = 2,
}

public readonly record struct EnrollmentSharedAdmissionResult(
    EnrollmentSharedAdmissionDecision Decision,
    int RetryAfterSeconds)
{
    public static EnrollmentSharedAdmissionResult Permitted() =>
        new(EnrollmentSharedAdmissionDecision.Permitted, 0);

    public static EnrollmentSharedAdmissionResult Exhausted(int retryAfterSeconds) =>
        new(EnrollmentSharedAdmissionDecision.Exhausted, Math.Max(1, retryAfterSeconds));

    public static EnrollmentSharedAdmissionResult Unavailable() =>
        new(EnrollmentSharedAdmissionDecision.Unavailable, 0);
}

public sealed record EnrollmentSharedAdmissionSettings(
    int ReadPermitLimit,
    int MutationPermitLimit,
    int WindowSeconds,
    int PolicyRevision,
    TimeSpan Timeout,
    int CleanupBatchSize)
{
    public static EnrollmentSharedAdmissionSettings FromDefaults() =>
        new(
            EnrollmentRequestLimitDefaults.ReadPermitLimit,
            EnrollmentRequestLimitDefaults.MutationPermitLimit,
            EnrollmentRequestLimitDefaults.WindowSeconds,
            EnrollmentRequestLimitDefaults.PolicyRevision,
            TimeSpan.FromMilliseconds(EnrollmentRequestLimitDefaults.AdmissionTimeoutMilliseconds),
            EnrollmentRequestLimitDefaults.CleanupBatchSize);
}

public interface IEnrollmentSharedAdmissionPort
{
    Task<EnrollmentSharedAdmissionResult> AcquireAsync(
        Guid organizationId,
        Guid actorId,
        string surface,
        CancellationToken cancellationToken = default);

    Task<bool> PolicyMatchesAsync(CancellationToken cancellationToken = default);
}

public static class EnrollmentTelemetryLabels
{
    public const string Operation = "operation";
    public const string Outcome = "outcome";
    public const string Surface = "surface";
    public const string Decision = "decision";

    public const string Succeeded = "succeeded";
    public const string Denied = "denied";
    public const string Conflict = "conflict";
    public const string Unavailable = "unavailable";
    public const string Invalid = "invalid";
    public const string Permitted = "permitted";
    public const string Limited = "limited";

    public static readonly HashSet<string> AllowedKeys = new(StringComparer.Ordinal)
    {
        Operation,
        Outcome,
        Surface,
        Decision,
    };

    public static readonly HashSet<string> AllowedValues = new(StringComparer.Ordinal)
    {
        EnrollmentOperationKinds.Assign,
        EnrollmentOperationKinds.Suspend,
        EnrollmentOperationKinds.Restore,
        EnrollmentOperationKinds.Close,
        EnrollmentOperationKinds.Revoke,
        AccommodationOperationKinds.Grant,
        AccommodationOperationKinds.Decide,
        AccommodationOperationKinds.Revoke,
        EnrollmentRequestSurfaces.Read,
        EnrollmentRequestSurfaces.Mutation,
        Succeeded,
        Denied,
        Conflict,
        Unavailable,
        Invalid,
        Permitted,
        Limited,
        EnrollmentFailureCodes.RateLimited,
    };

    public static string ClassifyMutation(bool succeeded, string outcomeCode)
    {
        if (succeeded)
        {
            return Succeeded;
        }

        return outcomeCode switch
        {
            EnrollmentFailureCodes.Denied or EnrollmentFailureCodes.Ineligible => Denied,
            EnrollmentFailureCodes.Conflict
                or EnrollmentFailureCodes.IdempotencyConflict
                or EnrollmentFailureCodes.StaleRevision
                or EnrollmentFailureCodes.Terminal => Conflict,
            EnrollmentFailureCodes.AuditUnavailable
                or EnrollmentFailureCodes.Unavailable
                or EnrollmentFailureCodes.MissingLifecyclePolicy => Unavailable,
            _ => Invalid,
        };
    }
}

public interface IEnrollmentTelemetry
{
    void RecordMutation(string operationKind, string outcomeClass, TimeSpan duration);

    void RecordRequestLimit(string surface, string decision);
}

public sealed class NullEnrollmentTelemetry : IEnrollmentTelemetry
{
    public static NullEnrollmentTelemetry Instance { get; } = new();

    public void RecordMutation(string operationKind, string outcomeClass, TimeSpan duration)
    {
    }

    public void RecordRequestLimit(string surface, string decision)
    {
    }
}

public sealed class RecordingEnrollmentTelemetry : IEnrollmentTelemetry
{
    private readonly List<IReadOnlyDictionary<string, string>> _points = [];

    public IReadOnlyList<IReadOnlyDictionary<string, string>> Points => _points;

    public void RecordMutation(string operationKind, string outcomeClass, TimeSpan duration) =>
        _points.Add(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EnrollmentTelemetryLabels.Operation] = operationKind,
            [EnrollmentTelemetryLabels.Outcome] = outcomeClass,
        });

    public void RecordRequestLimit(string surface, string decision) =>
        _points.Add(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EnrollmentTelemetryLabels.Surface] = surface,
            [EnrollmentTelemetryLabels.Decision] = decision,
        });
}
