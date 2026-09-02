using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed record AttemptReadinessProjection(
    Guid EnrollmentId,
    string ReadinessState,
    int NextOrdinal,
    int RemainingEntitlement,
    string EntitlementSource,
    int BaselineAttemptLimit,
    Guid? ActiveAttemptId,
    Guid? ActiveSessionId,
    string StartCommandDigest,
    IReadOnlyList<AcceptedVersionSummary> BoundVersionCandidates,
    IReadOnlyList<AttemptHistoryItem> History,
    IReadOnlyList<RequiredNoticeProjection> RequiredNotices,
    IReadOnlyList<string> PermittedActions);

public sealed record AttemptHistoryItem(
    Guid AttemptId,
    int Ordinal,
    string Status,
    bool Consumed,
    Guid? SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? TerminalAtUtc,
    string? TerminalReasonCategory);

public sealed record RequiredNoticeProjection(
    Guid NoticeId,
    string NoticeType,
    string RequiredOutcome,
    string ProtectedContentRef,
    Guid SourceVersionId,
    string ContentDigest,
    Guid SourceId);

public sealed record AcknowledgeAttemptNoticeCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    Guid NoticeId,
    Guid SourceVersionId,
    string Outcome,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record AcknowledgmentMutationOutcome(
    bool Succeeded,
    string OutcomeCode,
    Guid? RecordId,
    string? Outcome);

public sealed record StartAttemptCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record StartAttemptOutcome(
    bool Succeeded,
    string OutcomeCode,
    string? ReadinessState,
    Guid? AttemptId,
    int? Ordinal,
    Guid? SessionId,
    int RemainingEntitlement,
    IReadOnlyList<string> PermittedActions);

public interface IAttemptReadinessQuery
{
    Task<QueryResult<AttemptReadinessProjection>> GetAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);
}

public interface IAttemptAcknowledgmentCoordinator
{
    Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        CancellationToken cancellationToken = default);
}

public interface IAttemptStartCoordinator
{
    Task<StartAttemptOutcome> StartAsync(
        StartAttemptCommand command,
        CancellationToken cancellationToken = default);

    Task<StartAttemptOutcome> ReconcileAsync(
        StartAttemptCommand command,
        CancellationToken cancellationToken = default);
}

public interface IAttemptStore
{
    Task<IReadOnlyList<Attempt>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task InsertAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<Attempt?> FindByIdAsync(
        Guid organizationId,
        Guid attemptId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task UpdateTerminalAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IStartOperationStore
{
    Task AcquireLockAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<StartOperation?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StartOperation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        StartOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IRetryEntitlementReader
{
    Task<IReadOnlyList<RetryEntitlementFact>> ListUnusedAsync(
        Guid organizationId,
        Guid enrollmentId,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public interface IParticipantNoticePort
{
    Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public interface IAcknowledgmentLifecyclePort
{
    Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        RequiredNoticeProjection notice,
        object commitTransaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrentAcknowledgmentFact>> ListCurrentAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        IReadOnlyList<RequiredNoticeProjection> notices,
        object commitTransaction,
        CancellationToken cancellationToken = default);

    Task<string?> BindToAttemptAsync(
        IReadOnlyList<CurrentAcknowledgmentFact> records,
        Guid attemptId,
        Guid enrollmentId,
        Guid participantActorId,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}

public sealed record CurrentAcknowledgmentFact(
    Guid RecordId,
    Guid EnrollmentId,
    Guid ParticipantActorId,
    Guid NoticeId,
    Guid SourceVersionId,
    string ContentDigest,
    string Outcome,
    DateTimeOffset RecordedAtUtc,
    Guid? BoundAttemptId);

public sealed record SessionStartCommitRequest(
    Guid AttemptId,
    Guid SessionId,
    Guid ConfigurationId,
    Guid ManifestId,
    SubmissionParentScope Scope,
    IReadOnlyList<AttemptSubmissionBinding> SubmissionBindings,
    DateTimeOffset StartedAtUtc);

public sealed record SessionStartCommitResult(
    bool Succeeded,
    string OutcomeCode,
    string? ConfigurationDigest,
    string? ManifestDigest);

public interface ISessionStartCommitPort
{
    bool CanCommit { get; }

    Task<SessionStartCommitResult> CommitActiveAsync(
        SessionStartCommitRequest request,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}

public interface IAttemptTerminalMappingPort
{
    Task MapTerminalAsync(
        Guid organizationId,
        Guid attemptId,
        string terminalStatus,
        string reasonCategory,
        DateTimeOffset terminalAtUtc,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}
