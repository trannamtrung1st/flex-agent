using System.Globalization;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record ReplayAuthorizedSessionEventsCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    string? UntrustedLastEventId);

public interface IReplayAuthorizedSessionEventsHandler
{
    AuthorizedSessionEventReplayResult Handle(
        ReplayAuthorizedSessionEventsCommand command,
        SessionRuntime session);
}

public sealed class ReplayAuthorizedSessionEventsHandler : IReplayAuthorizedSessionEventsHandler
{
    public AuthorizedSessionEventReplayResult Handle(
        ReplayAuthorizedSessionEventsCommand command,
        SessionRuntime session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);

        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            return Denied(SessionEventReplayOutcomeCodes.Denied);
        }

        if (command.Ownership != session.Ownership)
        {
            return Denied(SessionEventReplayOutcomeCodes.OwnershipMismatch);
        }

        if (!TryParseCursor(command.UntrustedLastEventId, out var afterSequence, out var malformed))
        {
            return malformed
                ? Denied(SessionEventReplayOutcomeCodes.Reconcile)
                : AuthorizedSessionEventProjector.Project(session, afterSequence: 0);
        }

        if (afterSequence < 1 || afterSequence > session.SessionSequence)
        {
            return Denied(SessionEventReplayOutcomeCodes.Reconcile);
        }

        return AuthorizedSessionEventProjector.Project(session, afterSequence);
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
}
