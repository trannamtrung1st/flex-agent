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
        var context = await ExecuteAndPersistAsync();

        var second = await context.Service.TryExecuteAndPersistAsync(
            EvaluatorRegistryVersions.P0,
            context.Request,
            CancellationToken);

        Assert.True(context.First.Succeeded, context.First.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(DeterministicInvocationOutcomes.Succeeded, context.First.Value!.Outcome);
        Assert.Equal(context.First.Value.DeterministicAttemptId, second.Value!.DeterministicAttemptId);
        Assert.Equal(context.First.Value.OutputContentDigest, second.Value.OutputContentDigest);

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
                context.Claimed.Ownership.OrganizationId,
                context.Claimed.RequestId,
                CriterionId = "crit.objective.word-count",
            });
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Conflicting_output_digest_on_same_attempt_id_fails_with_deterministic_conflict()
    {
        var context = await ExecuteAndPersistAsync();
        var tampered = context.First.Value! with
        {
            OutputContentDigest = new string('f', 64),
            ProtectedOutputRef = DeterministicInvocationProvenance.ProtectedOutputRef(new string('f', 64)),
        };

        var append = await context.Store.TryAppendAsync(
            new DeterministicInvocationAppendCommand(
                context.Claimed.Ownership,
                context.Claimed.RequestId,
                context.Claimed.InvocationAttemptId,
                tampered.DeterministicAttemptId,
                context.Request.CriterionId,
                context.Request.CriterionVersion,
                context.Request.Input.CanonicalInputDigest,
                context.Request.Binding,
                tampered),
            CancellationToken);

        Assert.False(append.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, append.OutcomeCode);
    }

    [Fact]
    public async Task Changed_evaluator_digest_on_same_attempt_id_fails_with_deterministic_conflict()
    {
        var context = await ExecuteAndPersistAsync();
        var binding = context.Request.Binding with
        {
            EvaluatorDigest = new string('9', 64),
        };

        var append = await context.Store.TryAppendAsync(
            new DeterministicInvocationAppendCommand(
                context.Claimed.Ownership,
                context.Claimed.RequestId,
                context.Claimed.InvocationAttemptId,
                context.First.Value!.DeterministicAttemptId,
                context.Request.CriterionId,
                context.Request.CriterionVersion,
                context.Request.Input.CanonicalInputDigest,
                binding,
                context.First.Value),
            CancellationToken);

        Assert.False(append.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, append.OutcomeCode);
    }

    [Fact]
    public async Task Changed_criterion_version_on_same_attempt_id_fails_with_deterministic_conflict()
    {
        var context = await ExecuteAndPersistAsync();

        var append = await context.Store.TryAppendAsync(
            new DeterministicInvocationAppendCommand(
                context.Claimed.Ownership,
                context.Claimed.RequestId,
                context.Claimed.InvocationAttemptId,
                context.First.Value!.DeterministicAttemptId,
                context.Request.CriterionId,
                "crit.objective.word-count.v2",
                context.Request.Input.CanonicalInputDigest,
                context.Request.Binding,
                context.First.Value),
            CancellationToken);

        Assert.False(append.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, append.OutcomeCode);
    }

    [Fact]
    public async Task Changed_invocation_attempt_on_same_attempt_id_fails_with_deterministic_conflict()
    {
        var context = await ExecuteAndPersistAsync();

        var append = await context.Store.TryAppendAsync(
            new DeterministicInvocationAppendCommand(
                context.Claimed.Ownership,
                context.Claimed.RequestId,
                Guid.CreateVersion7(),
                context.First.Value!.DeterministicAttemptId,
                context.Request.CriterionId,
                context.Request.CriterionVersion,
                context.Request.Input.CanonicalInputDigest,
                context.Request.Binding,
                context.First.Value),
            CancellationToken);

        Assert.False(append.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, append.OutcomeCode);
    }

    private async Task<ExecutionContext> ExecuteAndPersistAsync()
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
        Assert.True(first.Succeeded, first.OutcomeCode);

        return new ExecutionContext(claimed, request, first, service, store);
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

    private sealed record ExecutionContext(
        EvaluationDurableWorkItem Claimed,
        DeterministicEvaluatorExecutionRequest Request,
        EvaluationDecision<DeterministicEvaluatorExecutionResult> First,
        DeterministicEvaluatorExecutionService Service,
        PostgresDeterministicInvocationStore Store);
}
