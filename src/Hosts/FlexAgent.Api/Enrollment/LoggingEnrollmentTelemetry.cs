using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

public sealed class LoggingEnrollmentTelemetry(ILogger<LoggingEnrollmentTelemetry> logger) : IEnrollmentTelemetry
{
    public void RecordMutation(string operationKind, string outcomeClass, TimeSpan duration) =>
        logger.LogInformation(
            "enrollment.mutation {Operation} {Outcome} {DurationMs}",
            operationKind,
            outcomeClass,
            (int)duration.TotalMilliseconds);

    public void RecordRequestLimit(string surface, string decision) =>
        logger.LogInformation("enrollment.request_limit {Surface} {Decision}", surface, decision);
}
