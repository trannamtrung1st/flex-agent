using System.Collections.Concurrent;
using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class DurableInvocationWorkCrashRecoveryTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Crash_after_claim_keeps_the_lease_until_database_time_reclaim()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.crash.claim", "idem.crash.claim");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(prepared.InvocationId, "adec.crash.claim00001"));
        var gateway = new FaultInjectingSessionGateway(CreateGateway(prepared)) { FailNextLoad = 1 };
        var processor = CreateProcessor(store, gateway, adapter, prepared);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken));
        Assert.Equal(DurableSessionWorkStates.Claimed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        Assert.Equal(0, adapter.ExecuteCount);
        Assert.Equal(0, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));

        var blocked = await processor.TryProcessNextAsync(CancellationToken);
        Assert.Equal(DurableInvocationWorkOutcomes.Idle, blocked.Outcome);

        await ExpireLeaseAsync(prepared.Binding.Ownership);
        var recovered = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, recovered.Outcome);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.Equal(1, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
    }

    [Fact]
    public async Task Crash_after_provider_return_retries_without_a_decision_until_commit()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.crash.provider", "idem.crash.provider");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var adapter = new CrashAfterReturnPort(EnqueueNoAction(prepared.InvocationId, "adec.crash.provider01", copies: 2))
        {
            FailAfterNextReturn = 1,
        };
        var processor = CreateProcessor(store, CreateGateway(prepared), adapter, prepared);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken));
        Assert.Equal(DurableSessionWorkStates.Claimed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        Assert.Equal(0, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
        Assert.Equal(1, adapter.ExecuteCount);

        await ExpireLeaseAsync(prepared.Binding.Ownership);
        var recovered = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, recovered.Outcome);
        Assert.Equal(2, adapter.ExecuteCount);
        Assert.Equal(1, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
    }

    [Fact]
    public async Task Crash_during_decision_commit_does_not_persist_and_retries_to_one_decision()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.crash.commit", "idem.crash.commit");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(prepared.InvocationId, "adec.crash.commit0001", copies: 2));
        var gateway = new FaultInjectingSessionGateway(CreateGateway(prepared)) { FailNextSave = 1 };
        var processor = CreateProcessor(store, gateway, adapter, prepared);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken));
        Assert.Equal(0, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
        Assert.Equal(DurableSessionWorkStates.Claimed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        Assert.Equal(1, adapter.ExecuteCount);

        await ExpireLeaseAsync(prepared.Binding.Ownership);
        var recovered = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, recovered.Outcome);
        Assert.Equal(2, adapter.ExecuteCount);
        Assert.Equal(1, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
    }

    [Fact]
    public async Task Failed_durable_work_acknowledgement_after_decision_commit_reconciles_without_a_second_provider_call()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.crash.ack", "idem.crash.ack");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var inner = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        // Throw before MarkCompletedAsync so Decision is committed and the
        // durable-work acknowledgement itself fails (not a lost successful-ack response).
        var store = new FaultInjectingWorkStore(inner) { FailNextMarkCompleted = 1 };
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(prepared.InvocationId, "adec.crash.ack0000001"));
        var processor = CreateProcessor(store, CreateGateway(prepared), adapter, prepared);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken));
        Assert.Equal(1, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
        Assert.Equal(DurableSessionWorkStates.Claimed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        Assert.Equal(1, adapter.ExecuteCount);

        await ExpireLeaseAsync(prepared.Binding.Ownership);
        var recovered = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Reconciled, recovered.Outcome);
        Assert.Equal(InvocationCompletionOutcomeCodes.AlreadyTerminal, recovered.CompletionOutcomeCode);
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.Equal(1, await CountDecisionsAsync(prepared.Binding.Ownership, prepared.InvocationId));
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
    }

    [Fact]
    public async Task Host_clock_still_inside_the_lease_does_not_block_database_time_reclaim()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.crash.skew", "idem.crash.skew");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromMinutes(30), CancellationToken);
        Assert.NotNull(claimed);
        Assert.True(claimed!.ClaimLeaseUntil > DateTimeOffset.UtcNow.AddMinutes(20));

        await ExpireLeaseAsync(prepared.Binding.Ownership);
        var reclaimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(claimed.WorkId, reclaimed!.WorkId);
        Assert.Equal(DurableSessionWorkStates.Claimed, reclaimed.State);
    }

    [Fact]
    public async Task Stale_lease_acknowledgement_does_not_complete_a_reclaimed_claim()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.crash.cas", "idem.crash.cas");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var original = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.NotNull(original);

        await ExpireLeaseAsync(prepared.Binding.Ownership);
        var reclaimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.NotNull(reclaimed);

        await store.MarkCompletedAsync(original!, CancellationToken);

        Assert.Equal(DurableSessionWorkStates.Claimed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        await store.MarkCompletedAsync(reclaimed!, CancellationToken);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
    }

    [Fact]
    public async Task Unprocessable_oldest_item_does_not_block_later_pending_work_on_the_next_claim()
    {
        var prepared = await PrepareAdmittedWorkAsync(
            "trig.crash.poison",
            "idem.crash.poison",
            insertOlderUnprocessableWork: true);
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var adapter = new CountingModelExecutionPort(EnqueueNoAction(prepared.InvocationId, "adec.crash.poison0001"));
        var processor = CreateProcessor(
            new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor),
            CreateGateway(prepared),
            adapter,
            prepared);

        var first = await processor.TryProcessNextAsync(CancellationToken);
        var second = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, first.Outcome);
        Assert.Equal("ainv.orphan.poison0001", first.AgentInvocationId);
        Assert.Equal(DurableInvocationWorkOutcomes.Decided, second.Outcome);
        Assert.Equal(prepared.InvocationId, second.AgentInvocationId);
        Assert.Equal(1, adapter.ExecuteCount);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(
            prepared.Binding.Ownership,
            prepared.InvocationId));
        Assert.Equal(DurableSessionWorkStates.Pending, await ReadWorkStateAsync(
            prepared.Binding.Ownership,
            "ainv.orphan.poison0001"));
    }

    [Fact]
    public async Task Concurrent_workers_complete_two_independent_sessions()
    {
        var first = await PrepareAdmittedWorkAsync("trig.crash.w1", "idem.crash.w1");
        var second = await PrepareAdmittedWorkAsync("trig.crash.w2", "idem.crash.w2");
        await using var otherWork = await HoldOtherClaimableWorkAsync(
            first.Binding.Ownership,
            second.Binding.Ownership);
        var adapter = new InvocationKeyedModelExecutionPort();
        adapter.Register(first.InvocationId, EnqueueNoAction(first.InvocationId, "adec.crash.w1dec00001"));
        adapter.Register(second.InvocationId, EnqueueNoAction(second.InvocationId, "adec.crash.w2dec00001"));
        var gateway = CreateGateway(first, second);
        var firstProcessor = CreateProcessor(
            new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor),
            gateway,
            adapter,
            first);
        var secondProcessor = CreateProcessor(
            new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor),
            gateway,
            adapter,
            second);

        var results = await Task.WhenAll(
            firstProcessor.TryProcessNextAsync(CancellationToken),
            secondProcessor.TryProcessNextAsync(CancellationToken));

        Assert.Equal(2, results.Count(result => result.Outcome == DurableInvocationWorkOutcomes.Decided));
        Assert.Contains(results, result => result.AgentInvocationId == first.InvocationId);
        Assert.Contains(results, result => result.AgentInvocationId == second.InvocationId);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(first.Binding.Ownership));
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(second.Binding.Ownership));
        Assert.Equal(1, adapter.ExecuteCount(first.InvocationId));
        Assert.Equal(1, adapter.ExecuteCount(second.InvocationId));
    }

    private DurableInvocationWorkProcessor CreateProcessor(
        IDurableInvocationWorkStore store,
        IInvocationWorkSessionGateway gateway,
        IModelExecutionPort adapter,
        PreparedWork prepared) =>
        new(
            store,
            gateway,
            adapter,
            new CompleteInvocationHandler(),
            CreateWorkerSettings(prepared.Organization.ActorId));

    private PostgresInvocationWorkSessionGateway CreateGateway(params PreparedWork[] prepared)
    {
        Assert.NotEmpty(prepared);
        var bindingSource = new MemoryTrustedSessionBindingSource();
        foreach (var item in prepared)
        {
            bindingSource.Register(item.Binding);
        }

        return new PostgresInvocationWorkSessionGateway(
            Fixture.Services.ConnectionAccessor,
            new PostgresSessionRuntimeRepository(),
            bindingSource,
            CreateWorkerSettings(prepared[0].Organization.ActorId));
    }

    private async Task<PreparedWork> PrepareAdmittedWorkAsync(
        string triggerId,
        string idempotencyKey,
        bool insertOlderUnprocessableWork = false)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        if (insertOlderUnprocessableWork)
        {
            await InsertPendingWorkAsync(binding.Ownership, "ainv.orphan.poison0001");
        }

        var admitted = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger(triggerId),
                idempotencyKey,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        return new PreparedWork(organization, binding, admitted.Invocation!.AgentInvocationId);
    }

    private async Task InsertPendingWorkAsync(SessionOwnership ownership, string businessKey)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var inserted = await connection.ExecuteAsync(
            """
            INSERT INTO session_durable_work (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                work_id, work_type, business_key, state)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @WorkId, @WorkType, @BusinessKey, @State);
            """,
            new
            {
                ownership.OrganizationId,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId,
                ownership.SessionId,
                WorkId = Guid.NewGuid(),
                WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                BusinessKey = businessKey,
                State = DurableSessionWorkStates.Pending,
            });
        Assert.Equal(1, inserted);
    }

    private async Task<IAsyncDisposable> HoldOtherClaimableWorkAsync(params SessionOwnership[] keep)
    {
        var connection = new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var transaction = await connection.BeginTransactionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                SELECT work_id
                FROM session_durable_work
                WHERE (organization_id, session_id) NOT IN (
                        SELECT unnest(@OrganizationIds), unnest(@SessionIds)
                      )
                  AND (
                        state = @Pending
                        OR (
                            state = @Claimed
                            AND claim_lease_until IS NOT NULL
                            AND claim_lease_until < clock_timestamp()
                        )
                      )
                FOR UPDATE;
                """,
                new
                {
                    OrganizationIds = keep.Select(item => item.OrganizationId).ToArray(),
                    SessionIds = keep.Select(item => item.SessionId).ToArray(),
                    Pending = DurableSessionWorkStates.Pending,
                    Claimed = DurableSessionWorkStates.Claimed,
                },
                transaction,
                cancellationToken: CancellationToken));
        return new HeldWorkScope(connection, transaction);
    }

    private async Task ExpireLeaseAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var updated = await connection.ExecuteAsync(
            """
            UPDATE session_durable_work
            SET claim_lease_until = clock_timestamp() - INTERVAL '1 second'
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND work_type = @WorkType
              AND state = @Claimed;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                Claimed = DurableSessionWorkStates.Claimed,
            });
        Assert.Equal(1, updated);
    }

    private async Task<string> ReadWorkStateAsync(SessionOwnership ownership, string? businessKey = null)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<string>(
            """
            SELECT state
            FROM session_durable_work
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND work_type = @WorkType
              AND (@BusinessKey IS NULL OR business_key = @BusinessKey)
            ORDER BY last_committed_at DESC, work_id DESC
            LIMIT 1;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                BusinessKey = businessKey,
            }) ?? string.Empty;
    }

    private async Task<int> CountDecisionsAsync(SessionOwnership ownership, string invocationId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM session_decisions
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @InvocationId;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                InvocationId = invocationId,
            });
    }

    private static DurableInvocationWorkSettings CreateWorkerSettings(Guid actorId) =>
        new(
            SessionPersistenceFixtures.Actor(actorId),
            "synthetic.provider",
            "worker.session_runtime",
            65_536,
            ownership => new ModelDeploymentCredentialBindingRequest(
                ownership.OrganizationId,
                "synthetic.provider",
                "bind.opaque.0001",
                "bind.v1",
                null,
                null,
                false,
                false,
                false));

    private static DeterministicFakeModelExecutionAdapter EnqueueNoAction(
        string invocationId,
        string decisionId,
        int copies = 1)
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        for (var i = 0; i < copies; i++)
        {
            adapter.EnqueueEnvelope(
                new EnvelopeRecommendation(
                    decisionId,
                    invocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.NoAction,
                    [],
                    [],
                    NoActionReasonCategories.IntentionalSilence,
                    null));
        }

        return adapter;
    }

    private sealed class HeldWorkScope(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed record PreparedWork(
        SeededOrganization Organization,
        TrustedSessionBinding Binding,
        string InvocationId);

    private sealed class FaultInjectingSessionGateway(IInvocationWorkSessionGateway inner) : IInvocationWorkSessionGateway
    {
        public int FailNextLoad { get; set; }

        public int FailNextSave { get; set; }

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
            CancellationToken cancellationToken)
        {
            if (FailNextSave > 0)
            {
                FailNextSave--;
                throw new InvalidOperationException("Injected crash during Decision commit.");
            }

            return inner.TrySaveCompletionAsync(
                ownership,
                expectedSessionVersion,
                session,
                invocation,
                correlationId,
                cancellationToken);
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

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken) =>
            inner.StreamParticipantVisibleContentAsync(request, cancellationToken);
    }

    private sealed class InvocationKeyedModelExecutionPort : IModelExecutionPort
    {
        private readonly Dictionary<string, IModelExecutionPort> _ports = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, int> _executeCounts = new(StringComparer.Ordinal);

        public void Register(string agentInvocationId, IModelExecutionPort port)
        {
            _ports[agentInvocationId] = port;
            _executeCounts[agentInvocationId] = 0;
        }

        public int ExecuteCount(string agentInvocationId) => _executeCounts[agentInvocationId];

        public Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            _executeCounts.AddOrUpdate(request.AgentInvocationId, 1, static (_, count) => count + 1);
            return _ports[request.AgentInvocationId].ExecuteAsync(request, cancellationToken);
        }

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken) =>
            _ports[request.AgentInvocationId].StreamParticipantVisibleContentAsync(request, cancellationToken);
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
