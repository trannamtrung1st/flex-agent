using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class InMemoryModelProviderAttemptProvenanceWriter : IModelProviderAttemptProvenanceWriter
{
    private readonly List<StoredFact> _facts = [];
    private readonly object _gate = new();

    public bool ThrowOnFinished { get; set; }

    public IReadOnlyList<StoredFact> Facts
    {
        get
        {
            lock (_gate)
            {
                return _facts.ToArray();
            }
        }
    }

    public Task WriteAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        ModelProviderAttemptProvenance provenance,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            WriteCore(ownership, agentInvocationId, invocationAttemptOrdinal, provenance);
        }

        return Task.CompletedTask;
    }

    public Task<int> CountAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(CountCore(ownership, agentInvocationId));
        }
    }

    public Task<ProviderRequestReservationResult> TryReserveStartedAsync(
        DurableInvocationWorkItem claimedWork,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        int maxProviderRequestAttempts,
        ModelProviderAttemptProvenance started,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimedWork);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInvocationId);
        ArgumentNullException.ThrowIfNull(started);
        if (maxProviderRequestAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxProviderRequestAttempts));
        }

        if (lease <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lease));
        }

        lock (_gate)
        {
            if (claimedWork.State != DurableSessionWorkStates.Claimed
                || claimedWork.ClaimLeaseUntil is null
                || claimedWork.ClaimLeaseUntil <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult(ProviderRequestReservationResult.LostClaim);
            }

            if (CountCore(claimedWork.Ownership, agentInvocationId) >= maxProviderRequestAttempts)
            {
                var held = DateTimeOffset.UtcNow.Add(lease);
                claimedWork.ClaimLeaseUntil = held;
                return Task.FromResult(ProviderRequestReservationResult.BudgetExhausted(held));
            }

            WriteCore(claimedWork.Ownership, agentInvocationId, invocationAttemptOrdinal, started with
            {
                FactKind = ModelProviderRequestFacts.Started,
            });
            var renewed = DateTimeOffset.UtcNow.Add(lease);
            claimedWork.ClaimLeaseUntil = renewed;
            return Task.FromResult(ProviderRequestReservationResult.Succeeded(renewed));
        }
    }

    private void WriteCore(
        SessionOwnership ownership,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        ModelProviderAttemptProvenance provenance)
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
            return;
        }

        _facts.Add(new StoredFact(ownership, agentInvocationId, invocationAttemptOrdinal, provenance with
        {
            FactKind = factKind,
        }));
    }

    private int CountCore(SessionOwnership ownership, string agentInvocationId) =>
        _facts
            .Where(fact =>
                fact.Ownership == ownership
                && string.Equals(fact.AgentInvocationId, agentInvocationId, StringComparison.Ordinal))
            .Select(fact => fact.Provenance.ProviderRequestId ?? fact.Provenance.ProviderRequestRef)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Count();

    public sealed record StoredFact(
        SessionOwnership Ownership,
        string AgentInvocationId,
        int InvocationAttemptOrdinal,
        ModelProviderAttemptProvenance Provenance);
}
