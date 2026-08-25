using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

internal static class GrantAccommodationRequestValidator
{
    public static bool IsValid(GrantAccommodationCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null
        && EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision)
        && !string.IsNullOrWhiteSpace(body.Dimension)
        && !string.IsNullOrWhiteSpace(body.RequestedValue)
        && !string.IsNullOrWhiteSpace(body.ReasonCategory);
}
