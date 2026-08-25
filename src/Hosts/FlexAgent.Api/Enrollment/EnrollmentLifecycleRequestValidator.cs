using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

internal static class EnrollmentLifecycleRequestValidator
{
    public static bool IsValid(EnrollmentLifecycleCommandV1? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v1", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null
        && !string.IsNullOrWhiteSpace(body.ReasonCode)
        && body.ExpectedRevision >= 1;
}
