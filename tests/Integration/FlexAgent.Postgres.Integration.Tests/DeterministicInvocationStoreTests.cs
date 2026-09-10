using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
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

    [Fact]
    public async Task Successful_invocation_materializes_protected_output_payload()
    {
        var context = await ExecuteAndPersistAsync();
        var attemptId = context.First.Value!.DeterministicAttemptId;
        var digest = context.First.Value.OutputContentDigest!;

        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var projection = await outputStore.TryLoadProjectionAsync(
            context.Claimed.Ownership.OrganizationId,
            context.Claimed.RequestId,
            attemptId,
            digest,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            CancellationToken);

        Assert.NotNull(projection);
        Assert.Equal(
            EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(attemptId),
            projection!.SourceId);

        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var payloadCount = await connection.QuerySingleAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_deterministic_payloads
            WHERE organization_id = @OrganizationId
              AND deterministic_attempt_id = @DeterministicAttemptId
              AND content_digest = @ContentDigest;
            """,
            new
            {
                context.Claimed.Ownership.OrganizationId,
                DeterministicAttemptId = attemptId,
                ContentDigest = digest,
            });
        Assert.Equal(1, payloadCount);
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
        var authorityStore = new PostgresEvaluationRequestAuthorityStore(Fixture.Services.ConnectionAccessor);
        var procedureSource = new PostgresProtectedEvaluationProcedureSource(Fixture.Services.ConnectionAccessor);
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var service = new DeterministicEvaluatorExecutionService(
            authorityStore,
            procedureSource,
            registry,
            runner,
            store,
            outputStore);

        var rubric = prepared.Request.FrozenInput.Rubric;
        var payload = await procedureSource.GetCanonicalUtf8Async(
            claimed!.Ownership.OrganizationId,
            rubric.SourceId,
            rubric.SourceVersionId,
            rubric.ContentDigest,
            CancellationToken);
        Assert.NotNull(payload);
        var procedure = EvaluationProcedureResolver.TryResolve(payload.Utf8).Value!;
        var criterion = procedure.Criteria.Single(item =>
            item.CriterionId == "crit.objective.word-count");
        var binding = criterion.DeterministicEvaluator!;
        var inputJson = JsonSerializer.Serialize(new
        {
            schema = binding.InputSchemaId,
            operation = "word_count",
            text = "alpha beta",
            minimum = 1,
            maximum = 10,
        });
        var inputBytes = Encoding.UTF8.GetBytes(inputJson);
        var inputDigest = Convert.ToHexString(SHA256.HashData(inputBytes)).ToLowerInvariant();
        var request = new DeterministicEvaluatorExecutionRequest(
            claimed.Ownership,
            claimed.RequestId,
            claimed.InvocationAttemptId,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            binding,
            new DeterministicEvaluatorCanonicalInput(inputBytes, inputDigest));

        var first = await service.TryExecuteAndPersistAsync(request, CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        return new ExecutionContext(claimed, request, first, service, store);
    }

    private sealed record ExecutionContext(
        EvaluationDurableWorkItem Claimed,
        DeterministicEvaluatorExecutionRequest Request,
        EvaluationDecision<DeterministicEvaluatorExecutionResult> First,
        DeterministicEvaluatorExecutionService Service,
        PostgresDeterministicInvocationStore Store);
}
