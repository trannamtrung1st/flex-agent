using FlexAgent.AssessmentConfiguration.Application;

namespace FlexAgent.Api;

internal static class AssessmentReconcileQueryValidator
{
    public static bool IsValid(string? idempotencyKey) =>
        !string.IsNullOrWhiteSpace(idempotencyKey)
        && AssessmentIdempotencyKey.Validate(idempotencyKey) is null;
}
