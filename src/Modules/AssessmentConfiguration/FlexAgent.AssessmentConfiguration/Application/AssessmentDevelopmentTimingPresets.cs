using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public static class AssessmentDevelopmentTimingPresets
{
    public const string SyntheticTimedV1 = "development.synthetic_timed.v1";

    public static TimingRules SyntheticTimedV1Rules() => new(
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
        "UTC",
        2,
        3600,
        900,
        300);
}
