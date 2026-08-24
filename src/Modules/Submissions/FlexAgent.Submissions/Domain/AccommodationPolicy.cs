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

    public static DateTimeOffset CanonicalizeExpiry(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var truncated = utc.UtcTicks - (utc.UtcTicks % TimeSpan.TicksPerMicrosecond);
        return new DateTimeOffset(truncated, TimeSpan.Zero);
    }

    public static string FormatInstant(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static string FormatCanonicalInstant(DateTimeOffset value) =>
        CanonicalizeExpiry(value).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);

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

    public static NormalizedAccommodationPolicy? Intersect(
        NormalizedAccommodationPolicy frozen,
        NormalizedAccommodationPolicy current)
    {
        if (frozen.OrganizationId != current.OrganizationId)
        {
            return null;
        }

        var dimensions = new Dictionary<string, AccommodationDimensionBounds>(StringComparer.Ordinal);
        foreach (var dimension in AccommodationDimensions.All)
        {
            if (!frozen.Dimensions.TryGetValue(dimension, out var left)
                || !current.Dimensions.TryGetValue(dimension, out var right)
                || !string.Equals(left.ValueKind, right.ValueKind, StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryIntersectBounds(left, right, out var intersected))
            {
                continue;
            }

            dimensions[dimension] = intersected;
        }

        var reasons = frozen.ReasonCategories
            .Intersect(current.ReasonCategories, StringComparer.Ordinal)
            .ToArray();
        var fairnessRuleId = frozen.FairnessExceptionRuleId is not null && current.FairnessExceptionRuleId is not null
            ? current.FairnessExceptionRuleId
            : null;

        return FromAbsoluteRanges(
            current.OrganizationId,
            current.Identity,
            current.EffectiveFromUtc,
            current.EffectiveUntilUtc,
            current.Environment,
            frozen.EnvironmentEligible && current.EnvironmentEligible,
            dimensions,
            reasons,
            fairnessRuleId,
            frozen.RequiresExpiry || current.RequiresExpiry,
            frozen.SyntheticDevelopmentOnly || current.SyntheticDevelopmentOnly);
    }

    public static NormalizedAccommodationPolicy? EffectiveBounds(
        AccommodationPolicyIdentity frozenIdentity,
        NormalizedAccommodationPolicy? frozenSnapshot,
        NormalizedAccommodationPolicy? current)
    {
        if (current is null)
        {
            return null;
        }

        if (frozenSnapshot is not null)
        {
            if (frozenSnapshot.Identity != frozenIdentity
                || frozenSnapshot.OrganizationId != current.OrganizationId)
            {
                return null;
            }

            return Intersect(frozenSnapshot, current);
        }

        if (!current.SyntheticDevelopmentOnly || current.Identity != frozenIdentity)
        {
            return null;
        }

        return Intersect(current, current);
    }

    private static bool TryIntersectBounds(
        AccommodationDimensionBounds left,
        AccommodationDimensionBounds right,
        out AccommodationDimensionBounds intersected)
    {
        intersected = left;
        if (left.ValueKind == AccommodationValueKinds.PositiveSeconds
            && TryParseDuration(left.RoutineMin, out var leftRoutineMin)
            && TryParseDuration(left.RoutineMax, out var leftRoutineMax)
            && TryParseDuration(left.HardMin, out var leftHardMin)
            && TryParseDuration(left.HardMax, out var leftHardMax)
            && TryParseDuration(right.RoutineMin, out var rightRoutineMin)
            && TryParseDuration(right.RoutineMax, out var rightRoutineMax)
            && TryParseDuration(right.HardMin, out var rightHardMin)
            && TryParseDuration(right.HardMax, out var rightHardMax))
        {
            var hardMin = Math.Max(leftHardMin, rightHardMin);
            var hardMax = Math.Min(leftHardMax, rightHardMax);
            if (hardMin > hardMax)
            {
                return false;
            }

            var routineMin = Math.Max(leftRoutineMin, rightRoutineMin);
            var routineMax = Math.Min(leftRoutineMax, rightRoutineMax);

            intersected = new AccommodationDimensionBounds(
                left.Enabled && right.Enabled,
                left.ValueKind,
                Duration(routineMin),
                Duration(routineMax),
                Duration(hardMin),
                Duration(hardMax));
            return true;
        }

        if (TryParseInstant(left.RoutineMin, out var leftRoutineMinAt)
            && TryParseInstant(left.RoutineMax, out var leftRoutineMaxAt)
            && TryParseInstant(left.HardMin, out var leftHardMinAt)
            && TryParseInstant(left.HardMax, out var leftHardMaxAt)
            && TryParseInstant(right.RoutineMin, out var rightRoutineMinAt)
            && TryParseInstant(right.RoutineMax, out var rightRoutineMaxAt)
            && TryParseInstant(right.HardMin, out var rightHardMinAt)
            && TryParseInstant(right.HardMax, out var rightHardMaxAt))
        {
            var hardMin = leftHardMinAt > rightHardMinAt ? leftHardMinAt : rightHardMinAt;
            var hardMax = leftHardMaxAt < rightHardMaxAt ? leftHardMaxAt : rightHardMaxAt;
            if (hardMin > hardMax)
            {
                return false;
            }

            var routineMin = leftRoutineMinAt > rightRoutineMinAt ? leftRoutineMinAt : rightRoutineMinAt;
            var routineMax = leftRoutineMaxAt < rightRoutineMaxAt ? leftRoutineMaxAt : rightRoutineMaxAt;

            intersected = new AccommodationDimensionBounds(
                left.Enabled && right.Enabled,
                left.ValueKind,
                FormatInstant(routineMin),
                FormatInstant(routineMax),
                FormatInstant(hardMin),
                FormatInstant(hardMax));
            return true;
        }

        return false;
    }

    private static string Duration(int seconds) =>
        Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture);
}
