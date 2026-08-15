using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record FireDueTimerCommand(
    TrustedRuntimeActor Actor,
    Guid CorrelationId,
    string SourceChannel);
