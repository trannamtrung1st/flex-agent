using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record CompleteInvocationCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string AgentInvocationId,
    DecisionRecommendation? Decision,
    ExecutionFailureCompletion? ExecutionFailure,
    Guid CorrelationId,
    string SourceChannel);

public interface ICompleteInvocationHandler
{
    InvocationCompletionResult Handle(
        CompleteInvocationCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class CompleteInvocationHandler : ICompleteInvocationHandler
{
    public InvocationCompletionResult Handle(
        CompleteInvocationCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);

        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.Denied, null);
        }

        if (command.Ownership != session.Ownership)
        {
            return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.OwnershipMismatch, null);
        }

        if (command.Decision is not null && command.ExecutionFailure is not null)
        {
            return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.IdentityMismatch, null);
        }

        if (command.Decision is null && command.ExecutionFailure is null)
        {
            return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.IdentityMismatch, null);
        }

        var invocation = session.Invocations.FirstOrDefault(item =>
            string.Equals(item.AgentInvocationId, command.AgentInvocationId, StringComparison.Ordinal));
        if (invocation is null)
        {
            return new InvocationCompletionResult(false, InvocationCompletionOutcomeCodes.AlreadyTerminal, null);
        }

        if (!invocation.IsTerminal && command.ExpectedSessionVersion != session.SessionVersion)
        {
            return new InvocationCompletionResult(
                false,
                InvocationCompletionOutcomeCodes.StaleVersion,
                invocation);
        }

        return command.Decision is not null
            ? session.CompleteInvocation(command.AgentInvocationId, command.Decision, authoritativeUtc)
            : session.CompleteInvocation(command.AgentInvocationId, command.ExecutionFailure!, authoritativeUtc);
    }
}
