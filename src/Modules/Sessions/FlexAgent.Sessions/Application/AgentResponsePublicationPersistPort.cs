using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public interface IAgentResponsePublicationPersistPort
{
    Task<AgentResponseFragmentCommitResult> PersistFragmentAsync(
        PublishAgentResponseFragmentCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken);

    Task<AgentResponseFragmentCommitResult> PersistSealAsync(
        SealAgentResponseCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken);

    Task<bool> TryPersistUnpublishedFailureAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        long expectedSessionVersion,
        SessionRuntime session,
        CancellationToken cancellationToken);
}

public sealed class PassThroughAgentResponsePublicationPersistPort : IAgentResponsePublicationPersistPort
{
    public static PassThroughAgentResponsePublicationPersistPort Succeed { get; } = new(true);

    public PassThroughAgentResponsePublicationPersistPort(bool persistSucceeded)
    {
        PersistSucceeded = persistSucceeded;
    }

    public bool PersistSucceeded { get; }

    public int FragmentPersists { get; private set; }

    public int SealPersists { get; private set; }

    public int UnpublishedFailurePersists { get; private set; }

    public Task<AgentResponseFragmentCommitResult> PersistFragmentAsync(
        PublishAgentResponseFragmentCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);
        FragmentPersists++;
        return Task.FromResult(
            PersistSucceeded
                ? new AgentResponseFragmentCommitResult(true, FragmentCommitOutcomeCodes.Succeeded)
                : new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.StaleVersion));
    }

    public Task<AgentResponseFragmentCommitResult> PersistSealAsync(
        SealAgentResponseCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);
        SealPersists++;
        return Task.FromResult(
            PersistSucceeded
                ? new AgentResponseFragmentCommitResult(true, FragmentCommitOutcomeCodes.Succeeded)
                : new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.StaleVersion));
    }

    public Task<bool> TryPersistUnpublishedFailureAsync(
        SessionOwnership ownership,
        TrustedSessionBinding binding,
        long expectedSessionVersion,
        SessionRuntime session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(session);
        UnpublishedFailurePersists++;
        return Task.FromResult(PersistSucceeded);
    }
}
