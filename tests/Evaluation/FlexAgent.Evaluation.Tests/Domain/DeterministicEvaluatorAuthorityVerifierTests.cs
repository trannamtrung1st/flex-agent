using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicEvaluatorAuthorityVerifierTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly byte[] ProcedureUtf8 = EvaluationFixtures.LoadSyntheticProcedureUtf8();
    private static readonly ExactSourceIdentity ProcedureRef = EvaluationFixtures.SyntheticProcedureRef();

    [Fact]
    public void Admitted_authority_and_matching_procedure_payload_passes()
    {
        var request = CreateRequest(Procedure.Criteria[0], Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request);

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(ProcedureRef, ProcedureUtf8),
            request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(Procedure.ProcedureId, result.Value!.Procedure.ProcedureId);
    }

    [Fact]
    public void Wrong_request_id_is_rejected()
    {
        var request = CreateRequest(Procedure.Criteria[0], Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request) with { RequestId = Guid.CreateVersion7() };

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(ProcedureRef, ProcedureUtf8),
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("request_id", result.Field);
    }

    [Fact]
    public void Wrong_invocation_attempt_is_rejected()
    {
        var request = CreateRequest(Procedure.Criteria[0], Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request) with { InvocationAttemptId = Guid.CreateVersion7() };

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(ProcedureRef, ProcedureUtf8),
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("invocation_attempt_id", result.Field);
    }

    [Fact]
    public void Wrong_ownership_is_rejected()
    {
        var request = CreateRequest(Procedure.Criteria[0], Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request) with
        {
            Ownership = EvaluationFixtures.PerturbOwnership("session"),
        };

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(ProcedureRef, ProcedureUtf8),
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("ownership", result.Field);
    }

    [Fact]
    public void Alternate_valid_procedure_digest_is_rejected()
    {
        var request = CreateRequest(Procedure.Criteria[0], Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request);
        var alternateDigest = new string('9', 64);
        var alternateRef = ExactSourceIdentity.TryCreate(
            ProcedureRef.SourceKey,
            ProcedureRef.SourceId,
            ProcedureRef.SourceVersionId,
            alternateDigest).Value!;

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(alternateRef, ProcedureUtf8),
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("procedure", result.Field);
    }

    [Fact]
    public void Procedure_source_version_drift_is_rejected()
    {
        var request = CreateRequest(Procedure.Criteria[0], Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request);
        var driftedPayload = new ProtectedCanonicalUtf8(
            ProcedureRef.SourceId,
            Guid.CreateVersion7(),
            ProcedureUtf8,
            ProcedureRef.ContentDigest);

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            driftedPayload,
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("procedure_ref", result.Field);
    }

    [Fact]
    public void Binding_matching_substituted_procedure_but_not_admitted_authority_is_rejected()
    {
        var criterion = Procedure.Criteria[0];
        var substitutedBinding = criterion.DeterministicEvaluator! with
        {
            EvaluatorDigest = new string('9', 64),
        };
        var request = CreateRequest(criterion, substitutedBinding);
        var authority = CreateAuthority(request);

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(ProcedureRef, ProcedureUtf8),
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("evaluator_digest", result.Field);
    }

    [Fact]
    public void Agent_judgment_criterion_is_rejected_before_runner_authority()
    {
        var criterion = Procedure.Criteria[2];
        var request = CreateRequest(criterion, Procedure.Criteria[0].DeterministicEvaluator!);
        var authority = CreateAuthority(request);

        var result = DeterministicEvaluatorAuthorityVerifier.TryVerify(
            authority,
            CreatePayload(ProcedureRef, ProcedureUtf8),
            request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("evaluator_mode", result.Field);
    }

    private static AdmittedEvaluationRequestAuthority CreateAuthority(
        DeterministicEvaluatorExecutionRequest request) =>
        new(
            request.RequestId,
            request.InvocationAttemptId,
            request.Ownership,
            new string('f', 64),
            ProcedureRef,
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
}
