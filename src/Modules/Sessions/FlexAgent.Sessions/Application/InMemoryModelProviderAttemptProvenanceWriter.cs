using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class InMemoryModelProviderAttemptProvenanceWriter : IModelProviderAttemptProvenanceWriter
{
    private readonly List<StoredFact> _facts = [];

    public bool ThrowOnFinished { get; set; }

    public IReadOnlyList<StoredFact> Facts => _facts;

    public Task WriteAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        ModelProviderAttemptProvenance provenance,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInvocationId);
        ArgumentNullException.ThrowIfNull(provenance);
        var factKind = string.IsNullOrWhiteSpace(provenance.FactKind)
            ? ModelProviderRequestFacts.Finished
            : provenance.FactKind;
        if (ThrowOnFinished
            && string.Equals(factKind, ModelProviderRequestFacts.Finished, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Injected crash before finished provider-request fact.");
        }

        var requestId = provenance.ProviderRequestId ?? provenance.ProviderRequestRef;
        if (_facts.Any(fact =>
                fact.Ownership == ownership
                && string.Equals(fact.Provenance.ProviderRequestId ?? fact.Provenance.ProviderRequestRef, requestId, StringComparison.Ordinal)
                && string.Equals(fact.Provenance.FactKind, factKind, StringComparison.Ordinal)))
        {
            return Task.CompletedTask;
        }

        _facts.Add(new StoredFact(ownership, agentInvocationId, invocationAttemptOrdinal, provenance with
        {
            FactKind = factKind,
        }));
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInvocationId);
        var used = _facts
            .Where(fact =>
                fact.Ownership == ownership
                && string.Equals(fact.AgentInvocationId, agentInvocationId, StringComparison.Ordinal))
            .Select(fact => fact.Provenance.ProviderRequestId ?? fact.Provenance.ProviderRequestRef)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Count();
        return Task.FromResult(used);
    }

    public sealed record StoredFact(
        SessionOwnership Ownership,
        string AgentInvocationId,
        int InvocationAttemptOrdinal,
        ModelProviderAttemptProvenance Provenance);
}
