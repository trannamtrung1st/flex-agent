using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class DeterministicInvocationStoreTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Deterministic_invocation_store_appends_provenance_and_retries_idempotently()
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

        var registry = new BuiltinEvaluatorRegistry();
        var runner = new RestrictedBuiltinDeterministicEvaluatorRunner();
        var store = new PostgresDeterministicInvocationStore(Fixture.Services.ConnectionAccessor);
        var service = new DeterministicEvaluatorExecutionService(registry, runner, store);

        var registrySnapshot = registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var entry = registrySnapshot.Entries["eval.builtin.bounded-calc"];
        var binding = ToBinding(entry);
        var inputJson = JsonSerializer.Serialize(new
        {
            schema = entry.InputSchemaId,
            operation = "word_count",
            text = "alpha beta",
            minimum = 1,
            maximum = 10,
        });
        var inputBytes = Encoding.UTF8.GetBytes(inputJson);
        var inputDigest = Convert.ToHexString(SHA256.HashData(inputBytes)).ToLowerInvariant();
        var request = new DeterministicEvaluatorExecutionRequest(
            claimed!.Ownership,
            claimed.RequestId,
            claimed.InvocationAttemptId,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            binding,
            new DeterministicEvaluatorCanonicalInput(inputBytes, inputDigest));

        var first = await service.TryExecuteAndPersistAsync(
            EvaluatorRegistryVersions.P0,
            request,
            CancellationToken);
        var second = await service.TryExecuteAndPersistAsync(
            EvaluatorRegistryVersions.P0,
            request,
            CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(DeterministicInvocationOutcomes.Succeeded, first.Value!.Outcome);
        Assert.Equal(first.Value.DeterministicAttemptId, second.Value!.DeterministicAttemptId);

        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var count = await connection.QuerySingleAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_deterministic_attempts
            WHERE organization_id = @OrganizationId
              AND request_id = @RequestId
              AND criterion_id = @CriterionId;
            """,
            new
            {
                claimed.Ownership.OrganizationId,
                claimed.RequestId,
                CriterionId = "crit.objective.word-count",
            });
        Assert.Equal(1, count);
    }

    private static DeterministicEvaluatorBindingV1 ToBinding(EvaluatorRegistryEntry entry) =>
        new(
            entry.EvaluatorId,
            entry.EvaluatorVersion,
            entry.EvaluatorDigest,
            entry.Operation,
            entry.InputSchemaId,
            entry.OutputSchemaId,
            entry.CanonicalizationProcedure,
            entry.ConfigurationDigest,
            entry.DependencyDigest,
            entry.CpuTimeLimit,
            entry.ElapsedTimeLimit,
            entry.MemoryLimitBytes,
            entry.OutputLimitBytes,
            entry.NetworkEgress,
            entry.ExecutableSelection);
}
