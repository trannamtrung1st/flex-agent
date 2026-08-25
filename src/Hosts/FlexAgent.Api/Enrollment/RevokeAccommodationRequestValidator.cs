using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

public static class RevokeAccommodationRequestValidator
{
    public static bool IsValid(RevokeAccommodationCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null
        && EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision);
}
