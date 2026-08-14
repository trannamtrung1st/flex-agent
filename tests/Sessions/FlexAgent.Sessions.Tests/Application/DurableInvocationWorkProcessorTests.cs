using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class DurableInvocationWorkProcessorTests
{
    [Fact]
    public async Task Idle_store_does_not_call_the_model_port()
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var processor = CreateProcessor(adapter, pending: false);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Idle, result.Outcome);
    }

    [Fact]
    public async Task Claimed_work_records_one_schema_valid_decision_and_completes_the_claim()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.worker",
            SessionRuntimeTestFixtures.T0);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                DecisionDispositions.NoAction,
                outputs: [],
                requestedActions: [],
                noActionReasonCategory: NoActionReasonCategories.IntentionalSilence,
                decisionId: "adec.worker.00000001"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, result.Outcome);
        Assert.Equal(invocationId, result.AgentInvocationId);
        Assert.True(store.Completed);
        var invocation = Assert.Single(session.Invocations);
        Assert.NotNull(invocation.Decision);
        Assert.Null(invocation.ExecutionOutcome);
        Assert.Equal(AgentInvocationStatuses.Decided, invocation.Status);
    }

    [Fact]
    public async Task Schema_invalid_control_is_an_execution_outcome_not_a_decision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.malformed"),
            "idem.opening.malformed",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueControlJson("{ not json"u8.ToArray());
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.ExecutionFailed, result.Outcome);
        Assert.True(store.Completed);
        var invocation = Assert.Single(session.Invocations);
        Assert.Null(invocation.Decision);
        Assert.NotNull(invocation.ExecutionOutcome);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, invocation.ExecutionOutcome!.ReasonCategory);
    }

    [Fact]
    public async Task Missing_credential_binding_fails_closed_without_a_decision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.bind"),
            "idem.opening.bind",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(SessionRuntimeTestFixtures.Envelope(invocationId));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(
            adapter,
            session,
            store,
            bindingRequest: ownership => new ModelDeploymentCredentialBindingRequest(
                ownership.OrganizationId,
                "synthetic.provider",
                null,
                null,
                null,
                null,
                false,
                false,
                false));

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.ExecutionFailed, result.Outcome);
        Assert.True(store.Completed);
        Assert.Null(session.Invocations[0].Decision);
        Assert.Equal(
            ExecutionFailureReasons.CredentialBindingFailed,
            session.Invocations[0].ExecutionOutcome!.ReasonCategory);
    }

    [Fact]
    public async Task Redelivery_after_a_terminal_invocation_completes_work_without_calling_the_port()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.retry"),
            "idem.opening.retry",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(ExecutionFailureReasons.ProviderTimeout),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Reconciled, result.Outcome);
        Assert.True(store.Completed);
        Assert.Equal(AgentInvocationStatuses.ExecutionFailed, session.Invocations[0].Status);
    }

    [Fact]
    public async Task Retry_later_releases_the_claim_so_the_next_poll_can_take_the_work()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var store = new MemoryWorkStore(session.Ownership, "ainv.orphan.00000001");
        var processor = CreateProcessor(adapter, session, store);

        var first = await processor.TryProcessNextAsync(CancellationToken.None);
        var second = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, first.Outcome);
        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, second.Outcome);
        Assert.False(store.Completed);
        Assert.Equal(2, store.ClaimCount);
    }

    [Fact]
    public async Task Pre_cancelled_worker_releases_claimed_work_with_a_cleanup_token()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.cancel"),
            "idem.opening.cancel",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                DecisionDispositions.NoAction,
                outputs: [],
                requestedActions: [],
                noActionReasonCategory: NoActionReasonCategories.IntentionalSilence,
                decisionId: "adec.worker.cancel001"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cancelled = await processor.TryProcessNextAsync(cts.Token);
        var retry = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, cancelled.Outcome);
        Assert.Equal(DurableInvocationWorkOutcomes.Decided, retry.Outcome);
        Assert.True(store.Completed);
        Assert.Equal(AgentInvocationStatuses.Decided, session.Invocations[0].Status);
    }

    [Fact]
    public async Task Crash_after_claim_leaves_work_claimed_until_lease_recovery()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.claimcrash"),
            "idem.opening.claimcrash",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(invocationId, "adec.worker.claimcrash"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var gateway = new FaultInjectingSessionGateway(new MemorySessionGateway(session))
        {
            FailNextLoad = 1,
        };
        var processor = CreateProcessor(adapter, gateway, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken.None));
        Assert.Equal(DurableSessionWorkStates.Claimed, store.ClaimedState);
        Assert.Equal(0, adapter.ExecuteCount);

        var blocked = await processor.TryProcessNextAsync(CancellationToken.None);
        Assert.Equal(DurableInvocationWorkOutcomes.Idle, blocked.Outcome);

        store.ExpireClaimedLeases();
        var recovered = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, recovered.Outcome);
        Assert.True(store.Completed);
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.Equal(AgentInvocationStatuses.Decided, session.Invocations[0].Status);
    }

    [Fact]
    public async Task Crash_after_provider_return_does_not_record_a_decision_until_retry()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.providercrash"),
            "idem.opening.providercrash",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CrashAfterReturnPort(EnqueueNoAction(invocationId, "adec.worker.provcrash", copies: 2))
        {
            FailAfterNextReturn = 1,
        };
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken.None));
        Assert.Null(session.Invocations[0].Decision);
        Assert.False(session.Invocations[0].IsTerminal);
        Assert.Equal(DurableSessionWorkStates.Claimed, store.ClaimedState);

        store.ExpireClaimedLeases();
        var recovered = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, recovered.Outcome);
        Assert.Equal(2, adapter.ExecuteCount);
        Assert.NotNull(session.Invocations[0].Decision);
        Assert.True(store.Completed);
    }

    [Fact]
    public async Task Lost_acknowledgement_after_decision_commit_reconciles_without_a_second_provider_call()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.ackcrash"),
            "idem.opening.ackcrash",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(invocationId, "adec.worker.ackcrash01"));
        var inner = new MemoryWorkStore(session.Ownership, invocationId);
        var store = new FaultInjectingWorkStore(inner) { FailNextMarkCompleted = 1 };
        var processor = CreateProcessor(adapter, session, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken.None));
        Assert.NotNull(session.Invocations[0].Decision);
        Assert.False(inner.Completed);

        inner.ExpireClaimedLeases();
        var recovered = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Reconciled, recovered.Outcome);
        Assert.Equal(InvocationCompletionOutcomeCodes.AlreadyTerminal, recovered.CompletionOutcomeCode);
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.True(inner.Completed);
        Assert.NotNull(session.Invocations[0].Decision);
    }

    [Fact]
    public async Task Unprocessable_oldest_item_does_not_monopolize_the_queue()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.fairqueue"),
            "idem.opening.fairqueue",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(invocationId, "adec.worker.fairqueue1"));
        var store = new MemoryWorkStore();
        store.Enqueue(session.Ownership, "ainv.orphan.poison0001");
        store.Enqueue(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var first = await processor.TryProcessNextAsync(CancellationToken.None);
        var second = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, first.Outcome);
        Assert.Equal("ainv.orphan.poison0001", first.AgentInvocationId);
        Assert.Equal(DurableInvocationWorkOutcomes.Decided, second.Outcome);
        Assert.Equal(invocationId, second.AgentInvocationId);
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.True(store.Completed);
    }

    private static DurableInvocationWorkProcessor CreateProcessor(
        DeterministicFakeModelExecutionAdapter adapter,
        bool pending)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var store = new MemoryWorkStore(session.Ownership, "ainv.missing", enqueue: pending);
        return CreateProcessor(adapter, session, store);
    }

    private static DurableInvocationWorkProcessor CreateProcessor(
        IModelExecutionPort adapter,
        SessionRuntime session,
        IDurableInvocationWorkStore store,
        Func<SessionOwnership, ModelDeploymentCredentialBindingRequest>? bindingRequest = null) =>
        CreateProcessor(adapter, new MemorySessionGateway(session), store, bindingRequest);

    private static DurableInvocationWorkProcessor CreateProcessor(
        IModelExecutionPort adapter,
        IInvocationWorkSessionGateway gateway,
        IDurableInvocationWorkStore store,
        Func<SessionOwnership, ModelDeploymentCredentialBindingRequest>? bindingRequest = null) =>
        new(
            store,
            gateway,
            adapter,
            new CompleteInvocationHandler(),
            new DurableInvocationWorkSettings(
                SessionRuntimeTestFixtures.CreateActor(),
                "synthetic.provider",
                "worker.session_runtime",
                65_536,
                bindingRequest ?? (ownership => new ModelDeploymentCredentialBindingRequest(
                    ownership.OrganizationId,
                    "synthetic.provider",
                    "bind.opaque.0001",
                    "bind.v1",
                    null,
                    null,
                    false,
                    false,
                    false))));

    private static DeterministicFakeModelExecutionAdapter EnqueueNoAction(
        string invocationId,
        string decisionId,
        int copies = 1)
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        for (var i = 0; i < copies; i++)
        {
            adapter.EnqueueEnvelope(
                SessionRuntimeTestFixtures.Envelope(
                    invocationId,
                    DecisionDispositions.NoAction,
                    [],
                    [],
                    NoActionReasonCategories.IntentionalSilence,
                    decisionId));
        }

        return adapter;
    }

    private sealed class MemoryWorkStore : IDurableInvocationWorkStore
    {
        private readonly List<WorkSlot> _slots = [];
        private long _queueClock;

        public MemoryWorkStore()
        {
        }

        public MemoryWorkStore(SessionOwnership ownership, string agentInvocationId, bool enqueue = true)
        {
            if (enqueue)
            {
                Enqueue(ownership, agentInvocationId);
            }
        }

        public bool Completed { get; private set; }

        public int ClaimCount { get; private set; }

        public string? ClaimedState => _slots.Count == 0 ? null : _slots[0].Item.State;

        public void Enqueue(SessionOwnership ownership, string agentInvocationId) =>
            _slots.Add(new WorkSlot(
                new DurableInvocationWorkItem(
                    Guid.NewGuid(),
                    ownership,
                    agentInvocationId,
                    DurableSessionWorkStates.Pending),
                _queueClock++));

        public void ExpireClaimedLeases()
        {
            foreach (var slot in _slots)
            {
                if (slot.Item.State == DurableSessionWorkStates.Claimed)
                {
                    slot.LeaseExpired = true;
                }
            }
        }

        public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
            TimeSpan lease,
            CancellationToken cancellationToken)
        {
            var candidate = _slots
                .Where(slot =>
                    slot.Item.State == DurableSessionWorkStates.Pending
                    || (slot.Item.State == DurableSessionWorkStates.Claimed && slot.LeaseExpired))
                .OrderBy(slot => slot.QueueOrder)
                .ThenBy(slot => slot.Item.WorkId)
                .FirstOrDefault();
            if (candidate is null)
            {
                return Task.FromResult<DurableInvocationWorkItem?>(null);
            }

            ClaimCount++;
            candidate.Item = candidate.Item with { State = DurableSessionWorkStates.Claimed };
            candidate.LeaseExpired = false;
            candidate.QueueOrder = _queueClock++;
            return Task.FromResult<DurableInvocationWorkItem?>(candidate.Item);
        }

        public Task ReleaseToPendingAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slot = Require(work);
            slot.Item = work with { State = DurableSessionWorkStates.Pending };
            slot.LeaseExpired = false;
            slot.QueueOrder = _queueClock++;
            return Task.CompletedTask;
        }

        public Task MarkCompletedAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slot = Require(work);
            Completed = true;
            slot.Item = work with { State = DurableSessionWorkStates.Completed };
            slot.LeaseExpired = false;
            slot.QueueOrder = _queueClock++;
            return Task.CompletedTask;
        }

        private WorkSlot Require(DurableInvocationWorkItem work) =>
            _slots.Single(slot => slot.Item.WorkId == work.WorkId);

        private sealed class WorkSlot(DurableInvocationWorkItem item, long queueOrder)
        {
            public DurableInvocationWorkItem Item { get; set; } = item;

            public long QueueOrder { get; set; } = queueOrder;

            public bool LeaseExpired { get; set; }
        }
    }

    private sealed class MemorySessionGateway(SessionRuntime session) : IInvocationWorkSessionGateway
    {
        public Task<LoadedInvocationWorkSession?> LoadAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            Task.FromResult<LoadedInvocationWorkSession?>(
                ownership == session.Ownership
                    ? new LoadedInvocationWorkSession(session, session.Binding, session.SessionVersion)
                    : null);

        public Task<DateTimeOffset> ReadAuthoritativeUtcAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SessionRuntimeTestFixtures.T0.AddSeconds(2));

        public Task<bool> TrySaveCompletionAsync(
            SessionOwnership ownership,
            long expectedSessionVersion,
            SessionRuntime runtime,
            AgentInvocation invocation,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class FaultInjectingSessionGateway(IInvocationWorkSessionGateway inner) : IInvocationWorkSessionGateway
    {
        public int FailNextLoad { get; set; }

        public Task<LoadedInvocationWorkSession?> LoadAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken)
        {
            if (FailNextLoad > 0)
            {
                FailNextLoad--;
                throw new InvalidOperationException("Injected crash after claim.");
            }

            return inner.LoadAsync(ownership, cancellationToken);
        }

        public Task<DateTimeOffset> ReadAuthoritativeUtcAsync(CancellationToken cancellationToken) =>
            inner.ReadAuthoritativeUtcAsync(cancellationToken);

        public Task<bool> TrySaveCompletionAsync(
            SessionOwnership ownership,
            long expectedSessionVersion,
            SessionRuntime session,
            AgentInvocation invocation,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            inner.TrySaveCompletionAsync(
                ownership,
                expectedSessionVersion,
                session,
                invocation,
                correlationId,
                cancellationToken);
    }

    private sealed class FaultInjectingWorkStore(IDurableInvocationWorkStore inner) : IDurableInvocationWorkStore
    {
        public int FailNextMarkCompleted { get; set; }

        public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
            TimeSpan lease,
            CancellationToken cancellationToken) =>
            inner.TryClaimExecuteInvocationAsync(lease, cancellationToken);

        public Task ReleaseToPendingAsync(
            DurableInvocationWorkItem work,
            CancellationToken cancellationToken) =>
            inner.ReleaseToPendingAsync(work, cancellationToken);

        public Task MarkCompletedAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            if (FailNextMarkCompleted > 0)
            {
                FailNextMarkCompleted--;
                throw new InvalidOperationException("Injected pre-acknowledgement failure.");
            }

            return inner.MarkCompletedAsync(work, cancellationToken);
        }
    }

    private sealed class CountingModelExecutionPort(IModelExecutionPort inner) : IModelExecutionPort
    {
        public int ExecuteCount { get; private set; }

        public Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;
            return inner.ExecuteAsync(request, cancellationToken);
        }
    }

    private sealed class CrashAfterReturnPort(IModelExecutionPort inner) : IModelExecutionPort
    {
        public int FailAfterNextReturn { get; set; }

        public int ExecuteCount { get; private set; }

        public async Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;
            var result = await inner.ExecuteAsync(request, cancellationToken);
            if (FailAfterNextReturn > 0)
            {
                FailAfterNextReturn--;
                throw new InvalidOperationException("Injected crash after provider return.");
            }

            return result;
        }
    }
}
