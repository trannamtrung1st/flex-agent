using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class DurableInvocationWorkClaimTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    private Guid? _workerActorId;

    private async Task<Guid> WorkerActorIdAsync()
    {
        _workerActorId ??= await Fixture.SeedWorkerActorAsync();
        return _workerActorId.Value;
    }
    [Fact]
    public async Task Claim_takes_pending_invocation_execute_work_and_sets_a_database_lease()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.pending", "idem.claim.pending");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = await CreateStoreAsync();

        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(claimed);
        Assert.Equal(prepared.InvocationId, claimed!.AgentInvocationId);
        Assert.Equal(prepared.Binding.Ownership, claimed.Ownership);
        Assert.Equal(DurableSessionWorkStates.Claimed, claimed.State);
        Assert.True(await ReadLeaseRemainingSecondsAsync(prepared.Binding.Ownership) > 0);
    }

    [Fact]
    public async Task Malformed_older_execute_envelope_does_not_block_head_of_line()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var candidateBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var foreignBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var workerActorId = await WorkerActorIdAsync();
        var foreignDelegationId = await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            foreignBinding,
            CancellationToken,
            workerActorId);
        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            candidateBinding,
            CancellationToken,
            workerActorId);
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var inserted = await connection.ExecuteAsync(
                """
                INSERT INTO session_durable_work (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    work_id, work_type, business_key, state, invocation_execute_delegation_id)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @WorkId, @WorkType, 'ainv.poison.hol0000001', @Pending, @DelegationId);
                """,
                new
                {
                    candidateBinding.Ownership.OrganizationId,
                    candidateBinding.Ownership.ActivityId,
                    candidateBinding.Ownership.ParticipantId,
                    candidateBinding.Ownership.AttemptId,
                    candidateBinding.Ownership.SessionId,
                    WorkId = Guid.NewGuid(),
                    WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                    Pending = DurableSessionWorkStates.Pending,
                    DelegationId = foreignDelegationId,
                });
            Assert.Equal(1, inserted);
        }

        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.RuntimeRepository(),
            new AdmitTrustedTriggerHandler());
        var admitted = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                candidateBinding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.claim.poison.hol"),
                "idem.claim.poison.hol",
                Guid.NewGuid(),
                "integration.test"),
            candidateBinding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        await using var otherWork = await HoldOtherClaimableWorkAsync(
            candidateBinding.Ownership,
            foreignBinding.Ownership);
        var store = await CreateStoreAsync();
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(claimed);
        Assert.Equal(admitted.Invocation!.AgentInvocationId, claimed!.AgentInvocationId);
    }

    [Fact]
    public async Task Delegation_revoke_after_lease_update_rolls_back_the_claim()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.reauth", "idem.claim.reauth");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        Guid delegationId;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            delegationId = await connection.ExecuteScalarAsync<Guid>(
                """
                SELECT invocation_execute_delegation_id
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId;
                """,
                new
                {
                    prepared.Binding.Ownership.OrganizationId,
                    prepared.Binding.Ownership.SessionId,
                });
        }

        var store = new PostgresDurableInvocationWorkStore(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.Actor(await WorkerActorIdAsync()),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel);
        store.AfterLeaseUpdateBeforeCommitAsync = async () =>
        {
            await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
            var updated = await connection.ExecuteAsync(
                """
                UPDATE service_delegations
                SET revoked_at = clock_timestamp()
                WHERE delegation_id = @DelegationId;
                """,
                new { DelegationId = delegationId });
            Assert.Equal(1, updated);
        };

        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.Null(claimed);
        Assert.Equal(DurableSessionWorkStates.Pending, await ReadWorkStateAsync(prepared.Binding.Ownership));
    }

    [Fact]
    public async Task Concurrent_claims_on_one_row_yield_exactly_one_winner()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.race", "idem.claim.race");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var firstStore = await CreateStoreAsync();
        var secondStore = await CreateStoreAsync();

        var results = await Task.WhenAll(
            firstStore.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken),
            secondStore.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken));

        var winners = results.Where(item => item is not null).ToArray();
        Assert.Single(winners);
        Assert.Equal(prepared.InvocationId, winners[0]!.AgentInvocationId);
        Assert.Contains(results, item => item is null);
    }

    [Fact]
    public async Task Claim_interleaves_a_waiting_organization_after_the_oldest_partition_completes()
    {
        var first = await PrepareAdmittedWorkAsync("trig.claim.fair.a", "idem.claim.fair.a");
        var secondOrg = await Fixture.SeedOrganizationAsync("-b");
        var secondBinding = SessionPersistenceFixtures.CreateBinding(secondOrg.OrganizationId, cooldownSeconds: 0);
        var second = await AdmitPreparedWorkAsync(secondOrg, secondBinding, "trig.claim.fair.b", "idem.claim.fair.b");
        await using var otherWork = await HoldOtherClaimableWorkAsync(first.Binding.Ownership, second.Binding.Ownership);
        var store = await CreateStoreAsync();

        var claimedFirst = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.Equal(first.InvocationId, claimedFirst!.AgentInvocationId);
        await store.MarkCompletedAsync(claimedFirst, CancellationToken);
        var claimedSecond = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.Equal(second.InvocationId, claimedSecond!.AgentInvocationId);
        Assert.Equal(second.Binding.Ownership.OrganizationId, claimedSecond.Ownership.OrganizationId);
    }

    [Fact]
    public async Task Claim_interleaves_a_waiting_organization_while_outstanding_work_remains_claimed()
    {
        var firstOrg = await Fixture.SeedOrganizationAsync("-fair-outstanding-a");
        var activityA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
        var first = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.outstanding.a1",
            "idem.claim.outstanding.a1");
        var firstSibling = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.outstanding.a2",
            "idem.claim.outstanding.a2");
        var firstTail = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.outstanding.a3",
            "idem.claim.outstanding.a3");
        var secondOrg = await Fixture.SeedOrganizationAsync("-fair-outstanding-b");
        var second = await AdmitPreparedWorkAsync(
            secondOrg,
            SessionPersistenceFixtures.CreateBinding(secondOrg.OrganizationId, cooldownSeconds: 0),
            "trig.claim.outstanding.b1",
            "idem.claim.outstanding.b1");
        await using var otherWork = await HoldOtherClaimableWorkAsync(
            first.Binding.Ownership,
            firstSibling.Binding.Ownership,
            firstTail.Binding.Ownership,
            second.Binding.Ownership);
        var store = await CreateStoreAsync();

        var claimedFirst = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        var claimedSecond = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.Equal(first.InvocationId, claimedFirst!.AgentInvocationId);
        Assert.Equal(second.InvocationId, claimedSecond!.AgentInvocationId);
        Assert.Equal(DurableSessionWorkStates.Claimed, claimedFirst.State);
        Assert.Equal(DurableSessionWorkStates.Claimed, claimedSecond.State);
    }

    [Fact]
    public async Task Claim_via_direct_row_update_advances_partition_state_for_the_next_poll()
    {
        // Legacy UPDATE still stamps scheduler state; a compatible claimer then
        // respects it. This does not make two pre-partition claimers fair.
        var firstOrg = await Fixture.SeedOrganizationAsync("-fair-trigger-a");
        var activityA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02");
        var first = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.trigger.a1",
            "idem.claim.trigger.a1");
        var firstSibling = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.trigger.a2",
            "idem.claim.trigger.a2");
        var secondOrg = await Fixture.SeedOrganizationAsync("-fair-trigger-b");
        var second = await AdmitPreparedWorkAsync(
            secondOrg,
            SessionPersistenceFixtures.CreateBinding(secondOrg.OrganizationId, cooldownSeconds: 0),
            "trig.claim.trigger.b1",
            "idem.claim.trigger.b1");
        await using var otherWork = await HoldOtherClaimableWorkAsync(
            first.Binding.Ownership,
            firstSibling.Binding.Ownership,
            second.Binding.Ownership);
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var updated = await connection.ExecuteAsync(
                """
                UPDATE session_durable_work
                SET
                    state = @Claimed,
                    claim_lease_until = clock_timestamp() + INTERVAL '30 seconds'
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND work_type = @WorkType;
                """,
                new
                {
                    first.Binding.Ownership.OrganizationId,
                    first.Binding.Ownership.SessionId,
                    WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                    Claimed = DurableSessionWorkStates.Claimed,
                });
            Assert.Equal(1, updated);
        }

        var store = await CreateStoreAsync();
        var claimedSecond = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.Equal(second.InvocationId, claimedSecond!.AgentInvocationId);
    }

    [Fact]
    public async Task Pre_partition_claim_sql_can_take_two_heads_from_the_same_busy_activity()
    {
        // Historical f4f248c selection ignores session_durable_work_claim_partitions,
        // so two overlapping legacy claimers can still drain A while B waits.
        var firstOrg = await Fixture.SeedOrganizationAsync("-fair-legacy-a");
        var activityA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03");
        var first = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.legacy.a1",
            "idem.claim.legacy.a1");
        var firstSibling = await AdmitPreparedWorkAsync(
            firstOrg,
            SessionPersistenceFixtures.CreateBinding(firstOrg.OrganizationId, cooldownSeconds: 0, activityId: activityA),
            "trig.claim.legacy.a2",
            "idem.claim.legacy.a2");
        var secondOrg = await Fixture.SeedOrganizationAsync("-fair-legacy-b");
        var second = await AdmitPreparedWorkAsync(
            secondOrg,
            SessionPersistenceFixtures.CreateBinding(secondOrg.OrganizationId, cooldownSeconds: 0),
            "trig.claim.legacy.b1",
            "idem.claim.legacy.b1");
        await using var otherWork = await HoldOtherClaimableWorkAsync(
            first.Binding.Ownership,
            firstSibling.Binding.Ownership,
            second.Binding.Ownership);

        var firstLegacy = await ClaimWithPrePartitionSqlAsync();
        var secondLegacy = await ClaimWithPrePartitionSqlAsync();

        Assert.Equal(first.InvocationId, firstLegacy);
        Assert.Equal(firstSibling.InvocationId, secondLegacy);
        Assert.NotEqual(second.InvocationId, secondLegacy);
    }

    [Fact]
    public async Task Unexpired_claimed_work_is_not_taken_by_another_poll()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.held", "idem.claim.held");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = await CreateStoreAsync();

        var first = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        var second = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(first);
        Assert.Equal(prepared.InvocationId, first!.AgentInvocationId);
        Assert.Null(second);
    }

    [Fact]
    public async Task Expired_lease_can_be_reclaimed_using_database_time()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.expire", "idem.claim.expire");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = await CreateStoreAsync();
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.NotNull(claimed);

        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var updated = await connection.ExecuteAsync(
                """
                UPDATE session_durable_work
                SET claim_lease_until = clock_timestamp() - INTERVAL '1 second'
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND work_id = @WorkId;
                """,
                new
                {
                    prepared.Binding.Ownership.OrganizationId,
                    prepared.Binding.Ownership.SessionId,
                    claimed!.WorkId,
                });
            Assert.Equal(1, updated);
        }

        var reclaimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(claimed.WorkId, reclaimed!.WorkId);
        Assert.Equal(prepared.InvocationId, reclaimed.AgentInvocationId);
        Assert.Equal(DurableSessionWorkStates.Claimed, reclaimed.State);
    }

    [Fact]
    public async Task Release_returns_work_to_pending_so_the_next_poll_can_claim_it()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.release", "idem.claim.release");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = await CreateStoreAsync();
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.NotNull(claimed);

        await store.ReleaseToPendingAsync(claimed!, CancellationToken);
        var again = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(again);
        Assert.Equal(prepared.InvocationId, again!.AgentInvocationId);
        Assert.Equal(DurableSessionWorkStates.Claimed, again.State);
    }

    [Fact]
    public async Task Processor_claims_admitted_work_records_one_decision_and_completes_the_row()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.e2e", "idem.claim.e2e");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            new EnvelopeRecommendation(
                "adec.worker.e2e000001",
                prepared.InvocationId,
                new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                DecisionDispositions.NoAction,
                [],
                [],
                NoActionReasonCategories.IntentionalSilence,
                null));
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(prepared.Binding);
        var settings = CreateWorkerSettings(prepared.Organization.ActorId, prepared.Binding.Ownership.OrganizationId);
        var processor = new DurableInvocationWorkProcessor(
            await CreateStoreAsync(),
            new PostgresInvocationWorkSessionGateway(
                Fixture.Services.ConnectionAccessor,
                SessionPersistenceFixtures.RuntimeRepository(),
                bindingSource,
                settings),
            adapter,
            new CompleteInvocationHandler(),
            settings,
            PassThroughAgentResponsePublicationPersistPort.Succeed,
            await CreateAdmissionAsync());

        var result = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, result.Outcome);
        Assert.Equal(prepared.InvocationId, result.AgentInvocationId);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        await using var loadScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await SessionPersistenceFixtures.RuntimeRepository().LoadForUpdateAsync(
            prepared.Binding.Ownership,
            prepared.Binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);
        var invocation = Assert.Single(loaded!.Invocations);
        Assert.NotNull(invocation.Decision);
        Assert.Null(invocation.ExecutionOutcome);
        Assert.Equal(AgentInvocationStatuses.Decided, invocation.Status);
    }

    [Fact]
    public async Task Processor_persists_fragments_through_the_publication_coordinator_before_completing_work()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var workerActorId = await WorkerActorIdAsync();
        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            binding,
            CancellationToken,
            workerActorId);

        var accepted = await new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler()).AcceptAsync(
            new AcceptParticipantMessageCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                0,
                "msg.claim.persist",
                "turn.claim.persist",
                "slot.claim.persist",
                "trig.claim.persist",
                "idem.claim.persist",
                Guid.NewGuid(),
                "integration.test",
                "synthetic.participant.message"),
            binding,
            CancellationToken);
        Assert.True(accepted.Succeeded, accepted.OutcomeCode);
        var invocationId = accepted.Invocation!.AgentInvocationId;
        await using var otherWork = await HoldOtherClaimableWorkAsync(binding.Ownership);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(
            new EnvelopeRecommendation(
                "adec.worker.persist0001",
                invocationId,
                new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                DecisionDispositions.Respond,
                [
                    new OutputRecommendation(
                        AgentOutputKinds.Message,
                        "out.message.primary",
                        "participant_reply",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null),
                ],
                [],
                null,
                null));
        adapter.EnqueueContent(new ModelContentTextDelta("Hi"), new ModelContentCompleted());
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(binding);
        var settings = CreateWorkerSettings(workerActorId, binding.Ownership.OrganizationId);
        var persist = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler());
        var processor = new DurableInvocationWorkProcessor(
            await CreateStoreAsync(),
            new PostgresInvocationWorkSessionGateway(
                Fixture.Services.ConnectionAccessor,
                repository,
                bindingSource,
                settings),
            adapter,
            new CompleteInvocationHandler(),
            settings,
            persist,
            await CreateAdmissionAsync());

        var result = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Published, result.Outcome);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(binding.Ownership));
        await using var loadScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);
        var message = Assert.Single(loaded!.AgentMessages);
        Assert.Equal("Hi", message.AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Complete, message.CompletionState);
        Assert.Single(message.Fragments);
    }

    [Fact]
    public async Task History_scale_claim_uses_the_claimable_partial_index()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.history", "idem.claim.history");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        await InsertCompletedHistoryAsync(prepared.Binding.Ownership, rowCount: 10_000);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition("ANALYZE session_durable_work;", cancellationToken: CancellationToken));

        var index = await connection.ExecuteScalarAsync<string?>(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename = 'session_durable_work'
              AND indexname = @IndexName;
            """,
            new { IndexName = PostgresDurableInvocationWorkStore.ClaimableIndexName });
        Assert.Equal(PostgresDurableInvocationWorkStore.ClaimableIndexName, index);

        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var planJson = await scope.Connection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "EXPLAIN (ANALYZE, FORMAT JSON) " + PostgresDurableInvocationWorkStore.ClaimCandidateSql,
                new
                {
                    WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                    Pending = DurableSessionWorkStates.Pending,
                    Claimed = DurableSessionWorkStates.Claimed,
                    ServiceActorId = await WorkerActorIdAsync(),
                    AllowedAction = AuthorizationActions.ExecuteSessionInvocation,
                },
                scope.Transaction,
                cancellationToken: CancellationToken));
        await scope.RollbackAsync(CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(planJson));
        Assert.Contains(PostgresDurableInvocationWorkStore.ClaimableIndexName, planJson, StringComparison.Ordinal);
        using var document = System.Text.Json.JsonDocument.Parse(planJson);
        var usedClaimableIndex = false;
        var seqScannedWork = false;
        WalkPlans(document.RootElement, node =>
        {
            var nodeType = node.TryGetProperty("Node Type", out var type) ? type.GetString() : null;
            var relation = node.TryGetProperty("Relation Name", out var rel) ? rel.GetString() : null;
            var indexName = node.TryGetProperty("Index Name", out var idx) ? idx.GetString() : null;
            if (string.Equals(indexName, PostgresDurableInvocationWorkStore.ClaimableIndexName, StringComparison.Ordinal))
            {
                usedClaimableIndex = true;
            }

            if (string.Equals(nodeType, "Seq Scan", StringComparison.Ordinal)
                && string.Equals(relation, "session_durable_work", StringComparison.Ordinal))
            {
                seqScannedWork = true;
            }
        });
        Assert.True(usedClaimableIndex, planJson);
        Assert.False(seqScannedWork, planJson);

        var store = await CreateStoreAsync();
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.Equal(prepared.InvocationId, claimed!.AgentInvocationId);
    }

    [Fact]
    public async Task Sampled_backlog_gauge_reads_claimable_depth_once_per_interval()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.backlog", "idem.claim.backlog");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero));
        var sampler = new DurableWorkBacklogSampler(
            await CreateStoreAsync(),
            new SessionRuntimeTelemetry(sink),
            clock);

        await sampler.SampleIfDueAsync(CancellationToken);
        await sampler.SampleIfDueAsync(CancellationToken);

        var gauge = Assert.Single(sink.Gauges);
        Assert.Equal(SessionRuntimeTelemetryInstruments.WorkBacklog, gauge.Instrument);
        Assert.True(gauge.Value >= 1);
        Assert.Equal(
            DurableSessionWorkTypes.ExecuteInvocation,
            gauge.Labels[SessionRuntimeTelemetryLabelKeys.WorkType]);
        Assert.Contains(
            gauge.Labels[SessionRuntimeTelemetryLabelKeys.BacklogBucket],
            (IReadOnlyList<string>)["n1", "n2_to_5", "n6_to_20", "n21_to_100", "n_over_100"]);
        Assert.DoesNotContain(sink.AllLabelValues(), value => Guid.TryParse(value, out _));
        Assert.DoesNotContain(
            sink.AllLabelValues(),
            value => value.Contains(prepared.InvocationId, StringComparison.Ordinal));
    }

    private async Task<PreparedWork> PrepareAdmittedWorkAsync(string triggerId, string idempotencyKey)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            binding,
            CancellationToken,
            await WorkerActorIdAsync());
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.RuntimeRepository(),
            new AdmitTrustedTriggerHandler());

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

    private async Task<PreparedWork> AdmitPreparedWorkAsync(
        SeededOrganization organization,
        TrustedSessionBinding binding,
        string triggerId,
        string idempotencyKey)
    {
        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            binding,
            CancellationToken,
            await WorkerActorIdAsync());
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.RuntimeRepository(),
            new AdmitTrustedTriggerHandler());

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

    private async Task<PostgresDurableInvocationWorkStore> CreateStoreAsync() =>
        new(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.Actor(await WorkerActorIdAsync()));

    private async Task<PostgresModelProviderAttemptProvenanceWriter> CreateAdmissionAsync()
    {
        var actorId = await WorkerActorIdAsync();
        return new PostgresModelProviderAttemptProvenanceWriter(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.Actor(actorId),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            new SyntheticConfiguredActorWorkloadIdentitySource(actorId));
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
                WHERE NOT EXISTS (
                        SELECT 1
                        FROM unnest(@OrganizationIds, @SessionIds) AS keep(organization_id, session_id)
                        WHERE keep.organization_id = session_durable_work.organization_id
                          AND keep.session_id = session_durable_work.session_id
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

    private static void WalkPlans(System.Text.Json.JsonElement node, Action<System.Text.Json.JsonElement> visit)
    {
        if (node.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                WalkPlans(child, visit);
            }

            return;
        }

        if (node.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return;
        }

        if (node.TryGetProperty("Plan", out var plan))
        {
            WalkPlans(plan, visit);
        }

        visit(node);
        if (node.TryGetProperty("Plans", out var plans))
        {
            WalkPlans(plans, visit);
        }
    }

    private async Task InsertCompletedHistoryAsync(SessionOwnership ownership, int rowCount)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var inserted = await connection.ExecuteAsync(
            """
            INSERT INTO session_durable_work (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                work_id, work_type, business_key, state)
            SELECT
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                gen_random_uuid(), @WorkType, 'hist.claim.' || gs, @Completed
            FROM generate_series(1, @RowCount) AS gs;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId,
                ownership.SessionId,
                WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                Completed = DurableSessionWorkStates.Completed,
                RowCount = rowCount,
            });
        Assert.Equal(rowCount, inserted);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private async Task<double> ReadLeaseRemainingSecondsAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<double>(
            """
            SELECT EXTRACT(EPOCH FROM (claim_lease_until - clock_timestamp()))
            FROM session_durable_work
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND work_type = @WorkType;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                WorkType = DurableSessionWorkTypes.ExecuteInvocation,
            });
    }

    private async Task<string> ReadWorkStateAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<string>(
            """
            SELECT state
            FROM session_durable_work
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND work_type = @WorkType;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                WorkType = DurableSessionWorkTypes.ExecuteInvocation,
            }) ?? string.Empty;
    }

    private async Task<string?> ClaimWithPrePartitionSqlAsync()
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        try
        {
            var invocationId = await scope.Connection.ExecuteScalarAsync<string?>(
                new CommandDefinition(
                    """
                    WITH partition_served AS (
                        SELECT organization_id, activity_id,
                               MAX(last_committed_at) FILTER (WHERE state = @Completed) AS last_served_at
                        FROM session_durable_work
                        WHERE work_type = @WorkType
                        GROUP BY organization_id, activity_id
                    ),
                    candidate AS MATERIALIZED (
                        SELECT work.organization_id, work.activity_id, work.participant_id, work.attempt_id,
                               work.session_id, work.work_id
                        FROM session_durable_work AS work
                        LEFT JOIN partition_served AS served
                          ON served.organization_id = work.organization_id
                         AND served.activity_id = work.activity_id
                        WHERE work.work_type = @WorkType
                          AND (
                                work.state = @Pending
                                OR (
                                    work.state = @Claimed
                                    AND work.claim_lease_until IS NOT NULL
                                    AND work.claim_lease_until < clock_timestamp()
                                )
                              )
                          AND NOT EXISTS (
                                SELECT 1
                                FROM session_durable_work AS older
                                WHERE older.work_type = work.work_type
                                  AND older.organization_id = work.organization_id
                                  AND older.activity_id = work.activity_id
                                  AND (
                                        older.state = @Pending
                                        OR (
                                            older.state = @Claimed
                                            AND older.claim_lease_until IS NOT NULL
                                            AND older.claim_lease_until < clock_timestamp()
                                        )
                                      )
                                  AND (older.last_committed_at, older.work_id) < (work.last_committed_at, work.work_id)
                          )
                        ORDER BY COALESCE(served.last_served_at, TIMESTAMPTZ '-infinity') ASC,
                                 work.last_committed_at ASC,
                                 work.work_id ASC
                        FOR UPDATE OF work SKIP LOCKED
                        LIMIT 1
                    )
                    UPDATE session_durable_work AS work
                    SET
                        state = @Claimed,
                        claim_lease_until = clock_timestamp() + (@LeaseSeconds * INTERVAL '1 second')
                    FROM candidate
                    WHERE work.organization_id = candidate.organization_id
                      AND work.session_id = candidate.session_id
                      AND work.work_id = candidate.work_id
                    RETURNING work.business_key;
                    """,
                    new
                    {
                        WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                        Pending = DurableSessionWorkStates.Pending,
                        Claimed = DurableSessionWorkStates.Claimed,
                        Completed = DurableSessionWorkStates.Completed,
                        LeaseSeconds = 30d,
                    },
                    scope.Transaction,
                    cancellationToken: CancellationToken));
            await scope.CommitAsync(CancellationToken);
            return invocationId;
        }
        catch
        {
            await scope.RollbackAsync(CancellationToken);
            throw;
        }
    }

    private static DurableInvocationWorkSettings CreateWorkerSettings(Guid actorId, Guid organizationId) =>
        new(
            SessionPersistenceFixtures.Actor(actorId),
            "worker.session_runtime",
            65_536,
            InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(
                SessionPersistenceFixtures.CreateInstalledProfile()),
            CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                SessionPersistenceFixtures.CreateCatalogRecord(organizationId)));

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
}
