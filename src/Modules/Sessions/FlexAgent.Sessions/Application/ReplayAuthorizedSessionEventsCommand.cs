using System.Diagnostics;
using System.Globalization;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record ReplayAuthorizedSessionEventsCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    string? UntrustedLastEventId,
    bool UseHostedProjection = false);

public interface IReplayAuthorizedSessionEventsCoordinator
{
    Task<AuthorizedSessionEventReplayResult> ReplayAsync(
        ReplayAuthorizedSessionEventsCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default);
}

public interface IReplayAuthorizedSessionEventsHandler
{
    AuthorizedSessionEventReplayResult Handle(
        ReplayAuthorizedSessionEventsCommand command,
        SessionRuntime session);
}

public sealed class ReplayAuthorizedSessionEventsHandler(ISessionRuntimeTelemetry? telemetry = null)
    : IReplayAuthorizedSessionEventsHandler
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    public AuthorizedSessionEventReplayResult Handle(
        ReplayAuthorizedSessionEventsCommand command,
        SessionRuntime session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        var started = Stopwatch.GetTimestamp();

        AuthorizedSessionEventReplayResult result;
        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            result = Denied(SessionEventReplayOutcomeCodes.Denied);
        }
        else if (command.Ownership != session.Ownership)
        {
            result = Denied(SessionEventReplayOutcomeCodes.OwnershipMismatch);
        }
        else if (!TryParseCursor(command.UntrustedLastEventId, out var afterSequence, out var malformed))
        {
            result = malformed
                ? Denied(SessionEventReplayOutcomeCodes.Reconcile)
                : Project(session, afterSequence: 0, command.UseHostedProjection);
        }
        else if (afterSequence < 1 || afterSequence > session.SessionSequence)
        {
            result = Denied(SessionEventReplayOutcomeCodes.Reconcile);
        }
        else if (!IsIssuedStreamCursor(session, afterSequence, command.UseHostedProjection))
        {
            result = Denied(SessionEventReplayOutcomeCodes.Reconcile);
        }
        else
        {
            result = Project(session, afterSequence, command.UseHostedProjection);
        }

        var labels = SessionRuntimeTelemetryRecording.Labels(
            (SessionRuntimeTelemetryLabelKeys.Outcome, result.OutcomeCode));
        _telemetry.RecordCounter(SessionRuntimeTelemetryInstruments.EventReplay, labels);
        _telemetry.RecordDuration(
            SessionRuntimeTelemetryInstruments.EventReplay,
            Stopwatch.GetElapsedTime(started),
            labels);
        return result;
    }

    private static bool TryParseCursor(string? untrustedLastEventId, out long afterSequence, out bool malformed)
    {
        afterSequence = 0;
        malformed = false;
        if (string.IsNullOrWhiteSpace(untrustedLastEventId))
        {
            return false;
        }

        if (!long.TryParse(untrustedLastEventId, NumberStyles.None, CultureInfo.InvariantCulture, out afterSequence))
        {
            malformed = true;
            return false;
        }

        return true;
    }

    private static AuthorizedSessionEventReplayResult Denied(string outcomeCode) =>
        new(false, outcomeCode, []);

    private static AuthorizedSessionEventReplayResult Project(
        SessionRuntime session,
        long afterSequence,
        bool useHostedProjection) =>
        useHostedProjection
            ? HostedSessionEventProjector.Project(session, afterSequence)
            : AuthorizedSessionEventProjector.Project(session, afterSequence);

    private static bool IsIssuedStreamCursor(SessionRuntime session, long sequence, bool useHostedProjection) =>
        useHostedProjection
            ? HostedSessionEventProjector.IsIssuedStreamCursor(session, sequence)
            : AuthorizedSessionEventProjector.IsIssuedStreamCursor(session, sequence);
}
