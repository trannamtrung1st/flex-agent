using Dapper;
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
    [Fact]
    public async Task Claim_takes_pending_invocation_execute_work_and_sets_a_database_lease()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.pending", "idem.claim.pending");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);

        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.NotNull(claimed);
        Assert.Equal(prepared.InvocationId, claimed!.AgentInvocationId);
        Assert.Equal(prepared.Binding.Ownership, claimed.Ownership);
        Assert.Equal(DurableSessionWorkStates.Claimed, claimed.State);
        Assert.True(await ReadLeaseRemainingSecondsAsync(prepared.Binding.Ownership) > 0);
    }

    [Fact]
    public async Task Concurrent_claims_on_one_row_yield_exactly_one_winner()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.race", "idem.claim.race");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var firstStore = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var secondStore = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);

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
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);

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
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);

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

        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
        var claimedSecond = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.Equal(second.InvocationId, claimedSecond!.AgentInvocationId);
    }

    [Fact]
    public async Task Unexpired_claimed_work_is_not_taken_by_another_poll()
    {
        var prepared = await PrepareAdmittedWorkAsync("trig.claim.held", "idem.claim.held");
        await using var otherWork = await HoldOtherClaimableWorkAsync(prepared.Binding.Ownership);
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);

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
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
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
        var store = new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor);
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
        var settings = CreateWorkerSettings(prepared.Organization.ActorId);
        var processor = new DurableInvocationWorkProcessor(
            new PostgresDurableInvocationWorkStore(Fixture.Services.ConnectionAccessor),
            new PostgresInvocationWorkSessionGateway(
                Fixture.Services.ConnectionAccessor,
                new PostgresSessionRuntimeRepository(),
                bindingSource,
                settings),
            adapter,
            new CompleteInvocationHandler(),
            settings);

        var result = await processor.TryProcessNextAsync(CancellationToken);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, result.Outcome);
        Assert.Equal(prepared.InvocationId, result.AgentInvocationId);
        Assert.Equal(DurableSessionWorkStates.Completed, await ReadWorkStateAsync(prepared.Binding.Ownership));
        await using var loadScope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var loaded = await new PostgresSessionRuntimeRepository().LoadForUpdateAsync(
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

    private async Task<PreparedWork> PrepareAdmittedWorkAsync(string triggerId, string idempotencyKey)
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
