using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record SealAgentResponseCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string AgentInvocationId,
    string CompletionState,
    Guid CorrelationId,
    string SourceChannel);

public interface ISealAgentResponseHandler
{
    AgentResponseFragmentCommitResult Handle(
        SealAgentResponseCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class SealAgentResponseHandler : ISealAgentResponseHandler
{
    public AgentResponseFragmentCommitResult Handle(
        SealAgentResponseCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);

        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
        }

        if (command.Ownership != session.Ownership)
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.OwnershipMismatch);
        }

        if (command.CompletionState is not (AgentMessageCompletionStates.Complete
            or AgentMessageCompletionStates.Incomplete))
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
        }

        var existing = session.AgentMessages.FirstOrDefault(message =>
            string.Equals(message.DrivingInvocationId, command.AgentInvocationId, StringComparison.Ordinal));
        if (existing is not { IsTerminal: true }
            && command.ExpectedSessionVersion != session.SessionVersion)
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.StaleVersion);
        }

        return command.CompletionState == AgentMessageCompletionStates.Complete
            ? session.CompleteAgentResponseMessage(command.AgentInvocationId, authoritativeUtc)
            : session.MarkAgentResponseIncomplete(command.AgentInvocationId, authoritativeUtc);
    }
}
