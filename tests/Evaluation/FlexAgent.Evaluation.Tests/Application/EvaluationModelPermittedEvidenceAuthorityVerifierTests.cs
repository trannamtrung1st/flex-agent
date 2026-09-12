using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelPermittedEvidenceAuthorityVerifierTests
{
    private static readonly Guid EvidenceId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    [Fact]
    public void Forged_execution_context_source_type_is_rejected()
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        var context = CreateContext(
            [
                new EvaluationModelPermittedEvidenceV1(
                    evidenceStableId,
                    "session.transcript_item",
                    new string('b', 64)),
            ],
            EvaluationFixtures.VerifiedPermittedEvidence(
                evidenceStableId,
                EvidenceId,
                "submission.direct_text",
                new string('b', 64)));

        var result = EvaluationModelPermittedEvidenceAuthorityVerifier.TryResolve(context);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("permitted_evidence", result.Field);
    }

    [Fact]
    public void Authoritative_permitted_evidence_is_returned_when_claims_match_verified_material()
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(EvidenceId);
        var context = CreateContext(
            [
                new EvaluationModelPermittedEvidenceV1(
                    evidenceStableId,
                    "submission.direct_text",
                    new string('b', 64)),
            ],
            EvaluationFixtures.VerifiedPermittedEvidence(
                evidenceStableId,
                EvidenceId,
                "submission.direct_text",
                new string('b', 64)));

        var result = EvaluationModelPermittedEvidenceAuthorityVerifier.TryResolve(context);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Single(result.Value!);
        Assert.Equal("submission.direct_text", result.Value![0].SourceType);
    }

    private static EvaluationModelExecutionContext CreateContext(
        IReadOnlyList<EvaluationModelPermittedEvidenceV1> permittedEvidence,
        IReadOnlyList<VerifiedPermittedEvidenceMaterial> verifiedPermittedEvidence)
    {
        var ownership = EvaluationFixtures.Ownership();
        var evidenceStableId = permittedEvidence[0].EvidenceId;
        return new EvaluationModelExecutionContext(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(ownership),
            ownership,
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab"),
            EvaluationStableOwnershipReferenceFactory.StableRequestId(
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab")),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc"),
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc")),
            Guid.Parse("11111111-1111-4111-8111-111111111115"),
            EvaluationFixtures.Model(),
            "eval.instructions.p0.v1",
            permittedEvidence,
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [evidenceStableId] = EvidenceId,
            },
            verifiedPermittedEvidence,
            null,
            null);
    }
}
