using Dapper;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationProviderArtifactStoreTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Provider_artifact_store_appends_protected_refs_and_retries_idempotently()
    {
        var context = await SeedClaimedAsync();
        var store = new PostgresEvaluationProviderArtifactStore(Fixture.Services.ConnectionAccessor);
        var command = CreateCommand(context);

        var first = await store.TryAppendAsync(command, CancellationToken);
        var second = await store.TryAppendAsync(command, CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(first.Value, second.Value);

        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var row = await connection.QuerySingleAsync<(string RequestRef, string? ResponseRef, string Outcome)>(
            """
            SELECT protected_request_ref, protected_response_ref, outcome
            FROM evaluation_provider_artifacts
            WHERE organization_id = @OrganizationId
              AND provider_artifact_id = @ProviderArtifactId;
            """,
            new
            {
                context.Claimed.Ownership.OrganizationId,
                ProviderArtifactId = command.ProviderArtifactId,
            });
        Assert.Equal(command.ProtectedRequestRef, row.RequestRef);
        Assert.Equal(command.ProtectedResponseRef, row.ResponseRef);
        Assert.Equal(ProviderArtifactOutcomes.Succeeded, row.Outcome);
    }

    [Fact]
    public async Task Conflicting_response_ref_on_same_artifact_id_fails_with_deterministic_conflict()
    {
        var context = await SeedClaimedAsync();
        var store = new PostgresEvaluationProviderArtifactStore(Fixture.Services.ConnectionAccessor);
        var command = CreateCommand(context);
        Assert.True((await store.TryAppendAsync(command, CancellationToken)).Succeeded);

        var conflicting = command with
        {
            ProtectedResponseRef = ProviderArtifactProvenance.ProtectedResponseRef(new string('f', 64)),
        };
        var result = await store.TryAppendAsync(conflicting, CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
    }

    private async Task<SeedContext> SeedClaimedAsync()
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
        return new SeedContext(prepared, claimed!);
    }

    private static ProviderArtifactAppendCommand CreateCommand(SeedContext context)
    {
        var model = context.Prepared.Request.FrozenInput.Model;
        var criterionId = "crit.judgment.quality";
        var criterionVersion = "crit.judgment.quality.v1";
        var requestContentDigest = ProviderArtifactIdentity.ComputeRequestContentDigest(
            context.Claimed.RequestId,
            context.Claimed.InvocationAttemptId,
            criterionId,
            criterionVersion,
            model,
            "eval.instructions.p0.v1",
            [
                new EvaluationModelPermittedEvidenceV1(
                    "evidence.submission.0001",
                    "submission.direct_text",
                    new string('b', 64)),
            ]);
        return new ProviderArtifactAppendCommand(
            context.Claimed.Ownership,
            context.Claimed.RequestId,
            context.Claimed.InvocationAttemptId,
            ProviderArtifactIdentity.ComputeArtifactId(
                context.Claimed.RequestId,
                context.Claimed.InvocationAttemptId,
                criterionId,
                criterionVersion,
                requestContentDigest),
            criterionId,
            criterionVersion,
            model,
            ProviderArtifactProvenance.ProtectedRequestRef(requestContentDigest),
            ProviderArtifactProvenance.ProtectedResponseRef(new string('c', 64)),
            ProviderArtifactOutcomes.Succeeded,
            null);
    }

    private sealed record SeedContext(
        EvaluationPersistenceTestSeed.PreparedEvaluation Prepared,
        EvaluationDurableWorkItem Claimed);
}
