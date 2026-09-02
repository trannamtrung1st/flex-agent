namespace FlexAgent.Api;

public interface IHostedSessionTelemetry
{
    void RecordSnapshot(string outcomeClass, TimeSpan duration);

    void RecordCommand(string commandFamily, string outcomeClass, TimeSpan duration);

    void RecordSubscribe(string outcomeClass, TimeSpan duration);
}

public sealed class LoggingHostedSessionTelemetry(ILogger<LoggingHostedSessionTelemetry> logger) : IHostedSessionTelemetry
{
    public void RecordSnapshot(string outcomeClass, TimeSpan duration) =>
        logger.LogInformation(
            "session.hosted.snapshot {Outcome} {DurationMs}",
            Sanitize(outcomeClass),
            (int)duration.TotalMilliseconds);

    public void RecordCommand(string commandFamily, string outcomeClass, TimeSpan duration) =>
        logger.LogInformation(
            "session.hosted.command {Command} {Outcome} {DurationMs}",
            Sanitize(commandFamily),
            Sanitize(outcomeClass),
            (int)duration.TotalMilliseconds);

    public void RecordSubscribe(string outcomeClass, TimeSpan duration) =>
        logger.LogInformation(
            "session.hosted.subscribe {Outcome} {DurationMs}",
            Sanitize(outcomeClass),
            (int)duration.TotalMilliseconds);

    private static string Sanitize(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Contains(' ', StringComparison.Ordinal)
            ? "unknown"
            : value;
}
