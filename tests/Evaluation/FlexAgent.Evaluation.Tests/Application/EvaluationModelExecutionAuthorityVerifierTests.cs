using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelExecutionAuthorityVerifierTests
{
    private static readonly EvaluationOwnership Ownership = EvaluationFixtures.Ownership();
    private static readonly Guid RequestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
    private static readonly Guid InvocationAttemptId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc");
    private static readonly Guid EvaluationId = Guid.Parse("11111111-1111-4111-8111-111111111115");
    private static readonly Guid DeterministicInvocationId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public void Bound_context_accepts_matching_assisted_provenance()
    {
        var result = EvaluationModelExecutionAuthorityVerifier.TryValidateBoundContext(
            CreateAssistedContext(),
            EvaluatorModes.AgentAssisted);

        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    [Fact]
    public void Bound_context_rejects_mismatched_session_ownership_ref()
    {
        var result = EvaluationModelExecutionAuthorityVerifier.TryValidateBoundContext(
            CreateAssistedContext() with
            {
                Ownership = new SessionOwnershipRefV1(
                    "org.forged.demo",
                    "act.forged.demo",
                    "part.forged.demo",
                    "att.forged.demo",
                    "sess.forged.demo"),
            },
            EvaluatorModes.AgentAssisted);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, result.OutcomeCode);
        Assert.Equal("ownership", result.Field);
    }

    [Fact]
    public void Bound_context_rejects_mismatched_request_stable_id()
    {
        var result = EvaluationModelExecutionAuthorityVerifier.TryValidateBoundContext(
            CreateAssistedContext() with { RequestStableId = "ereq.forged.0001" },
            EvaluatorModes.AgentAssisted);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("request_id", result.Field);
    }

    [Fact]
    public void Bound_context_rejects_mismatched_deterministic_invocation_stable_id()
    {
        var result = EvaluationModelExecutionAuthorityVerifier.TryValidateBoundContext(
            CreateAssistedContext() with { DeterministicInvocationStableId = "dinv.forged.0001" },
            EvaluatorModes.AgentAssisted);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_invocation_id", result.Field);
    }

    [Fact]
    public async Task Reload_authority_rejects_wrong_same_org_ownership_scope()
    {
        var result = await EvaluationModelExecutionAuthorityVerifier.TryReloadAuthorityAsync(
            CreateAssistedContext() with { OwnershipScope = EvaluationFixtures.PerturbOwnership("activity") },
            CreateAuthorityStore(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("request", result.Field);
    }

    private static EvaluationModelExecutionContext CreateAssistedContext()
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        return new EvaluationModelExecutionContext(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            Ownership,
            RequestId,
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            InvocationAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            EvaluationId,
            EvaluationFixtures.Model(),
            "eval.instructions.p0.v1",
            [
                new EvaluationModelPermittedEvidenceV1(
                    evidenceStableId,
                    "submission.direct_text",
                    new string('b', 64)),
            ],
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [evidenceStableId] = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            },
            EvaluationFixtures.VerifiedPermittedEvidence(
                evidenceStableId,
                Guid.Parse("22222222-2222-4222-8222-222222222222")),
            DeterministicInvocationId,
            EvaluationStableOwnershipReferenceFactory.StableDeterministicInvocationId(DeterministicInvocationId));
    }

    private static FixedAuthorityStore CreateAuthorityStore() =>
        new(
            new AdmittedEvaluationRequestAuthority(
                RequestId,
                InvocationAttemptId,
                Ownership,
                new string('f', 64),
                EvaluationFixtures.SyntheticProcedureRef(),
                EvaluatorRegistryVersions.P0));

    private sealed class FixedAuthorityStore(AdmittedEvaluationRequestAuthority authority) : IEvaluationRequestAuthorityStore
    {
        public Task<AdmittedEvaluationRequestAuthority?> TryLoadAsync(
            EvaluationOwnership ownership,
            Guid requestId,
            Guid invocationAttemptId,
            CancellationToken cancellationToken)
        {
            if (requestId != authority.RequestId
                || invocationAttemptId != authority.InvocationAttemptId
                || ownership.OrganizationId != authority.Ownership.OrganizationId
                || ownership.ActivityId != authority.Ownership.ActivityId
                || ownership.ParticipantId != authority.Ownership.ParticipantId
                || ownership.AttemptId != authority.Ownership.AttemptId
                || ownership.SessionId != authority.Ownership.SessionId)
            {
                return Task.FromResult<AdmittedEvaluationRequestAuthority?>(null);
            }

            return Task.FromResult<AdmittedEvaluationRequestAuthority?>(authority);
        }
    }
}
