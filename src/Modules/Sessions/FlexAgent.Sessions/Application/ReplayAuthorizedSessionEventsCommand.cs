using System.Diagnostics;
using System.Globalization;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record ReplayAuthorizedSessionEventsCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    string? UntrustedLastEventId,
    bool UseHostedProjection = false,
    HostedSessionEventProjectionOptions? HostedProjectionOptions = null);

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
        else if (!TryParseCursor(command.UntrustedLastEventId, out var afterCursor, out var malformed))
        {
            result = malformed
                ? Denied(SessionEventReplayOutcomeCodes.Reconcile)
                : Project(session, afterCursor: 0, command.UseHostedProjection, command.HostedProjectionOptions);
        }
        else if (command.UseHostedProjection)
        {
            result = afterCursor < 1 || !HostedSessionEventProjector.IsIssuedStreamCursor(
                session,
                afterCursor,
                command.HostedProjectionOptions)
                ? Denied(SessionEventReplayOutcomeCodes.Reconcile)
                : HostedSessionEventProjector.Project(session, afterCursor, command.HostedProjectionOptions);
        }
        else if (afterCursor < 1 || afterCursor > session.SessionSequence)
        {
            result = Denied(SessionEventReplayOutcomeCodes.Reconcile);
        }
        else if (!AuthorizedSessionEventProjector.IsIssuedStreamCursor(session, afterCursor))
        {
            result = Denied(SessionEventReplayOutcomeCodes.Reconcile);
        }
        else
        {
            result = AuthorizedSessionEventProjector.Project(session, afterCursor);
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
        long afterCursor,
        bool useHostedProjection,
        HostedSessionEventProjectionOptions? hostedProjectionOptions = null) =>
        useHostedProjection
            ? HostedSessionEventProjector.Project(session, afterCursor, hostedProjectionOptions)
            : AuthorizedSessionEventProjector.Project(session, afterCursor);
}
