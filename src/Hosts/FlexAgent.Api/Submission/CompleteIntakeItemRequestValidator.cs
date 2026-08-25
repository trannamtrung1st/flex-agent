using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

public static class CompleteIntakeItemRequestValidator
{
    public static bool IsValid(CompleteIntakeItemCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null
        && EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision)
        && !string.IsNullOrWhiteSpace(body.Content);
}
