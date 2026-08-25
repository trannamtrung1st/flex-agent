using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

internal static class EnrollmentAssignRequestValidator
{
    public static bool IsValid(EnrollmentAssignCommandV1? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v1", StringComparison.Ordinal)
        && body.ParticipantActorId != Guid.Empty
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null;
}
