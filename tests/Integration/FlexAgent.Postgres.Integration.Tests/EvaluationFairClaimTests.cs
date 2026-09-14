using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationFairClaimTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    private const int SaturatedOrganizationConcurrency = 1;
    private const int MultiClaimOrganizationConcurrency = 2;

    [Fact]
    public async Task Claim_interleaves_a_waiting_organization_after_the_oldest_partition_completes()
    {
        var first = await AdmitPreparedAsync("eval.fair.complete.a1");
        var firstSibling = await AdmitSiblingAsync(first, "eval.fair.complete.a2");
        var second = await AdmitPreparedAsync(
            "eval.fair.complete.b1",
            first.WorkerActorId);
        await using var otherWork = await HoldOtherClaimableEvaluationWorkAsync(
            first,
            firstSibling,
            second);
        var store = first.Work;

        var claimedFirst = await TryClaimAsync(first, SaturatedOrganizationConcurrency);
        Assert.NotNull(claimedFirst);
        Assert.Equal(first.Request.RequestId, claimedFirst!.RequestId);
        var partitionAfterClaim = await ReadPartitionLastClaimedAtAsync(first);
        Assert.NotNull(partitionAfterClaim);

        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimedFirst,
                CancellationToken);
        }

        Assert.True(await store.MarkCompletedAsync(
            claimedFirst,
            first.WorkerActorId,
            CancellationToken));
        await AssertOrganizationHasClaimableWorkAsync(
            first,
            MultiClaimOrganizationConcurrency);
        await AssertOrganizationHasClaimableWorkAsync(
            second,
            MultiClaimOrganizationConcurrency);

        var claimedNext = await TryClaimAsync(second, MultiClaimOrganizationConcurrency);

        Assert.NotNull(claimedNext);
        Assert.Equal(second.Request.RequestId, claimedNext!.RequestId);
        Assert.Equal("pending", await ReadWorkStateAsync(firstSibling));
    }

    [Fact]
    public async Task Claim_interleaves_a_waiting_organization_while_outstanding_work_remains_claimed()
    {
        var first = await AdmitPreparedAsync("eval.fair.outstanding.a1");
        var firstSibling = await AdmitSiblingAsync(first, "eval.fair.outstanding.a2");
        var second = await AdmitPreparedAsync(
            "eval.fair.outstanding.b1",
            first.WorkerActorId);
        await using var otherWork = await HoldOtherClaimableEvaluationWorkAsync(
            first,
            firstSibling,
            second);

        var claimedFirst = await TryClaimAsync(first, MultiClaimOrganizationConcurrency);
        Assert.NotNull(claimedFirst);
        Assert.Equal(first.Request.RequestId, claimedFirst!.RequestId);
        Assert.Equal("claimed", await ReadWorkStateAsync(claimedFirst));
        await AssertOrganizationHasClaimableWorkAsync(
            firstSibling,
            MultiClaimOrganizationConcurrency);

        var claimedSecond = await TryClaimAsync(second, MultiClaimOrganizationConcurrency);

        Assert.NotNull(claimedSecond);
        Assert.Equal(second.Request.RequestId, claimedSecond!.RequestId);
        Assert.Equal("pending", await ReadWorkStateAsync(firstSibling));
    }

    [Fact]
    public async Task Claim_via_direct_row_update_advances_partition_state_for_the_next_poll()
    {
        var first = await AdmitPreparedAsync("eval.fair.trigger.a1");
        var firstSibling = await AdmitSiblingAsync(first, "eval.fair.trigger.a2");
        var second = await AdmitPreparedAsync(
            "eval.fair.trigger.b1",
            first.WorkerActorId);
        await using var otherWork = await HoldOtherClaimableEvaluationWorkAsync(
            first,
            firstSibling,
            second);
        var partitionBefore = await ReadPartitionLastClaimedAtAsync(first);

        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            var updated = await connection.ExecuteAsync(
                """
                UPDATE evaluation_durable_work
                SET
                    state = 'claimed',
                    claim_owner = @ClaimOwner,
                    claim_lease_until = clock_timestamp() + INTERVAL '30 seconds'
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId;
                """,
                new
                {
                    first.Request.FrozenInput.Ownership.OrganizationId,
                    first.Request.RequestId,
                    ClaimOwner = first.WorkerActorId,
                });
            Assert.Equal(1, updated);
        }

        var partitionAfter = await ReadPartitionLastClaimedAtAsync(first);
        Assert.NotNull(partitionAfter);
        Assert.True(
            partitionBefore is null || partitionAfter > partitionBefore,
            "Direct claim update must advance evaluation_work_claim_partitions.last_claimed_at.");
        await AssertOrganizationHasClaimableWorkAsync(
            firstSibling,
            MultiClaimOrganizationConcurrency);

        var claimedSecond = await TryClaimAsync(second, MultiClaimOrganizationConcurrency);

        Assert.NotNull(claimedSecond);
        Assert.Equal(second.Request.RequestId, claimedSecond!.RequestId);
        Assert.Equal("pending", await ReadWorkStateAsync(firstSibling));
    }

    private async Task<EvaluationPersistenceTestSeed.PreparedEvaluation> AdmitPreparedAsync(
        string key,
        Guid? workerActorId = null)
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            key,
            CancellationToken,
            workerActorId);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        return prepared;
    }

    private async Task<EvaluationPersistenceTestSeed.PreparedEvaluation> AdmitSiblingAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation sibling,
        string key)
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateSiblingAsync(
            Fixture,
            sibling,
            key,
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        return prepared;
    }

    private Task<EvaluationDurableWorkItem?> TryClaimAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared,
        int perOrganizationConcurrency) =>
        prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency,
            CancellationToken);

    private async Task AssertOrganizationHasClaimableWorkAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared,
        int perOrganizationConcurrency)
    {
        var snapshot = await prepared.Work.ReadClaimableSnapshotAsync(
            prepared.WorkerActorId,
            perOrganizationConcurrency,
            CancellationToken);
        Assert.True(snapshot.ClaimableCount >= 1);
    }

    private async Task<DateTimeOffset?> ReadPartitionLastClaimedAtAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared)
    {
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        return await connection.QuerySingleOrDefaultAsync<DateTimeOffset?>(
            """
            SELECT last_claimed_at
            FROM evaluation_work_claim_partitions
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId;
            """,
            new
            {
                OrganizationId = ownership.OrganizationId,
                ActivityId = ownership.ActivityId,
            });
    }

    private async Task<string> ReadWorkStateAsync(EvaluationPersistenceTestSeed.PreparedEvaluation prepared)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        return await connection.QuerySingleAsync<string>(
            """
            SELECT state
            FROM evaluation_durable_work
            WHERE organization_id = @OrganizationId
              AND request_id = @RequestId;
            """,
            new
            {
                prepared.Request.FrozenInput.Ownership.OrganizationId,
                prepared.Request.RequestId,
            });
    }

    private async Task<string> ReadWorkStateAsync(EvaluationDurableWorkItem work)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        return await connection.QuerySingleAsync<string>(
            """
            SELECT state
            FROM evaluation_durable_work
            WHERE organization_id = @OrganizationId
              AND work_id = @WorkId;
            """,
            new
            {
                work.Ownership.OrganizationId,
                work.WorkId,
            });
    }

    private async Task<IAsyncDisposable> HoldOtherClaimableEvaluationWorkAsync(
        params EvaluationPersistenceTestSeed.PreparedEvaluation[] keep)
    {
        var connection = new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var transaction = await connection.BeginTransactionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                SELECT work_id
                FROM evaluation_durable_work
                WHERE NOT EXISTS (
                        SELECT 1
                        FROM unnest(@OrganizationIds, @RequestIds) AS keep(organization_id, request_id)
                        WHERE keep.organization_id = evaluation_durable_work.organization_id
                          AND keep.request_id = evaluation_durable_work.request_id
                      )
                  AND (
                        state = 'pending'
                        OR (
                            state = 'claimed'
                            AND claim_lease_until IS NOT NULL
                            AND claim_lease_until < clock_timestamp())
                      )
                FOR UPDATE;
                """,
                new
                {
                    OrganizationIds = keep
                        .Select(item => item.Request.FrozenInput.Ownership.OrganizationId)
                        .ToArray(),
                    RequestIds = keep.Select(item => item.Request.RequestId).ToArray(),
                },
                transaction,
                cancellationToken: CancellationToken));
        return new HeldEvaluationWorkScope(connection, transaction);
    }

    private sealed class HeldEvaluationWorkScope(NpgsqlConnection connection, NpgsqlTransaction transaction)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await transaction.RollbackAsync();
            await connection.DisposeAsync();
        }
    }
}
