using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

internal static class IntakeRevisionRequestValidator
{
    public static bool IsValid(IntakeRevisionCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null
        && EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision);
}
