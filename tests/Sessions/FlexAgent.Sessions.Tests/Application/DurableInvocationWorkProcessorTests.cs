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
    public async Task Shutdown_cancellation_does_not_terminalize_a_claimed_invocation()
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

    private static DurableInvocationWorkProcessor CreateProcessor(
        DeterministicFakeModelExecutionAdapter adapter,
        bool pending)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var store = new MemoryWorkStore(session.Ownership, "ainv.missing", enqueue: pending);
        return CreateProcessor(adapter, session, store);
    }

    private static DurableInvocationWorkProcessor CreateProcessor(
        DeterministicFakeModelExecutionAdapter adapter,
        SessionRuntime session,
        MemoryWorkStore store,
        Func<SessionOwnership, ModelDeploymentCredentialBindingRequest>? bindingRequest = null) =>
        new(
            store,
            new MemorySessionGateway(session),
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

    private sealed class MemoryWorkStore(
        SessionOwnership ownership,
        string agentInvocationId,
        bool enqueue = true) : IDurableInvocationWorkStore
    {
        private DurableInvocationWorkItem? _item = enqueue
            ? new DurableInvocationWorkItem(Guid.NewGuid(), ownership, agentInvocationId, DurableSessionWorkStates.Pending)
            : null;

        public bool Completed { get; private set; }

        public int ClaimCount { get; private set; }

        public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
            TimeSpan lease,
            CancellationToken cancellationToken)
        {
            if (_item is null || _item.State != DurableSessionWorkStates.Pending)
            {
                return Task.FromResult<DurableInvocationWorkItem?>(null);
            }

            ClaimCount++;
            _item = _item with { State = DurableSessionWorkStates.Claimed };
            return Task.FromResult<DurableInvocationWorkItem?>(_item);
        }

        public Task ReleaseToPendingAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            _item = work with { State = DurableSessionWorkStates.Pending };
            return Task.CompletedTask;
        }

        public Task MarkCompletedAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            Completed = true;
            _item = work with { State = DurableSessionWorkStates.Completed };
            return Task.CompletedTask;
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
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
