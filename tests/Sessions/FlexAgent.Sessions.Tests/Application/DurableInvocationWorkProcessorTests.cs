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
    public async Task Crash_after_control_call_before_finished_fact_does_not_send_another_provider_request()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.reserve"),
            "idem.opening.reserve",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CountingModelExecutionPort(
            EnqueueNoAction(invocationId, "adec.reserve.00000001", copies: 2));
        var writer = new InMemoryModelProviderAttemptProvenanceWriter { ThrowOnFinished = true };
        var profile = SessionRuntimeTestFixtures.CreateInstalledProfile();
        var seededAt = SessionRuntimeTestFixtures.T0;
        await writer.WriteAsync(
            session.Ownership,
            invocationId,
            1,
            new ModelProviderAttemptProvenance(
                profile.AdapterKind,
                profile.AdapterContractVersion,
                profile.ProfileId,
                profile.ProfileVersion,
                profile.ProfileDigest,
                profile.RequestedModel,
                profile.ResolvedModelVersion,
                ExecutionAttemptOutcomeCategories.ProviderRequestStarted,
                null,
                null,
                "pref.prat.seeded",
                seededAt,
                seededAt,
                ModelProviderRequestPhases.Control,
                "prat.seeded",
                ModelProviderRequestFacts.Started),
            CancellationToken.None);
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store, provenanceWriter: writer);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken.None));
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.Equal(2, await writer.CountAsync(session.Ownership, invocationId, CancellationToken.None));

        store.ExpireClaimedLeases();
        writer.ThrowOnFinished = false;
        var retried = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(1, adapter.ExecuteCount);
        Assert.NotEqual(DurableInvocationWorkOutcomes.Decided, retried.Outcome);
        Assert.Null(session.Invocations[0].Decision);
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
    public async Task Denied_model_disclosure_does_not_call_the_model_port()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.disclosure",
            SessionRuntimeTestFixtures.T0);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CountingModelExecutionPort(
            EnqueueNoAction(invocationId, "adec.disclosure.00000001"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(
            adapter,
            new DenyingModelDisclosureGateway(new MemorySessionGateway(session)),
            store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(store.Completed);
        Assert.Equal(0, adapter.ExecuteCount);
    }

    [Fact]
    public async Task Denied_stream_disclosure_does_not_start_provider_content_stream()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.streamdeny", SessionRuntimeTestFixtures.T0);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var inner = new DeterministicFakeModelExecutionAdapter();
        inner.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.streamdeny"));
        inner.EnqueueContent(
            new ModelContentTextDelta("Hi"),
            new ModelContentCompleted());
        var adapter = new CountingModelExecutionPort(inner);
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(
            adapter,
            new DenyAfterSuccessfulDisclosuresGateway(new MemorySessionGateway(session), permitCount: 1),
            store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.Equal(0, adapter.StreamCount);
        Assert.False(store.Completed);
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
    public async Task Missing_frozen_model_deployment_fails_closed_without_a_decision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession(includeFrozenDeployment: false);
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.bind"),
            "idem.opening.bind",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(SessionRuntimeTestFixtures.Envelope(invocationId));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

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
    public async Task Failed_durable_work_acknowledgement_after_decision_commit_reconciles_without_a_second_provider_call()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.ackcrash"),
            "idem.opening.ackcrash",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(invocationId, "adec.worker.ackcrash01"));
        var inner = new MemoryWorkStore(session.Ownership, invocationId);
        // Throw before MarkCompletedAsync: Decision is in memory, work ack fails.
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
    public async Task Accepted_message_decision_publishes_delta_content_then_seals_complete()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.content001"));
        adapter.EnqueueContent(
            new ModelContentTextDelta("Hel"),
            new ModelContentTextDelta("lo"),
            new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Published, result.Outcome);
        Assert.True(store.Completed);
        var message = Assert.Single(session.AgentMessages);
        Assert.Equal("Hello", message.AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Complete, message.CompletionState);
        Assert.Equal(2, message.Fragments.Count);
        Assert.Equal(TurnStates.Complete, session.Turns[0].State);
    }

    [Fact]
    public async Task Cumulative_snapshots_publish_only_verified_suffixes()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.cumul0001"));
        adapter.EnqueueContent(
            new ModelContentCumulativeSnapshot("Hel"),
            new ModelContentCumulativeSnapshot("Hello"),
            new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Published, result.Outcome);
        Assert.Equal("Hello", session.AgentMessages[0].AssembleExactText());
        Assert.Equal("Hel", session.AgentMessages[0].Fragments[0].ExactUtf8Text);
        Assert.Equal("lo", session.AgentMessages[0].Fragments[1].ExactUtf8Text);
    }

    [Fact]
    public async Task Prefix_divergence_seals_visible_prefix_incomplete_without_echo()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.diverg001"));
        adapter.EnqueueContent(
            new ModelContentCumulativeSnapshot("Hel"),
            new ModelContentCumulativeSnapshot("Hey"),
            new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.PublicationIncomplete, result.Outcome);
        Assert.DoesNotContain("Hey", result.Outcome, StringComparison.Ordinal);
        Assert.True(store.Completed);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public async Task No_action_does_not_enter_the_content_phase()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.nocontent"),
            "idem.opening.nocontent",
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
                decisionId: "adec.worker.nocontent1"));
        adapter.EnqueueContent(new ModelContentTextDelta("should-not-publish"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, result.Outcome);
        Assert.Empty(session.AgentMessages);
        Assert.True(store.Completed);
    }

    [Fact]
    public async Task Terminal_publication_claimed_invocation_resumes_content_without_control()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.resume0001"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.PublicationPathClaimed, completed.OutcomeCode);
        var inner = new DeterministicFakeModelExecutionAdapter();
        inner.EnqueueContent(
            new ModelContentTextDelta("Hi"),
            new ModelContentCompleted());
        var adapter = new CountingModelExecutionPort(inner);
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Published, result.Outcome);
        Assert.Equal(0, adapter.ExecuteCount);
        Assert.Equal("Hi", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Complete, session.AgentMessages[0].CompletionState);
        Assert.True(store.Completed);
    }

    [Fact]
    public async Task Completed_with_zero_fragments_terminalizes_the_claimed_publication_path()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.emptycomp1"));
        adapter.EnqueueContent(new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.PublicationFailed, result.Outcome);
        Assert.True(store.Completed);
        Assert.Empty(session.AgentMessages);
        Assert.False(session.HasOpenAgentContentPublication(invocationId));
        Assert.Equal(TurnStates.Cancelled, session.Turns[0].State);
        Assert.Equal(ResponseSlotStates.Cancelled, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public async Task Redelivery_after_a_visible_delta_seals_incomplete_instead_of_duplicating_text()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.dupdelta01"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.PublicationPathClaimed, completed.OutcomeCode);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.visible.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        var inner = new DeterministicFakeModelExecutionAdapter();
        inner.EnqueueContent(
            new ModelContentTextDelta("Hel"),
            new ModelContentTextDelta("lo"),
            new ModelContentCompleted());
        var adapter = new CountingModelExecutionPort(inner);
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.PublicationIncomplete, result.Outcome);
        Assert.Equal(0, adapter.StreamCount);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
        Assert.Single(session.AgentMessages[0].Fragments);
        Assert.True(store.Completed);
    }

    [Fact]
    public async Task Cancellation_after_a_visible_fragment_seals_incomplete()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        using var workerCancellation = new CancellationTokenSource();
        var adapter = new CancelAfterFirstDeltaPort(workerCancellation);
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.cancelvis1"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(workerCancellation.Token);

        Assert.Equal(DurableInvocationWorkOutcomes.PublicationIncomplete, result.Outcome);
        Assert.True(store.Completed);
        Assert.Equal("Hel", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public async Task Cancellation_before_first_visibility_releases_the_claim()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new CancelBeforeFirstDeltaPort();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.cancelpre1"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(store.Completed);
        Assert.Empty(session.AgentMessages);
        Assert.True(session.HasOpenAgentContentPublication(invocationId));
    }

    [Fact]
    public async Task In_flight_bound_before_first_fragment_releases_the_claim_for_retry()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues();
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values with
            {
                InvocationBounds = values.InvocationBounds! with
                {
                    CooldownSeconds = 0,
                    DuplicateSuppressionWindowSeconds = 0,
                },
                StreamingPublicationBounds = new StreamingPublicationBounds(512, 40, 64, 8_192, 1),
            }));
        var firstAdmitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var firstInvocationId = firstAdmitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            firstInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(firstInvocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(firstInvocationId, 1, "a", "agen.inflight.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);
        var secondAdmitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.2", "turn.2", "slot.2", "trig.participant.2", "idem.p.2", session.LastCommittedAt);
        Assert.True(secondAdmitted.Succeeded, secondAdmitted.OutcomeCode);
        var secondInvocationId = secondAdmitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            secondInvocationId,
            SessionRuntimeTestFixtures.Envelope(
                secondInvocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.inflight01"),
            session.LastCommittedAt).PublicationPathClaimed);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueContent(new ModelContentTextDelta("b"), new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, secondInvocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(store.Completed);
        Assert.Single(session.AgentMessages);
        Assert.True(session.HasOpenAgentContentPublication(secondInvocationId));
    }

    [Fact]
    public async Task Stream_end_without_completed_event_seals_visible_prefix_incomplete()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.nocomplete1"));
        adapter.EnqueueContent(new ModelContentTextDelta("Hi"));
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.PublicationIncomplete, result.Outcome);
        Assert.True(store.Completed);
        Assert.Equal("Hi", session.AgentMessages[0].AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public async Task Rate_limit_before_first_fragment_releases_the_claim_for_retry()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues();
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values with
            {
                InvocationBounds = values.InvocationBounds! with
                {
                    CooldownSeconds = 0,
                    DuplicateSuppressionWindowSeconds = 0,
                },
                StreamingPublicationBounds = new StreamingPublicationBounds(512, 1, 64, 8_192, 2),
            }));
        var firstAdmitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var firstInvocationId = firstAdmitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            firstInvocationId,
            SessionRuntimeTestFixtures.EmitMessage(firstInvocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(firstInvocationId, 1, "a", "agen.rate.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);
        var secondAdmitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.2",
            "turn.2",
            "slot.2",
            "trig.participant.2",
            "idem.p.2",
            session.LastCommittedAt);
        Assert.True(secondAdmitted.Succeeded, secondAdmitted.OutcomeCode);
        var secondInvocationId = secondAdmitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            secondInvocationId,
            SessionRuntimeTestFixtures.Envelope(
                secondInvocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.rate000001"),
            session.LastCommittedAt).PublicationPathClaimed);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueContent(new ModelContentTextDelta("b"), new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, secondInvocationId);
        var processor = CreateProcessor(adapter, session, store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(store.Completed);
        Assert.Single(session.AgentMessages);
        Assert.Equal("a", session.AgentMessages[0].AssembleExactText());
    }

    [Fact]
    public async Task Content_phase_does_not_complete_the_claim_when_fragment_persist_fails()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.persist", "idem.p.persist", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.worker.persist001"));
        adapter.EnqueueContent(new ModelContentTextDelta("Hi"), new ModelContentCompleted());
        var store = new MemoryWorkStore(session.Ownership, invocationId);
        var persist = new PassThroughAgentResponsePublicationPersistPort(persistSucceeded: false);
        var processor = CreateProcessor(adapter, session, store, publicationPersist: persist);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(store.Completed);
        Assert.True(persist.FragmentPersists > 0);
        Assert.Equal(0, persist.SealPersists);
    }

    [Fact]
    public async Task Unprocessable_oldest_item_does_not_block_later_pending_work_on_the_next_claim()
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

    [Fact]
    public async Task Fair_claim_serves_another_organization_after_completing_the_oldest_partition()
    {
        var organizationA = SessionRuntimeTestFixtures.CreateOwnership();
        var organizationB = organizationA with
        {
            OrganizationId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            ActivityId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            SessionId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        };
        var store = new MemoryWorkStore();
        store.Enqueue(organizationA, "ainv.fair.a1");
        store.Enqueue(organizationA, "ainv.fair.a2");
        store.Enqueue(organizationB, "ainv.fair.b1");

        var first = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        await store.MarkCompletedAsync(first!, CancellationToken.None);
        var second = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

        Assert.Equal("ainv.fair.a1", first!.AgentInvocationId);
        Assert.Equal("ainv.fair.b1", second!.AgentInvocationId);
    }

    [Fact]
    public async Task Fair_claim_serves_another_organization_while_the_oldest_partition_is_still_claimed()
    {
        var organizationA = SessionRuntimeTestFixtures.CreateOwnership();
        var organizationB = organizationA with
        {
            OrganizationId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            ActivityId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            SessionId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        };
        var store = new MemoryWorkStore();
        store.Enqueue(organizationA, "ainv.fair.outstanding.a1");
        store.Enqueue(organizationA, "ainv.fair.outstanding.a2");
        store.Enqueue(organizationA, "ainv.fair.outstanding.a3");
        store.Enqueue(organizationB, "ainv.fair.outstanding.b1");

        var first = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
        var second = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken.None);

        Assert.Equal("ainv.fair.outstanding.a1", first!.AgentInvocationId);
        Assert.Equal("ainv.fair.outstanding.b1", second!.AgentInvocationId);
        Assert.Equal(DurableSessionWorkStates.Claimed, first.State);
        Assert.Equal(DurableSessionWorkStates.Claimed, second.State);
    }

    [Fact]
    public async Task Idle_claim_records_bounded_backlog_and_claim_labels()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var processor = CreateProcessor(adapter, pending: false, telemetry: telemetry);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Idle, result.Outcome);
        Assert.Contains(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.WorkClaim
                && item.Labels[SessionRuntimeTelemetryLabelKeys.Outcome] == SessionRuntimeTelemetryValues.Idle);
        Assert.DoesNotContain(
            sink.Points,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.WorkBacklog);
        Assert.DoesNotContain(sink.AllLabelValues(), value => Guid.TryParse(value, out _));
    }

    private static DurableInvocationWorkProcessor CreateProcessor(
        DeterministicFakeModelExecutionAdapter adapter,
        bool pending,
        ISessionRuntimeTelemetry? telemetry = null)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var store = new MemoryWorkStore(session.Ownership, "ainv.missing", enqueue: pending);
        return CreateProcessor(adapter, session, store, telemetry: telemetry);
    }

    private static DurableInvocationWorkProcessor CreateProcessor(
        IModelExecutionPort adapter,
        SessionRuntime session,
        IDurableInvocationWorkStore store,
        ISessionRuntimeTelemetry? telemetry = null,
        IAgentResponsePublicationPersistPort? publicationPersist = null,
        IProviderRequestAdmissionPort? provenanceWriter = null,
        InstalledModelDeploymentProfile? profile = null) =>
        CreateProcessor(adapter, new MemorySessionGateway(session), store, telemetry, publicationPersist, provenanceWriter, profile);

    private static DurableInvocationWorkProcessor CreateProcessor(
        IModelExecutionPort adapter,
        IInvocationWorkSessionGateway gateway,
        IDurableInvocationWorkStore store,
        ISessionRuntimeTelemetry? telemetry = null,
        IAgentResponsePublicationPersistPort? publicationPersist = null,
        IProviderRequestAdmissionPort? provenanceWriter = null,
        InstalledModelDeploymentProfile? profile = null) =>
        new(
            store,
            gateway,
            adapter,
            new CompleteInvocationHandler(telemetry),
            new DurableInvocationWorkSettings(
                SessionRuntimeTestFixtures.CreateActor(),
                "worker.session_runtime",
                65_536,
                InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(
                    profile ?? SessionRuntimeTestFixtures.CreateInstalledProfile()),
                CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                    SessionRuntimeTestFixtures.CreateCatalogRecord(
                        SessionRuntimeTestFixtures.CreateOwnership().OrganizationId))),
            publicationPersist ?? PassThroughAgentResponsePublicationPersistPort.Succeed,
            provenanceWriter ?? new InMemoryModelProviderAttemptProvenanceWriter(),
            telemetry);

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
        private readonly Dictionary<DurableWorkClaimPartitionKey, DateTimeOffset> _lastServed = [];
        private long _queueClock;
        private long _servedClock;

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
            var claimable = _slots
                .Where(slot =>
                    slot.Item.State == DurableSessionWorkStates.Pending
                    || (slot.Item.State == DurableSessionWorkStates.Claimed && slot.LeaseExpired))
                .Select(slot => (
                    Slot: slot,
                    Candidate: new DurableWorkClaimCandidate(
                        new DurableWorkClaimPartitionKey(
                            slot.Item.Ownership.OrganizationId,
                            slot.Item.Ownership.ActivityId),
                        slot.Item.WorkId,
                        DateTimeOffset.UnixEpoch.AddTicks(slot.QueueOrder))))
                .ToArray();
            var selected = DurableWorkFairClaimSelector.SelectHead(
                claimable.Select(item => item.Candidate).ToArray(),
                _lastServed);
            if (selected is null)
            {
                return Task.FromResult<DurableInvocationWorkItem?>(null);
            }

            var candidate = claimable.Single(item => item.Candidate.WorkId == selected.WorkId).Slot;

            ClaimCount++;
            candidate.Item.State = DurableSessionWorkStates.Claimed;
            candidate.Item.ClaimLeaseUntil = DateTimeOffset.UtcNow.Add(lease);
            candidate.LeaseExpired = false;
            candidate.QueueOrder = _queueClock++;
            _lastServed[new DurableWorkClaimPartitionKey(
                candidate.Item.Ownership.OrganizationId,
                candidate.Item.Ownership.ActivityId)] = DateTimeOffset.UnixEpoch.AddTicks(++_servedClock);
            return Task.FromResult<DurableInvocationWorkItem?>(candidate.Item);
        }

        public Task<DurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(CancellationToken cancellationToken)
        {
            var claimable = _slots
                .Where(slot =>
                    slot.Item.State == DurableSessionWorkStates.Pending
                    || (slot.Item.State == DurableSessionWorkStates.Claimed && slot.LeaseExpired))
                .ToArray();
            var partitions = claimable
                .Select(slot => (slot.Item.Ownership.OrganizationId, slot.Item.Ownership.ActivityId))
                .Distinct()
                .Count();
            return Task.FromResult(new DurableWorkBacklogSnapshot(claimable.Length, partitions));
        }

        public Task ReleaseToPendingAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slot = Require(work);
            slot.Item.State = DurableSessionWorkStates.Pending;
            slot.LeaseExpired = false;
            slot.QueueOrder = _queueClock++;
            return Task.CompletedTask;
        }

        public Task MarkCompletedAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slot = Require(work);
            Completed = true;
            slot.Item.State = DurableSessionWorkStates.Completed;
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
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<LoadedInvocationWorkSession?>(
                ownership == session.Ownership
                    ? new LoadedInvocationWorkSession(session, session.Binding, session.SessionVersion)
                    : null);
        }

        public Task<DateTimeOffset> ReadAuthoritativeUtcAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var floor = SessionRuntimeTestFixtures.T0.AddSeconds(2);
            return Task.FromResult(session.LastCommittedAt > floor ? session.LastCommittedAt : floor);
        }

        public Task<bool> TrySaveCompletionAsync(
            SessionOwnership ownership,
            long expectedSessionVersion,
            SessionRuntime runtime,
            AgentInvocation invocation,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> TryAuthorizeModelDisclosureAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(ownership);
            return Task.FromResult(true);
        }
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

        public Task<bool> TryAuthorizeModelDisclosureAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            inner.TryAuthorizeModelDisclosureAsync(ownership, cancellationToken);
    }

    private sealed class DenyingModelDisclosureGateway(IInvocationWorkSessionGateway inner) : IInvocationWorkSessionGateway
    {
        public Task<LoadedInvocationWorkSession?> LoadAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            inner.LoadAsync(ownership, cancellationToken);

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

        public Task<bool> TryAuthorizeModelDisclosureAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(ownership);
            return Task.FromResult(false);
        }
    }

    private sealed class DenyAfterSuccessfulDisclosuresGateway(
        IInvocationWorkSessionGateway inner,
        int permitCount) : IInvocationWorkSessionGateway
    {
        private int _permitted;

        public Task<LoadedInvocationWorkSession?> LoadAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            inner.LoadAsync(ownership, cancellationToken);

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

        public Task<bool> TryAuthorizeModelDisclosureAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(ownership);
            if (_permitted >= permitCount)
            {
                return Task.FromResult(false);
            }

            _permitted++;
            return Task.FromResult(true);
        }
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

        public Task<DurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(CancellationToken cancellationToken) =>
            inner.ReadClaimableSnapshotAsync(cancellationToken);
    }

    private sealed class CountingModelExecutionPort(IModelExecutionPort inner) : IModelExecutionPort
    {
        public int ExecuteCount { get; private set; }

        public int StreamCount { get; private set; }

        public Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;
            return inner.ExecuteAsync(request, cancellationToken);
        }

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken)
        {
            StreamCount++;
            return inner.StreamParticipantVisibleContentAsync(request, cancellationToken);
        }
    }

    private sealed class CancelAfterFirstDeltaPort(CancellationTokenSource workerCancellation) : IModelExecutionPort
    {
        private readonly DeterministicFakeModelExecutionAdapter _inner = new();

        public void EnqueueEnvelope(EnvelopeRecommendation envelope) => _inner.EnqueueEnvelope(envelope);

        public Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken) =>
            _inner.ExecuteAsync(request, cancellationToken);

        public async IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new ModelContentTextDelta("Hel");
            workerCancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private sealed class CancelBeforeFirstDeltaPort : IModelExecutionPort
    {
        private readonly DeterministicFakeModelExecutionAdapter _inner = new();

        public void EnqueueEnvelope(EnvelopeRecommendation envelope) => _inner.EnqueueEnvelope(envelope);

        public Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken) =>
            _inner.ExecuteAsync(request, cancellationToken);

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken)
        {
            throw new OperationCanceledException(cancellationToken);
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
