using System.Text.Json;
using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationEvidenceLocatorStoreTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Locator_store_persists_verified_metadata_and_retries_idempotently()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);

        Guid evaluationId;
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            evaluationId = await EvaluationPersistenceTestSeed.InsertCompletedEvaluationAsync(
                connection,
                claimed!,
                CancellationToken);
        }

        var ownership = prepared.Request.FrozenInput.Ownership;
        var evidenceId = Guid.CreateVersion7();
        const string canonicalLocatorJson =
            """
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"submission.direct_text",
              "source_ref":{"source_id":"item","source_version":"rev"},
              "ownership_ref":{
                "organization_id":"org","activity_id":"act","participant_id":"part",
                "attempt_id":"att","session_id":"sess","evaluation_id":"eval"
              },
              "location":{"location_type":"whole_item","item_id":"item"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv"}
            }
            """;
        using var canonicalDocument = JsonDocument.Parse(canonicalLocatorJson);
        var locatorDigest = EvidenceLocatorDigestComputer.TryComputeLocatorDigest(canonicalDocument.RootElement);
        Assert.True(locatorDigest.Succeeded, locatorDigest.OutcomeCode);
        var records = new[]
        {
            new EvaluationEvidenceLocatorRecord(
                evidenceId,
                "submission.direct_text",
                prepared.BoundSubmission.ItemId,
                prepared.BoundSubmission.VersionId,
                prepared.BoundSubmission.ContentDigest,
                "evidence-locator.v1",
                locatorDigest.Value!,
                "whole_item",
                "verified",
                canonicalLocatorJson),
        };
        var store = new PostgresEvaluationEvidenceLocatorStore(Fixture.Services.ConnectionAccessor);

        var first = await store.TryPersistAsync(
            ownership.OrganizationId,
            evaluationId,
            claimed!.RequestId,
            ownership,
            records,
            "evaluation.integration",
            CancellationToken);
        var second = await store.TryPersistAsync(
            ownership.OrganizationId,
            evaluationId,
            claimed.RequestId,
            ownership,
            records,
            "evaluation.integration",
            CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal([evidenceId], first.Value);
        Assert.Equal([evidenceId], second.Value);

        await using var verification = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var count = await verification.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_evidence_items
            WHERE organization_id = @OrganizationId
              AND evaluation_id = @EvaluationId
              AND evidence_id = @EvidenceId;
            """,
            new
            {
                ownership.OrganizationId,
                EvaluationId = evaluationId,
                EvidenceId = evidenceId,
            });
        Assert.Equal(1, count);
    }
}
