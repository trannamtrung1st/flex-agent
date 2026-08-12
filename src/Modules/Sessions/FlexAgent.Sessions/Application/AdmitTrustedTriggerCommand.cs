using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record AdmitTrustedTriggerCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    TrustedTrigger Trigger,
    string IdempotencyKey,
    Guid CorrelationId,
    string SourceChannel);

public interface IAdmitTrustedTriggerHandler
{
    TriggerAdmissionResult Handle(
        AdmitTrustedTriggerCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}
