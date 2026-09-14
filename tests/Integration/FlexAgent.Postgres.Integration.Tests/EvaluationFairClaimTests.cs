using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationFairClaimTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    private const int PerOrganizationConcurrency = 1;

    [Fact]
    public async Task Claim_interleaves_a_waiting_organization_after_the_oldest_partition_completes()
    {
        var first = await AdmitPreparedAsync("eval.fair.complete.a");
        var second = await AdmitPreparedAsync(
            "eval.fair.complete.b",
            first.WorkerActorId);
        await using var otherWork = await HoldOtherClaimableEvaluationWorkAsync(first, second);
        var store = first.Work;

        var claimedFirst = await TryClaimAsync(first);
        Assert.NotNull(claimedFirst);
        Assert.Equal(first.Request.RequestId, claimedFirst!.RequestId);
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

        var claimedSecond = await TryClaimAsync(second);

        Assert.NotNull(claimedSecond);
        Assert.Equal(second.Request.RequestId, claimedSecond!.RequestId);
        Assert.Equal(
            second.Request.FrozenInput.Ownership.OrganizationId,
            claimedSecond.Ownership.OrganizationId);
    }

    [Fact]
    public async Task Claim_interleaves_a_waiting_organization_while_outstanding_work_remains_claimed()
    {
        var first = await AdmitPreparedAsync("eval.fair.outstanding.a1");
        var firstSibling = await AdmitSiblingAsync(first, "eval.fair.outstanding.a2");
        var firstTail = await AdmitSiblingAsync(first, "eval.fair.outstanding.a3");
        var second = await AdmitPreparedAsync(
            "eval.fair.outstanding.b1",
            first.WorkerActorId);
        await using var otherWork = await HoldOtherClaimableEvaluationWorkAsync(
            first,
            firstSibling,
            firstTail,
            second);
        var store = first.Work;

        var claimedFirst = await TryClaimAsync(first);
        var claimedSecond = await TryClaimAsync(second);

        Assert.NotNull(claimedFirst);
        Assert.NotNull(claimedSecond);
        Assert.Equal(first.Request.RequestId, claimedFirst!.RequestId);
        Assert.Equal(second.Request.RequestId, claimedSecond!.RequestId);
        Assert.Equal("claimed", await ReadWorkStateAsync(claimedFirst));
        Assert.Equal("claimed", await ReadWorkStateAsync(claimedSecond));
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

        var claimedSecond = await TryClaimAsync(second);

        Assert.NotNull(claimedSecond);
        Assert.Equal(second.Request.RequestId, claimedSecond!.RequestId);
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
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared) =>
        prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            PerOrganizationConcurrency,
            CancellationToken);

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
