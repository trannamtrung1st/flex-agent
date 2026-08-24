using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public static class DevelopmentAccommodationPolicy
{
    public static NormalizedAccommodationPolicy Create(Guid organizationId, BaselineTiming baseline, string environment)
    {
        var allowances = new Dictionary<string, RelativeAccommodationAllowance>(StringComparer.Ordinal)
        {
            [AccommodationDimensions.SubmissionDeadlineUtc] = new(true, 0, 14, 0, 30, AccommodationValueKinds.UtcInstant),
            [AccommodationDimensions.AttemptStartNotBeforeUtc] = new(true, -7, 0, -14, 0, AccommodationValueKinds.UtcInstant),
            [AccommodationDimensions.AttemptStartBeforeUtc] = new(true, 0, 7, 0, 30, AccommodationValueKinds.UtcInstant),
            [AccommodationDimensions.PerAttemptDurationSeconds] = new(true, 0, 3600, 0, 10800, AccommodationValueKinds.PositiveSeconds),
        };

        return AccommodationPolicyNormalizer.FromRelativeAllowances(
            organizationId,
            baseline.FrozenPolicy,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            null,
            environment,
            environmentEligible: !string.Equals(environment, "production", StringComparison.Ordinal)
                && !string.Equals(environment, "staging", StringComparison.Ordinal),
            baseline,
            allowances,
            [AccommodationReasonCategories.DevelopmentSynthetic],
            Guid.Parse("44444444-4444-4444-8444-444444444401"),
            false,
            true);
    }
}

public sealed class FixedAccommodationPolicyPort : IAccommodationPolicyPort
{
    public NormalizedAccommodationPolicy? Policy { get; set; }

    public Task<NormalizedAccommodationPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        BaselineTiming baseline,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<NormalizedAccommodationPolicy?>(
            Policy ?? DevelopmentAccommodationPolicy.Create(organizationId, baseline, "development"));
}
