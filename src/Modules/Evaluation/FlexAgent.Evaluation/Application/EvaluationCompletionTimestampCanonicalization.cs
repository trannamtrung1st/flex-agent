namespace FlexAgent.Evaluation.Application;

public static class EvaluationCompletionTimestampCanonicalization
{
    private const long TicksPerMicrosecond = 10;

    public static DateTimeOffset ToPostgresUtc(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var truncatedTicks = utc.Ticks / TicksPerMicrosecond * TicksPerMicrosecond;
        return new DateTimeOffset(truncatedTicks, TimeSpan.Zero);
    }

    public static bool AreEquivalent(DateTimeOffset left, DateTimeOffset right) =>
        ToPostgresUtc(left) == ToPostgresUtc(right);
}
