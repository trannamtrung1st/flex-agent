using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

internal static class BeginIntakeRequestValidator
{
    public static bool IsValid(BeginIntakeCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null;
}
