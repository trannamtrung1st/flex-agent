using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class AdmitTrustedTriggerHandler : IAdmitTrustedTriggerHandler
{
    public TriggerAdmissionResult Handle(
        AdmitTrustedTriggerCommand command,
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

        return session.AdmitTrustedTrigger(
            command.Trigger,
            command.IdempotencyKey,
            authoritativeUtc,
            command.ExpectedSessionVersion);
    }
}
