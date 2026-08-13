using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record AcceptParticipantMessageCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string ParticipantMessageId,
    string TurnId,
    string ResponseSlotId,
    string TriggerId,
    string IdempotencyKey,
    Guid CorrelationId,
    string SourceChannel);

public interface IAcceptParticipantMessageHandler
{
    TriggerAdmissionResult Handle(
        AcceptParticipantMessageCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class AcceptParticipantMessageHandler : IAcceptParticipantMessageHandler
{
    public TriggerAdmissionResult Handle(
        AcceptParticipantMessageCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);

        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            return new TriggerAdmissionResult(false, TriggerAdmissionOutcomeCodes.Denied, null, null);
        }

        if (command.Ownership != session.Ownership)
        {
            return new TriggerAdmissionResult(
                false,
                TriggerAdmissionOutcomeCodes.OwnershipMismatch,
                null,
                null);
        }

        return session.AcceptParticipantMessage(
            command.ParticipantMessageId,
            command.TurnId,
            command.ResponseSlotId,
            command.TriggerId,
            command.IdempotencyKey,
            authoritativeUtc,
            command.ExpectedSessionVersion);
    }
}
