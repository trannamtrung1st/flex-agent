using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Enrollment;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EnrollmentAssignCommandV1(
    string SchemaVersion,
    Guid ParticipantActorId,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EnrollmentLifecycleCommandV1(
    string SchemaVersion,
    string ReasonCode,
    long ExpectedRevision,
    string IdempotencyKey);

public sealed record EnrollmentMutationOutcomeV1(
    string SchemaVersion,
    bool Succeeded,
    string OutcomeCode,
    Guid? EnrollmentId,
    string? Status,
    long? Revision,
    string? Visibility,
    IReadOnlyList<string> PermittedActions);

public sealed record MyWorkAssignmentV1(
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
