namespace FlexAgent.Submissions.Domain;

public sealed record BaselineTiming(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset DeadlineUtc,
    string TimeZoneId,
    int AttemptLimit,
    int? PerAttemptDurationSeconds,
    AccommodationPolicyIdentity FrozenPolicy,
    bool VerificationDegraded,
    NormalizedAccommodationPolicy? FrozenPolicySnapshot = null);

public sealed record AppliedAccommodationEffect(
    Guid AccommodationId,
    string Dimension,
    string ConsequenceCode);

public sealed record EffectiveTiming(
    BaselineTiming Baseline,
    DateTimeOffset EffectiveSubmissionStartUtc,
    DateTimeOffset EffectiveSubmissionExclusiveEndUtc,
    DateTimeOffset EffectiveAttemptStartUtc,
    DateTimeOffset EffectiveAttemptStartExclusiveEndUtc,
    int? EffectivePerAttemptDurationSeconds,
    DateTimeOffset EvaluatedAtUtc,
    string EligibilityState,
    bool IsAuthoritativeEligibility,
    IReadOnlyList<AppliedAccommodationEffect> CurrentAccommodations,
    string ParticipantConsequenceCode,
    string TimeZoneId);

public static class EffectiveTimingEvaluator
{
    public static EffectiveTiming Evaluate(
        BaselineTiming baseline,
        string enrollmentStatus,
        NormalizedAccommodationPolicy? currentPolicy,
        IReadOnlyList<Accommodation> records,
        DateTimeOffset nowUtc)
    {
        nowUtc = nowUtc.ToUniversalTime();
        var effectivePolicy = baseline.VerificationDegraded
            ? null
            : AccommodationPolicyNormalizer.EffectiveBounds(
                baseline.FrozenPolicy,
                baseline.FrozenPolicySnapshot,
                currentPolicy);
        var authoritative = enrollmentStatus == EnrollmentStates.Active
            && !baseline.VerificationDegraded
            && effectivePolicy is { EnvironmentEligible: true };
        var submissionEnd = baseline.DeadlineUtc;
        var attemptStart = baseline.StartsAtUtc;
        var attemptEnd = baseline.EndsAtUtc;
        var duration = baseline.PerAttemptDurationSeconds;
        var applied = new List<AppliedAccommodationEffect>();

        if (authoritative)
        {
            foreach (var record in CurrentEffects(records, effectivePolicy!, nowUtc))
            {
                switch (record.Dimension)
                {
                    case AccommodationDimensions.SubmissionDeadlineUtc
                        when AccommodationPolicyNormalizer.TryParseInstant(record.NormalizedValue, out var deadline):
                        submissionEnd = deadline;
                        applied.Add(new AppliedAccommodationEffect(
                            record.AccommodationId,
                            record.Dimension,
                            AccommodationConsequenceCodes.DeadlineReplacement));
                        break;
                    case AccommodationDimensions.AttemptStartNotBeforeUtc
                        when AccommodationPolicyNormalizer.TryParseInstant(record.NormalizedValue, out var notBefore):
                        attemptStart = notBefore;
                        applied.Add(new AppliedAccommodationEffect(
                            record.AccommodationId,
                            record.Dimension,
                            AccommodationConsequenceCodes.AttemptStartReplacement));
                        break;
                    case AccommodationDimensions.AttemptStartBeforeUtc
                        when AccommodationPolicyNormalizer.TryParseInstant(record.NormalizedValue, out var before):
                        attemptEnd = before;
                        applied.Add(new AppliedAccommodationEffect(
                            record.AccommodationId,
                            record.Dimension,
                            AccommodationConsequenceCodes.AttemptStartReplacement));
                        break;
                    case AccommodationDimensions.PerAttemptDurationSeconds
                        when AccommodationPolicyNormalizer.TryParseDuration(record.NormalizedValue, out var seconds):
                        duration = seconds;
                        applied.Add(new AppliedAccommodationEffect(
                            record.AccommodationId,
                            record.Dimension,
                            AccommodationConsequenceCodes.DurationReplacement));
                        break;
                }
            }
        }

        var consequence = SummarizeConsequences(applied);

        var state = !authoritative
            ? TimingEligibilityStates.Unavailable
            : nowUtc < baseline.StartsAtUtc || nowUtc < attemptStart
                ? TimingEligibilityStates.TooEarly
                : nowUtc >= attemptEnd
                    ? TimingEligibilityStates.AttemptStartClosed
                    : nowUtc >= submissionEnd
                        ? TimingEligibilityStates.SubmissionClosed
                        : TimingEligibilityStates.Open;

        return new EffectiveTiming(
            baseline,
            baseline.StartsAtUtc,
            submissionEnd,
            attemptStart,
            attemptEnd,
            duration,
            nowUtc,
            state,
            authoritative,
            applied,
            consequence,
            baseline.TimeZoneId);
    }

    internal static string SummarizeConsequences(IReadOnlyList<AppliedAccommodationEffect> applied)
    {
        if (applied.Count == 0)
        {
            return AccommodationConsequenceCodes.None;
        }

        var kinds = applied
            .Select(item => item.ConsequenceCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return kinds.Length == 1
            ? kinds[0]
            : AccommodationConsequenceCodes.MultipleReplacements;
    }

    private static IEnumerable<Accommodation> CurrentEffects(
        IReadOnlyList<Accommodation> records,
        NormalizedAccommodationPolicy currentPolicy,
        DateTimeOffset nowUtc)
    {
        foreach (var dimension in AccommodationDimensions.All)
        {
            Accommodation? selected = null;
            foreach (var record in records)
            {
                if (record.Dimension != dimension
                    || !record.AffectsEligibilityAt(nowUtc, currentPolicy)
                    || (selected is not null && record.CreatedAtUtc <= selected.CreatedAtUtc))
                {
                    continue;
                }

                selected = record;
            }

            if (selected is not null)
            {
                yield return selected;
            }
        }
    }
}
