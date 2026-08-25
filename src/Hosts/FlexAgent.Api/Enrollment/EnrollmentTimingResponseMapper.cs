using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Api;

internal static class EnrollmentTimingResponseMapper
{
    public static EnrollmentTimingV2 MapEnrollmentTiming(EnrollmentTimingDetail detail) =>
        new(
            "v2",
            new EnrollmentTimingEnrollmentV2(
                detail.Summary.EnrollmentId,
                detail.Summary.Status,
                detail.Summary.Revision,
                detail.Summary.Visibility,
                detail.Summary.PermittedActions),
            MapBaseline(detail.Baseline),
            MapEffective(detail.Timing),
            MapCurrent(detail.Timing),
            detail.PolicyAvailable,
            detail.PermittedAccommodationDimensions,
            detail.PermittedReasonCategories,
            detail.History.Select(MapHistory).ToArray());

    public static MyWorkTimingV2 MapMyWorkTiming(AssignmentTimingSummary detail) =>
        new(
            "v2",
            new MyWorkTimingAssignmentV2(
                detail.Assignment.EnrollmentId,
                detail.Assignment.Status,
                detail.Assignment.Visibility,
                detail.Assignment.ActivityTitle,
                detail.Assignment.TaskTitle,
                detail.Assignment.TimeZoneId,
                EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.StartsAtUtc),
                EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.EndsAtUtc),
                EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.DeadlineUtc),
                detail.Assignment.SummaryAvailable,
                detail.Assignment.PermittedActions),
            detail.Timing is null ? null : MapEffective(detail.Timing),
            detail.ParticipantConsequenceCode);

    private static TimingBaselineV2 MapBaseline(BaselineTiming baseline) =>
        new(
            RequiredUtc(baseline.StartsAtUtc),
            RequiredUtc(baseline.EndsAtUtc),
            RequiredUtc(baseline.DeadlineUtc),
            baseline.TimeZoneId,
            baseline.AttemptLimit,
            baseline.PerAttemptDurationSeconds);

    private static IReadOnlyList<CurrentAccommodationEffectV2> MapCurrent(EffectiveTiming timing) =>
        timing.CurrentAccommodations
            .Select(item => new CurrentAccommodationEffectV2(
                item.AccommodationId,
                item.Dimension,
                item.ConsequenceCode))
            .ToArray();

    private static TimingEffectiveWindowV2 MapEffective(EffectiveTiming timing) =>
        new(
            RequiredUtc(timing.EffectiveSubmissionStartUtc),
            RequiredUtc(timing.EffectiveSubmissionExclusiveEndUtc),
            RequiredUtc(timing.EffectiveAttemptStartUtc),
            RequiredUtc(timing.EffectiveAttemptStartExclusiveEndUtc),
            timing.EffectivePerAttemptDurationSeconds,
            RequiredUtc(timing.EvaluatedAtUtc),
            timing.EligibilityState,
            timing.IsAuthoritativeEligibility,
            timing.TimeZoneId,
            timing.ParticipantConsequenceCode);

    private static AccommodationHistoryItemV2 MapHistory(Accommodation item) =>
        new(
            item.AccommodationId,
            item.Dimension,
            item.Status,
            item.NormalizedValue,
            item.ReasonCategory,
            item.FairnessException,
            item.Revision,
            RequiredUtc(item.CreatedAtUtc),
            EnrollmentEndpointExtensions.FormatUtc(item.DecidedAtUtc),
            item.ExpiresAtUtc is null
                ? null
                : AccommodationPolicyNormalizer.FormatCanonicalInstant(item.ExpiresAtUtc.Value));

    private static string RequiredUtc(DateTimeOffset value) =>
        EnrollmentEndpointExtensions.FormatUtc(value)
        ?? throw new InvalidOperationException("Timing projection requires a UTC instant.");
}
