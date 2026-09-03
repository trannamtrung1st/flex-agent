using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed record GrantAccommodationCommand(
    EnrollmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    Guid EnrollmentId,
    string Dimension,
    string RequestedValue,
    string ReasonCategory,
    DateTimeOffset? ExpiresAtUtc,
    bool FairnessException,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record DecideAccommodationCommand(
    EnrollmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    Guid EnrollmentId,
    Guid AccommodationId,
    bool Approve,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record RevokeAccommodationCommand(
    EnrollmentActorContext Actor,
    Guid ActivityId,
    Guid CohortId,
    Guid EnrollmentId,
    Guid AccommodationId,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record AccommodationMutationOutcome(
    bool Succeeded,
    string OutcomeCode,
    Guid? AccommodationId,
    Guid? EnrollmentId,
    string? Status,
    long? Revision,
    IReadOnlyList<string> PermittedActions);

public sealed record TimingWindowProjection(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset ExclusiveEndsAtUtc);

public sealed record EnrollmentTimingDetail(
    EnrollmentSummary Summary,
    BaselineTiming Baseline,
    EffectiveTiming Timing,
    IReadOnlyList<Accommodation> History,
    IReadOnlyList<string> PermittedAccommodationDimensions,
    IReadOnlyList<string> PermittedReasonCategories,
    bool PolicyAvailable);

public sealed record AssignmentTimingSummary(
    AssignmentSummary Assignment,
    EffectiveTiming? Timing,
    string ParticipantConsequenceCode);

public interface IAccommodationCoordinator
{
    Task<AccommodationMutationOutcome> GrantAsync(
        GrantAccommodationCommand command,
        CancellationToken cancellationToken = default);

    Task<AccommodationMutationOutcome> DecideAsync(
        DecideAccommodationCommand command,
        CancellationToken cancellationToken = default);

    Task<AccommodationMutationOutcome> RevokeAsync(
        RevokeAccommodationCommand command,
        CancellationToken cancellationToken = default);
}

public interface IEnrollmentTimingQueryService
{
    Task<EnrollmentDecision<EnrollmentTimingDetail>> GetEnrollmentTimingAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<EnrollmentDecision<AssignmentTimingSummary>> GetMyWorkTimingAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<EffectiveTiming?> ComposeAuthoritativeInTransactionAsync(
        Enrollment enrollment,
        IEnrollmentTransaction transaction,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public interface IAccommodationPolicyPort
{
    Task<NormalizedAccommodationPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        BaselineTiming baseline,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public interface IAccommodationStore
{
    Task<Accommodation?> FindAsync(
        Guid organizationId,
        Guid accommodationId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Accommodation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken);

    Task InsertAsync(
        Accommodation accommodation,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Accommodation accommodation,
        string? priorStatus,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken);
}
