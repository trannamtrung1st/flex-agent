using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicEvaluatorExecutionServiceTests
{
    [Fact]
    public async Task Authoritative_procedure_rejects_agent_judgment_before_runner_or_store()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var criterion = procedure.Criteria[2];
        var request = CreateRequest(criterion, procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request);
        var registry = new CountingRegistry();
        var runner = new CountingRunner();
        var store = new CountingStore();
        var service = CreateService(
            new FixedAuthorityStore(authority),
            new FixedProcedureSource(CreatePayload(authority.ProcedureRef, EvaluationFixtures.LoadSyntheticProcedureCanonicalUtf8())),
            registry,
            runner,
            store);

        var result = await service.TryExecuteAndPersistAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
        Assert.Equal(0, registry.LookupCount);
        Assert.Equal(0, runner.ExecuteCount);
        Assert.Equal(0, store.AppendCount);
    }

    [Fact]
    public async Task Substituted_valid_procedure_is_rejected_when_not_admitted_authority()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var criterion = procedure.Criteria[0];
        var request = CreateRequest(criterion, criterion.DeterministicEvaluator!);
        var admittedRef = EvaluationFixtures.SyntheticProcedureRef();
        var authority = CreateAuthority(request) with
        {
            ProcedureRef = admittedRef with { ContentDigest = new string('8', 64) },
        };
        var registry = new CountingRegistry();
        var runner = new CountingRunner();
        var store = new CountingStore();
        var service = CreateService(
            new FixedAuthorityStore(authority),
            new FixedProcedureSource(CreatePayload(admittedRef, EvaluationFixtures.LoadSyntheticProcedureCanonicalUtf8())),
            registry,
            runner,
            store);

        var result = await service.TryExecuteAndPersistAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("procedure", result.Field);
        Assert.Equal(0, runner.ExecuteCount);
        Assert.Equal(0, store.AppendCount);
    }

    [Fact]
    public async Task Tampered_valid_procedure_bytes_with_unchanged_digest_metadata_are_rejected()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var criterion = procedure.Criteria[0];
        var request = CreateRequest(criterion, criterion.DeterministicEvaluator!);
        var authority = CreateAuthority(request);
        var tamperedUtf8 = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(EvaluationFixtures.LoadSyntheticProcedureCanonicalUtf8()).Replace(
                "evalproc.p0.text.synthetic.v1",
                "evalproc.p0.text.synthetic.v2",
                StringComparison.Ordinal));
        var registry = new CountingRegistry();
        var runner = new CountingRunner();
        var store = new CountingStore();
        var service = CreateService(
            new FixedAuthorityStore(authority),
            new FixedProcedureSource(new ProtectedCanonicalUtf8(
                authority.ProcedureRef.SourceId,
                authority.ProcedureRef.SourceVersionId,
                tamperedUtf8,
                authority.ProcedureRef.ContentDigest)),
            registry,
            runner,
            store);

        var result = await service.TryExecuteAndPersistAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("procedure_content_digest", result.Field);
        Assert.Equal(0, registry.LookupCount);
        Assert.Equal(0, runner.ExecuteCount);
        Assert.Equal(0, store.AppendCount);
    }

    [Fact]
    public async Task Missing_admitted_request_authority_rejects_before_runner_or_store()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var criterion = procedure.Criteria[0];
        var request = CreateRequest(criterion, criterion.DeterministicEvaluator!);
        var service = CreateService(
            new FixedAuthorityStore(null),
            new FixedProcedureSource(CreatePayload(EvaluationFixtures.SyntheticProcedureRef(), EvaluationFixtures.LoadSyntheticProcedureCanonicalUtf8())),
            new CountingRegistry(),
            new CountingRunner(),
            new CountingStore());

        var result = await service.TryExecuteAndPersistAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("request", result.Field);
    }

    private static DeterministicEvaluatorExecutionService CreateService(
        IEvaluationRequestAuthorityStore authorityStore,
        IProtectedEvaluationProcedureSource procedureSource,
        IEvaluatorRegistry registry,
        IDeterministicEvaluatorRunner runner,
        IDeterministicInvocationStore store,
        IProtectedDeterministicOutputStore? outputStore = null) =>
        new(authorityStore, procedureSource, registry, runner, store, outputStore ?? new NoOpOutputStore());

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
            string expectedCriterionId,
            string expectedCriterionVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult<EvaluationSafeFactProjection?>(null);
    }

    private static AdmittedEvaluationRequestAuthority CreateAuthority(
        DeterministicEvaluatorExecutionRequest request) =>
        new(
            request.RequestId,
            request.InvocationAttemptId,
            request.Ownership,
            new string('f', 64),
            EvaluationFixtures.SyntheticProcedureRef(),
            EvaluatorRegistryVersions.P0);

    private static ProtectedCanonicalUtf8 CreatePayload(
        ExactSourceIdentity procedureRef,
        byte[] utf8) =>
        new(
            procedureRef.SourceId,
            procedureRef.SourceVersionId,
            utf8,
            procedureRef.ContentDigest);

    private static DeterministicEvaluatorExecutionRequest CreateRequest(
        EvaluationProcedureCriterionV1 criterion,
        DeterministicEvaluatorBindingV1 binding) =>
        new(
            EvaluationFixtures.Ownership(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            criterion.CriterionId,
            criterion.CriterionVersion,
            binding,
            new DeterministicEvaluatorCanonicalInput(
                ReadOnlyMemory<byte>.Empty,
                new string('d', 64)));

    private sealed class FixedAuthorityStore(AdmittedEvaluationRequestAuthority? authority)
        : IEvaluationRequestAuthorityStore
    {
        public Task<AdmittedEvaluationRequestAuthority?> TryLoadAsync(
            EvaluationOwnership ownership,
            Guid requestId,
            Guid invocationAttemptId,
            CancellationToken cancellationToken) =>
            Task.FromResult(authority);
    }

    private sealed class FixedProcedureSource(ProtectedCanonicalUtf8? payload)
        : IProtectedEvaluationProcedureSource
    {
        public Task<ProtectedCanonicalUtf8?> GetCanonicalUtf8Async(
            Guid organizationId,
            Guid sourceId,
            Guid sourceVersionId,
            string expectedDigest,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                payload is not null
                && payload.SourceId == sourceId
                && payload.SourceVersionId == sourceVersionId
                && string.Equals(payload.ContentDigest, expectedDigest, StringComparison.Ordinal)
                    ? payload
                    : null);
    }

    private sealed class CountingRegistry : IEvaluatorRegistry
    {
        public int LookupCount { get; private set; }

        public EvaluationDecision<EvaluatorRegistrySnapshot> TryGetRegistry(string registryVersion)
        {
            LookupCount++;
            return EvaluationDecision<EvaluatorRegistrySnapshot>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator);
        }
    }

    private sealed class CountingRunner : IDeterministicEvaluatorRunner
    {
        public int ExecuteCount { get; private set; }

        public EvaluationDecision<DeterministicEvaluatorExecutionResult> TryExecute(
            EvaluatorRegistrySnapshot registry,
            DeterministicEvaluatorExecutionRequest request,
            DateTimeOffset startedAt)
        {
            ExecuteCount++;
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(EvaluationFailureCodes.UnqualifiedEvaluator);
        }
    }

    private sealed class CountingStore : IDeterministicInvocationStore
    {
        public int AppendCount { get; private set; }

        public Task<EvaluationDecision<Guid>> TryAppendAsync(
            DeterministicInvocationAppendCommand command,
            CancellationToken cancellationToken)
        {
            AppendCount++;
            return Task.FromResult(EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.DeterministicConflict));
        }
    }
}
