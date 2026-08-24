using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Enrollment;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GrantAccommodationCommandV2(
    string SchemaVersion,
    string Dimension,
    string RequestedValue,
    string ReasonCategory,
    DateTimeOffset? ExpiresAtUtc,
    [property: JsonRequired] bool FairnessException,
    long ExpectedRevision,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DecideAccommodationCommandV2(
    string SchemaVersion,
    [property: JsonRequired] bool Approve,
    long ExpectedRevision,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RevokeAccommodationCommandV2(
    string SchemaVersion,
    long ExpectedRevision,
    string IdempotencyKey);

public sealed record AccommodationMutationOutcomeV2(
    string SchemaVersion,
    bool Succeeded,
    string OutcomeCode,
    Guid? AccommodationId,
    Guid? EnrollmentId,
    string? Status,
    long? Revision,
    IReadOnlyList<string> PermittedActions);

public sealed record EnrollmentTimingEnrollmentV2(
    Guid EnrollmentId,
    string Status,
    long Revision,
    string Visibility,
    IReadOnlyList<string> PermittedActions);

public sealed record TimingBaselineV2(
    string StartsAtUtc,
    string EndsAtUtc,
    string DeadlineUtc,
    string TimeZoneId,
    int AttemptLimit,
    int? PerAttemptDurationSeconds);

public sealed record TimingEffectiveWindowV2(
    string SubmissionStartsAtUtc,
    string SubmissionExclusiveEndUtc,
    string AttemptStartUtc,
    string AttemptStartExclusiveEndUtc,
    int? PerAttemptDurationSeconds,
    string EvaluatedAtUtc,
    string EligibilityState,
    bool IsAuthoritative,
    string TimeZoneId,
    string ParticipantConsequenceCode);

public sealed record CurrentAccommodationEffectV2(
    Guid AccommodationId,
    string Dimension,
    string ConsequenceCode);

public sealed record AccommodationHistoryItemV2(
    Guid AccommodationId,
    string Dimension,
    string Status,
    string NormalizedValue,
    string ReasonCategory,
    bool FairnessException,
    long Revision,
    string CreatedAtUtc,
    string? DecidedAtUtc,
    string? ExpiresAtUtc);

public sealed record EnrollmentTimingV2(
    string SchemaVersion,
    EnrollmentTimingEnrollmentV2 Enrollment,
    TimingBaselineV2 Baseline,
    TimingEffectiveWindowV2 Effective,
    IReadOnlyList<CurrentAccommodationEffectV2> CurrentAccommodations,
    bool PolicyAvailable,
    IReadOnlyList<string> PermittedDimensions,
    IReadOnlyList<string> PermittedReasonCategories,
    IReadOnlyList<AccommodationHistoryItemV2> History);

public sealed record MyWorkTimingAssignmentV2(
    Guid EnrollmentId,
    string Status,
    string Visibility,
    string? ActivityTitle,
    string? TaskTitle,
    string? TimeZoneId,
    string? StartsAtUtc,
    string? EndsAtUtc,
    string? DeadlineUtc,
    bool SummaryAvailable,
    IReadOnlyList<string> PermittedActions);

public sealed record MyWorkTimingV2(
    string SchemaVersion,
    MyWorkTimingAssignmentV2 Assignment,
    TimingEffectiveWindowV2? Effective,
    string ParticipantConsequenceCode);
