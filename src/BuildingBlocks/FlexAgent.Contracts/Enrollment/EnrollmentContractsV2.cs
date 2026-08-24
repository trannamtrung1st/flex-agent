using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Enrollment;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GrantAccommodationCommandV2(
    string SchemaVersion,
    string Dimension,
    string RequestedValue,
    string ReasonCategory,
    DateTimeOffset? ExpiresAtUtc,
    bool FairnessException,
    long ExpectedRevision,
    string IdempotencyKey);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DecideAccommodationCommandV2(
    string SchemaVersion,
    bool Approve,
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
