using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed class InMemoryEvaluationProviderArtifactStore : IEvaluationProviderArtifactStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid ProviderArtifactId), ProviderArtifactProvenanceSnapshot> _artifacts = [];

    public Task<EvaluationDecision<Guid>> TryAppendAsync(
        ProviderArtifactAppendCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        _ = cancellationToken;

        if (command.Ownership.OrganizationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.InvocationAttemptId == Guid.Empty
            || command.ProviderArtifactId == Guid.Empty)
        {
            return Task.FromResult(EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField));
        }

        var key = (command.Ownership.OrganizationId, command.ProviderArtifactId);
        var candidate = ProviderArtifactProvenance.FromAppendCommand(command);
        if (_artifacts.TryGetValue(key, out var existing))
        {
            return Task.FromResult(
                ProviderArtifactProvenance.IsEquivalentRetry(existing, candidate)
                    ? EvaluationDecision<Guid>.Ok(command.ProviderArtifactId)
                    : EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.DeterministicConflict));
        }

        _artifacts[key] = candidate;
        return Task.FromResult(EvaluationDecision<Guid>.Ok(command.ProviderArtifactId));
    }
}
