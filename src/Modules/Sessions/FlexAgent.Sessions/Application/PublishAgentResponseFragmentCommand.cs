using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record PublishAgentResponseFragmentCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string AgentInvocationId,
    int FragmentOrdinal,
    string ExactUtf8Text,
    string GenerationAttemptId,
    Guid CorrelationId,
    string SourceChannel);

public interface IPublishAgentResponseFragmentHandler
{
    AgentResponseFragmentCommitResult Handle(
        PublishAgentResponseFragmentCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class PublishAgentResponseFragmentHandler : IPublishAgentResponseFragmentHandler
{
    public AgentResponseFragmentCommitResult Handle(
        PublishAgentResponseFragmentCommand command,
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

        if (command.ExpectedSessionVersion != session.SessionVersion)
        {
            return new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.StaleVersion);
        }

        return session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(
                command.AgentInvocationId,
                command.FragmentOrdinal,
                command.ExactUtf8Text,
                command.GenerationAttemptId),
            authoritativeUtc);
    }
}

public static class SessionRuntimePublicationOutbox
{
    public static string FragmentWakeupSeed(string messageId, int fragmentOrdinal, string contentDigest) =>
        $"frag:{messageId}:{fragmentOrdinal}:{contentDigest}";
}
