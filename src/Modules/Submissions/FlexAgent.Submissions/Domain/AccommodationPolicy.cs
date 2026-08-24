using System.Globalization;

namespace FlexAgent.Submissions.Domain;

public sealed record AccommodationPolicyIdentity(Guid PolicyId, Guid VersionId, string Digest);

public sealed record AccommodationParentBinding(
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    Guid BaselineId,
    Guid EnrollmentId,
    Guid ParticipantActorId);

public sealed record AccommodationDimensionBounds(
    bool Enabled,
    string ValueKind,
    string RoutineMin,
    string RoutineMax,
    string HardMin,
    string HardMax);

public sealed record RelativeAccommodationAllowance(
    bool Enabled,
    int RoutineMinOffset,
    int RoutineMaxOffset,
    int HardMinOffset,
    int HardMaxOffset,
    string ValueKind);

public sealed record NormalizedAccommodationPolicy(
    Guid OrganizationId,
    AccommodationPolicyIdentity Identity,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveUntilUtc,
    string Environment,
    bool EnvironmentEligible,
    IReadOnlyDictionary<string, AccommodationDimensionBounds> Dimensions,
    IReadOnlyList<string> ReasonCategories,
    Guid? FairnessExceptionRuleId,
    bool RequiresExpiry,
    bool SyntheticDevelopmentOnly);

public static class AccommodationPolicyNormalizer
{
    public static NormalizedAccommodationPolicy FromAbsoluteRanges(
        Guid organizationId,
        AccommodationPolicyIdentity identity,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveUntilUtc,
        string environment,
        bool environmentEligible,
        IReadOnlyDictionary<string, AccommodationDimensionBounds> dimensions,
        IReadOnlyList<string> reasonCategories,
        Guid? fairnessExceptionRuleId,
        bool requiresExpiry,
        bool syntheticDevelopmentOnly) =>
        new(
            organizationId,
            identity,
            effectiveFromUtc,
            effectiveUntilUtc,
            environment,
            environmentEligible,
            dimensions,
            reasonCategories,
            fairnessExceptionRuleId,
            requiresExpiry,
            syntheticDevelopmentOnly);

    public static NormalizedAccommodationPolicy FromRelativeAllowances(
        Guid organizationId,
        AccommodationPolicyIdentity identity,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveUntilUtc,
        string environment,
        bool environmentEligible,
        BaselineTiming baseline,
        IReadOnlyDictionary<string, RelativeAccommodationAllowance> allowances,
        IReadOnlyList<string> reasonCategories,
        Guid? fairnessExceptionRuleId,
        bool requiresExpiry,
        bool syntheticDevelopmentOnly)
    {
        var dimensions = new Dictionary<string, AccommodationDimensionBounds>(StringComparer.Ordinal);
        foreach (var (dimension, allowance) in allowances)
        {
            dimensions[dimension] = Normalize(dimension, allowance, baseline);
        }

        return FromAbsoluteRanges(
            organizationId,
            identity,
            effectiveFromUtc,
            effectiveUntilUtc,
            environment,
            environmentEligible,
            dimensions,
            reasonCategories,
            fairnessExceptionRuleId,
            requiresExpiry,
            syntheticDevelopmentOnly);
    }

    public static bool TryParseInstant(string value, out DateTimeOffset instant) =>
        DateTimeOffset.TryParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out instant);

    public static bool TryParseDuration(string value, out int seconds)
    {
        seconds = 0;
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out seconds) && seconds > 0;
    }

    public static string FormatInstant(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static AccommodationDimensionBounds Normalize(
        string dimension,
        RelativeAccommodationAllowance allowance,
        BaselineTiming baseline)
    {
        if (allowance.ValueKind == AccommodationValueKinds.PositiveSeconds)
        {
            var origin = baseline.PerAttemptDurationSeconds ?? 0;
            return new AccommodationDimensionBounds(
                allowance.Enabled,
                AccommodationValueKinds.PositiveSeconds,
                Duration(origin + allowance.RoutineMinOffset),
                Duration(origin + allowance.RoutineMaxOffset),
                Duration(Math.Max(1, origin + allowance.HardMinOffset)),
                Duration(origin + allowance.HardMaxOffset));
        }

        var originInstant = dimension switch
        {
            AccommodationDimensions.SubmissionDeadlineUtc => baseline.DeadlineUtc,
            AccommodationDimensions.AttemptStartNotBeforeUtc => baseline.StartsAtUtc,
            AccommodationDimensions.AttemptStartBeforeUtc => baseline.EndsAtUtc,
            _ => baseline.StartsAtUtc,
        };
        return new AccommodationDimensionBounds(
            allowance.Enabled,
            AccommodationValueKinds.UtcInstant,
            FormatInstant(originInstant.AddDays(allowance.RoutineMinOffset)),
            FormatInstant(originInstant.AddDays(allowance.RoutineMaxOffset)),
            FormatInstant(originInstant.AddDays(allowance.HardMinOffset)),
            FormatInstant(originInstant.AddDays(allowance.HardMaxOffset)));
    }

    private static string Duration(int seconds) =>
        Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture);
}
