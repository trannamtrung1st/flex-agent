using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class EffectiveTimingTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-09-01T09:00:00Z");
    private static readonly DateTimeOffset Deadline = DateTimeOffset.Parse("2026-09-10T17:00:00Z");
    private static readonly DateTimeOffset Ends = DateTimeOffset.Parse("2026-09-12T17:00:00Z");

    [Theory]
    [InlineData("2026-09-01T08:59:59Z", TimingEligibilityStates.TooEarly)]
    [InlineData("2026-09-01T09:00:00Z", TimingEligibilityStates.Open)]
    [InlineData("2026-09-10T16:59:59Z", TimingEligibilityStates.Open)]
    [InlineData("2026-09-10T17:00:00Z", TimingEligibilityStates.SubmissionClosed)]
    [InlineData("2026-09-12T16:59:59Z", TimingEligibilityStates.SubmissionClosed)]
    [InlineData("2026-09-12T17:00:00Z", TimingEligibilityStates.AttemptStartClosed)]
    public void Exclusive_windows_use_utc_instants_not_client_clocks(string now, string expected)
    {
        var timing = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [],
            DateTimeOffset.Parse(now));

        Assert.Equal(expected, timing.EligibilityState);
        Assert.Equal(Start, timing.Baseline.StartsAtUtc);
        Assert.Equal(Deadline, timing.EffectiveSubmissionExclusiveEndUtc);
        Assert.Equal(Ends, timing.EffectiveAttemptStartExclusiveEndUtc);
        Assert.Equal(3600, timing.EffectivePerAttemptDurationSeconds);
        Assert.Equal("America/New_York", timing.TimeZoneId);
        Assert.Null(timing.CurrentAccommodationId);
        Assert.True(timing.IsAuthoritativeEligibility);
    }

    [Fact]
    public void Per_attempt_duration_does_not_move_the_attempt_start_cutoff()
    {
        var longer = Accommodation.CreateGranted(
            Parent(),
            AccommodationDimensions.PerAttemptDurationSeconds,
            "7200",
            Frozen(),
            Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1).Value!;

        var timing = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [longer],
            Deadline.AddHours(-1));

        Assert.Equal(TimingEligibilityStates.Open, timing.EligibilityState);
        Assert.Equal(Ends, timing.EffectiveAttemptStartExclusiveEndUtc);
        Assert.Equal(7200, timing.EffectivePerAttemptDurationSeconds);
        Assert.Equal(longer.AccommodationId, timing.CurrentAccommodationId);
        Assert.Equal(AccommodationConsequenceCodes.DurationReplacement, timing.ParticipantConsequenceCode);
    }

    [Fact]
    public void Granted_deadline_replacement_changes_effective_timing_without_mutating_baseline()
    {
        var extended = Deadline.AddDays(2);
        var granted = Accommodation.CreateGranted(
            Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(extended),
            Frozen(),
            Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1).Value!;

        var timing = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [granted],
            Deadline.AddHours(1));

        Assert.Equal(TimingEligibilityStates.Open, timing.EligibilityState);
        Assert.Equal(extended, timing.EffectiveSubmissionExclusiveEndUtc);
        Assert.Equal(Deadline, timing.Baseline.DeadlineUtc);
        Assert.Equal(granted.AccommodationId, timing.CurrentAccommodationId);
    }

    [Fact]
    public void Current_policy_narrowing_removes_future_effect_without_rewriting_the_record()
    {
        var granted = Accommodation.CreateGranted(
            Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(10)),
            Frozen(),
            Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1).Value!;

        var timing = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            Current(Deadline.AddDays(1), Ends, 3600),
            [granted],
            Deadline.AddHours(1));

        Assert.Equal(TimingEligibilityStates.SubmissionClosed, timing.EligibilityState);
        Assert.Equal(Deadline, timing.EffectiveSubmissionExclusiveEndUtc);
        Assert.Null(timing.CurrentAccommodationId);
        Assert.Equal(AccommodationStates.Granted, granted.Status);
        Assert.Equal(Format(Deadline.AddDays(10)), granted.NormalizedValue);
    }

    [Fact]
    public void Derived_expiry_stops_effect_without_a_materialized_transition()
    {
        var expires = Deadline.AddHours(1);
        var granted = Accommodation.CreateGranted(
            Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(2)),
            Frozen(),
            Frozen(),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            expires,
            Guid.CreateVersion7(),
            1).Value!;

        var timing = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [granted],
            expires);

        Assert.Equal(TimingEligibilityStates.SubmissionClosed, timing.EligibilityState);
        Assert.Null(timing.CurrentAccommodationId);
        Assert.Equal(AccommodationStates.Granted, granted.Status);
        Assert.True(granted.IsExpiredAt(expires));
    }

    [Fact]
    public void Inactive_or_degraded_inputs_are_unavailable_and_not_authoritative()
    {
        var suspended = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Suspended,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [],
            Start.AddHours(1));
        Assert.Equal(TimingEligibilityStates.Unavailable, suspended.EligibilityState);
        Assert.False(suspended.IsAuthoritativeEligibility);

        var degraded = EffectiveTimingEvaluator.Evaluate(
            Baseline() with { VerificationDegraded = true },
            EnrollmentStates.Active,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [],
            Start.AddHours(1));
        Assert.Equal(TimingEligibilityStates.Unavailable, degraded.EligibilityState);

        var missingPolicy = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            null,
            [],
            Start.AddHours(1));
        Assert.Equal(TimingEligibilityStates.Unavailable, missingPolicy.EligibilityState);
    }

    [Fact]
    public void Pending_records_do_not_change_timing()
    {
        var pending = Accommodation.Request(
            Parent(),
            AccommodationDimensions.SubmissionDeadlineUtc,
            Format(Deadline.AddDays(10)),
            Frozen(),
            Current(Deadline.AddDays(1), Ends.AddDays(14), 7200),
            AccommodationReasonCategories.DevelopmentSynthetic,
            Start,
            null,
            Guid.CreateVersion7(),
            1,
            fairnessException: true).Value!;

        var timing = EffectiveTimingEvaluator.Evaluate(
            Baseline(),
            EnrollmentStates.Active,
            Current(Deadline.AddDays(14), Ends.AddDays(7), 7200),
            [pending],
            Deadline.AddHours(1));

        Assert.Equal(Deadline, timing.EffectiveSubmissionExclusiveEndUtc);
        Assert.Null(timing.CurrentAccommodationId);
        Assert.Equal(AccommodationStates.PendingApproval, pending.Status);
    }

    private static BaselineTiming Baseline() =>
        new(
            Start,
            Ends,
            Deadline,
            "America/New_York",
            2,
            3600,
            Frozen(),
            false);

    private static NormalizedAccommodationPolicy Current(
        DateTimeOffset maxDeadline,
        DateTimeOffset maxAttemptEnd,
        int maxDuration) =>
        AccommodationDomainTestsSupport.CurrentPolicy(maxDeadline, maxAttemptEnd, maxDuration);

    private static AccommodationParentBinding Parent() => AccommodationDomainTestsSupport.Parent();

    private static AccommodationPolicyIdentity Frozen() => AccommodationDomainTestsSupport.Frozen();

    private static string Format(DateTimeOffset value) => AccommodationDomainTestsSupport.FormatUtc(value);
}

