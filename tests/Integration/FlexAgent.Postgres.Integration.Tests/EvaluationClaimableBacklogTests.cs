using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationClaimableBacklogTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    private const int PerOrganizationConcurrency = 1;

    [Fact]
    public async Task Claimable_backlog_matches_try_claim_when_work_is_ready()
    {
        var prepared = await AdmitPreparedAsync("claimable-ready");

        var snapshot = await ReadSnapshotAsync(prepared);
        Assert.Equal(1, snapshot.ClaimableCount);
        Assert.Equal(1, snapshot.ClaimablePartitionCount);
        Assert.NotNull(await TryClaimAsync(prepared));
    }

    [Fact]
    public async Task Claimable_backlog_excludes_revoked_delegation()
    {
        var prepared = await AdmitPreparedAsync("claimable-revoked");
        await RevokeDelegationAsync(prepared);

        await AssertNotClaimableAsync(prepared);
    }

    [Fact]
    public async Task Claimable_backlog_excludes_expired_delegation()
    {
        var prepared = await AdmitPreparedAsync("claimable-expired");
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                UPDATE service_delegations
                SET expires_at = clock_timestamp() - interval '1 second'
                WHERE delegation_id = @DelegationId;
                """,
                new { prepared.DelegationId });
        }

        await AssertNotClaimableAsync(prepared);
    }

    [Fact]
    public async Task Claimable_backlog_excludes_not_yet_effective_delegation()
    {
        var prepared = await AdmitPreparedAsync("claimable-future-effective");
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                UPDATE service_delegations
                SET effective_at = clock_timestamp() + interval '1 hour',
                    expires_at = clock_timestamp() + interval '2 hours'
                WHERE delegation_id = @DelegationId;
                """,
                new { prepared.DelegationId });
        }

        await AssertNotClaimableAsync(prepared);
    }

    [Fact]
    public async Task Claimable_backlog_excludes_wrong_service_actor()
    {
        var prepared = await AdmitPreparedAsync("claimable-wrong-actor");
        var wrongActorId = Guid.CreateVersion7();

        await AssertNotClaimableAsync(prepared, wrongActorId);
    }

    [Fact]
    public async Task Claimable_backlog_excludes_concurrency_saturated_organization()
    {
        var prepared = await AdmitPreparedAsync("claimable-concurrency");

        var beforeClaim = await ReadSnapshotAsync(prepared);
        Assert.Equal(1, beforeClaim.ClaimableCount);
        Assert.NotNull(await TryClaimAsync(prepared));

        var afterClaim = await ReadSnapshotAsync(prepared);
        Assert.Equal(0, afterClaim.ClaimableCount);
        Assert.Null(await TryClaimAsync(prepared));
    }

    [Fact]
    public async Task TryClaimAsync_claims_ready_organization_when_first_ordered_organization_is_concurrency_saturated()
    {
        var saturatedOrganization = await AdmitPreparedAsync("multi-org-saturated-primary");
        Assert.NotNull(await TryClaimAsync(saturatedOrganization));

        var saturatedSibling = await AdmitSiblingAsync(
            saturatedOrganization,
            "multi-org-saturated-sibling");
        Assert.Equal(
            saturatedOrganization.Request.FrozenInput.Ownership.OrganizationId,
            saturatedSibling.Request.FrozenInput.Ownership.OrganizationId);
        var readyOrganization = await AdmitPreparedAsync(
            "multi-org-ready",
            saturatedOrganization.WorkerActorId);

        await SetOrganizationClaimPreferenceAsync(
            saturatedOrganization,
            preferred: true);
        await SetOrganizationClaimPreferenceAsync(
            readyOrganization,
            preferred: false);

        var snapshot = await ReadSnapshotAsync(readyOrganization);
        Assert.Equal(1, snapshot.ClaimableCount);
        Assert.Equal(1, snapshot.ClaimablePartitionCount);

        var claimed = await TryClaimAsync(readyOrganization);
        Assert.NotNull(claimed);
        Assert.Equal(readyOrganization.Request.RequestId, claimed.RequestId);
        Assert.Equal(
            readyOrganization.Request.FrozenInput.Ownership.OrganizationId,
            claimed.Ownership.OrganizationId);

        var saturatedSnapshot = await ReadSnapshotAsync(saturatedOrganization);
        Assert.Equal(0, saturatedSnapshot.ClaimableCount);
        Assert.Null(await TryClaimAsync(saturatedOrganization));
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

    private async Task SetOrganizationClaimPreferenceAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared,
        bool preferred)
    {
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var workId = await connection.QuerySingleAsync<Guid>(
            """
            SELECT work_id
            FROM evaluation_durable_work
            WHERE organization_id = @OrganizationId
              AND activity_id = @ActivityId
            ORDER BY work_id
            LIMIT 1;
            """,
            new
            {
                OrganizationId = ownership.OrganizationId,
                ActivityId = ownership.ActivityId,
            });
        await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_work_claim_partitions (
                organization_id, activity_id, last_claimed_at, last_claimed_work_id)
            VALUES (
                @OrganizationId, @ActivityId, @LastClaimedAt, @WorkId)
            ON CONFLICT (organization_id, activity_id)
            DO UPDATE SET last_claimed_at = EXCLUDED.last_claimed_at;
            """,
            new
            {
                OrganizationId = ownership.OrganizationId,
                ActivityId = ownership.ActivityId,
                WorkId = workId,
                LastClaimedAt = preferred
                    ? DateTimeOffset.Parse("1970-01-01T00:00:00Z")
                    : DateTimeOffset.Parse("2099-01-01T00:00:00Z"),
            });
    }

    private Task<EvaluationDurableWorkBacklogSnapshot> ReadSnapshotAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared) =>
        prepared.Work.ReadClaimableSnapshotAsync(
            prepared.WorkerActorId,
            PerOrganizationConcurrency,
            CancellationToken);

    private Task<EvaluationDurableWorkItem?> TryClaimAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared,
        Guid? claimOwner = null) =>
        prepared.Work.TryClaimAsync(
            claimOwner ?? prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            PerOrganizationConcurrency,
            CancellationToken);

    private async Task AssertNotClaimableAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared,
        Guid? claimOwner = null)
    {
        var owner = claimOwner ?? prepared.WorkerActorId;
        var snapshot = await prepared.Work.ReadClaimableSnapshotAsync(
            owner,
            PerOrganizationConcurrency,
            CancellationToken);
        Assert.Equal(0, snapshot.ClaimableCount);
        Assert.Null(await prepared.Work.TryClaimAsync(
            owner,
            TimeSpan.FromSeconds(30),
            PerOrganizationConcurrency,
            CancellationToken));
    }

    private async Task RevokeDelegationAsync(EvaluationPersistenceTestSeed.PreparedEvaluation prepared)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE service_delegations
            SET revoked_at = clock_timestamp()
            WHERE delegation_id = @DelegationId;
            """,
            new { prepared.DelegationId });
    }
}
