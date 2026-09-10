using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;
using Xunit;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class DeterministicPayloadTestSupport
{
    internal sealed record ExecutionContext(
        EvaluationDurableWorkItem Claimed,
        DeterministicEvaluatorExecutionRequest Request,
        EvaluationDecision<DeterministicEvaluatorExecutionResult> First,
        DeterministicEvaluatorExecutionService Service);

    internal static async Task<ExecutionContext> ExecuteAndPersistAsync(
        PostgresIntegrationFixture fixture,
        CancellationToken cancellationToken)
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            fixture,
            Guid.CreateVersion7().ToString("N"),
            cancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), cancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            cancellationToken);
        Assert.NotNull(claimed);

        var registry = new BuiltinEvaluatorRegistry();
        var runner = new RestrictedBuiltinDeterministicEvaluatorRunner();
        var store = new PostgresDeterministicInvocationStore(fixture.Services.ConnectionAccessor);
        var authorityStore = new PostgresEvaluationRequestAuthorityStore(fixture.Services.ConnectionAccessor);
        var procedureSource = new PostgresProtectedEvaluationProcedureSource(fixture.Services.ConnectionAccessor);
        var outputStore = new PostgresProtectedDeterministicOutputStore(fixture.Services.ConnectionAccessor);
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
            cancellationToken);
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

        var first = await service.TryExecuteAndPersistAsync(request, cancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        return new ExecutionContext(claimed, request, first, service);
    }

    internal static async Task<ExecutionContext> ExecuteAttemptOnlyAsync(
        PostgresIntegrationFixture fixture,
        CancellationToken cancellationToken)
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            fixture,
            Guid.CreateVersion7().ToString("N"),
            cancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), cancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            cancellationToken);
        Assert.NotNull(claimed);

        var registry = new BuiltinEvaluatorRegistry();
        var runner = new RestrictedBuiltinDeterministicEvaluatorRunner();
        var store = new PostgresDeterministicInvocationStore(fixture.Services.ConnectionAccessor);
        var authorityStore = new PostgresEvaluationRequestAuthorityStore(fixture.Services.ConnectionAccessor);
        var procedureSource = new PostgresProtectedEvaluationProcedureSource(fixture.Services.ConnectionAccessor);
        var service = new DeterministicEvaluatorExecutionService(
            authorityStore,
            procedureSource,
            registry,
            runner,
            store,
            new NoOpOutputStore());

        var rubric = prepared.Request.FrozenInput.Rubric;
        var payload = await procedureSource.GetCanonicalUtf8Async(
            claimed!.Ownership.OrganizationId,
            rubric.SourceId,
            rubric.SourceVersionId,
            rubric.ContentDigest,
            cancellationToken);
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

        var first = await service.TryExecuteAndPersistAsync(request, cancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        return new ExecutionContext(claimed, request, first, service);
    }

    private sealed class NoOpOutputStore : IProtectedDeterministicOutputStore
    {
        public Task<EvaluationDecision<bool>> TryPersistAsync(
            ProtectedDeterministicOutputPersistCommand command,
            CancellationToken cancellationToken) =>
            Task.FromResult(EvaluationDecision<bool>.Ok(true));

        public Task<EvaluationSafeFactProjection?> TryLoadProjectionAsync(
            Guid organizationId,
            Guid requestId,
            Guid deterministicAttemptId,
            string expectedContentDigest,
            CancellationToken cancellationToken) =>
            Task.FromResult<EvaluationSafeFactProjection?>(null);
    }
}