internal static class AccommodationDomainTestsSupport
{
    public static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-09-01T09:00:00Z");
    public static readonly DateTimeOffset Deadline = DateTimeOffset.Parse("2026-09-10T17:00:00Z");
    public static readonly DateTimeOffset Ends = DateTimeOffset.Parse("2026-09-12T17:00:00Z");

    public static AccommodationParentBinding Parent() =>
        new(
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb0"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1"));

    public static AccommodationPolicyIdentity Frozen() =>
        new(
            Guid.Parse("22222222-2222-2222-2222-222222222201"),
            Guid.Parse("33333333-3333-3333-3333-333333333301"),
            new string('b', 64));

    public static NormalizedAccommodationPolicy CurrentPolicy(
        DateTimeOffset maxDeadline,
        DateTimeOffset maxAttemptEnd,
        int maxDuration) =>
        AccommodationPolicyNormalizer.FromAbsoluteRanges(
            Parent().OrganizationId,
            Frozen(),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            null,
            "development",
            true,
            new Dictionary<string, AccommodationDimensionBounds>(StringComparer.Ordinal)
            {
                [AccommodationDimensions.SubmissionDeadlineUtc] = Instant(Deadline, maxDeadline, Start, maxAttemptEnd),
                [AccommodationDimensions.AttemptStartNotBeforeUtc] = Instant(Start.AddDays(-7), Start, Start.AddDays(-14), Start),
                [AccommodationDimensions.AttemptStartBeforeUtc] = Instant(Ends, maxAttemptEnd, Ends, Ends.AddDays(30)),
                [AccommodationDimensions.PerAttemptDurationSeconds] = Duration(3600, maxDuration, 1, 14400),
            },
            [AccommodationReasonCategories.DevelopmentSynthetic],
            Guid.Parse("44444444-4444-4444-8444-444444444401"),
            false,
            true);

    public static string FormatUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

    public static AccommodationDimensionBounds InstantBounds(
        DateTimeOffset routineMin,
        DateTimeOffset routineMax,
        DateTimeOffset hardMin,
        DateTimeOffset hardMax) =>
        Instant(routineMin, routineMax, hardMin, hardMax);

    public static AccommodationDimensionBounds DurationBounds(int routineMin, int routineMax, int hardMin, int hardMax) =>
        Duration(routineMin, routineMax, hardMin, hardMax);

    private static AccommodationDimensionBounds Instant(
        DateTimeOffset routineMin,
        DateTimeOffset routineMax,
        DateTimeOffset hardMin,
        DateTimeOffset hardMax) =>
        new(true, AccommodationValueKinds.UtcInstant, FormatUtc(routineMin), FormatUtc(routineMax), FormatUtc(hardMin), FormatUtc(hardMax));

    private static AccommodationDimensionBounds Duration(int routineMin, int routineMax, int hardMin, int hardMax) =>
        new(
            true,
            AccommodationValueKinds.PositiveSeconds,
            routineMin.ToString(System.Globalization.CultureInfo.InvariantCulture),
            routineMax.ToString(System.Globalization.CultureInfo.InvariantCulture),
            hardMin.ToString(System.Globalization.CultureInfo.InvariantCulture),
            hardMax.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
