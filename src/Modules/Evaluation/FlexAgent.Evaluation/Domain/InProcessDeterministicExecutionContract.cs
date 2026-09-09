namespace FlexAgent.Evaluation.Domain;

using FlexAgent.Contracts.Evaluation;

public static class InProcessDeterministicExecutionContract
{
    public const string EnforcementProfile = "in-process-algorithmically-bounded.v1";

    public const int DeadlineCheckIntervalScalars = 256;

    public static bool TryCreateWallClockDeadline(
        DateTimeOffset startedAt,
        string elapsedTimeLimit,
        string cpuTimeLimit,
        out DateTimeOffset deadlineUtc,
        out string field)
    {
        field = "elapsed_time_limit";
        deadlineUtc = default;
        if (!EvaluationPositiveDuration.TryParseTotalSeconds(elapsedTimeLimit, out var elapsedSeconds))
        {
            return false;
        }

        if (!EvaluationPositiveDuration.TryParseTotalSeconds(cpuTimeLimit, out var cpuSeconds))
        {
            field = "cpu_time_limit";
            return false;
        }

        var limitSeconds = Math.Min(elapsedSeconds, cpuSeconds);
        deadlineUtc = startedAt.AddSeconds(limitSeconds);
        return true;
    }

    public static bool ShouldAbortScalarLoop(int scalarIndex) =>
        scalarIndex > 0
        && scalarIndex % DeadlineCheckIntervalScalars == 0;
}

public readonly struct DeterministicExecutionDeadline(DateTimeOffset deadlineUtc)
{
    public DateTimeOffset DeadlineUtc { get; } = deadlineUtc;

    public bool IsExpired => DateTimeOffset.UtcNow >= DeadlineUtc;

    public bool TryCheck(out string field)
    {
        field = "elapsed_time_limit";
        return !IsExpired;
    }
}
