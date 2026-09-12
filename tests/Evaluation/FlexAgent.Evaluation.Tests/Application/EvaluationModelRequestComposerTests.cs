using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationModelRequestComposerTests
{
    private static readonly EvaluationProcedureV1 Procedure = EvaluationFixtures.LoadSyntheticProcedure();
    private static readonly Guid DeterministicAttemptId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    [Fact]
    public void Compose_builds_deterministic_facts_from_store_backed_authority_only()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var storeBacked = StoreBacked(verified);

        var context = CreateAssistedContext();
        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            storeBacked);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Value!.DeterministicFacts);
        Assert.Single(result.Value.DeterministicFacts!);
        Assert.Equal(verified.Keys.Single(), result.Value.DeterministicFacts![0].SourceId);
        Assert.Equal("prot.eval.fact.0002", result.Value.DeterministicFacts![0].ProtectedRef.ProtectedRef);
    }

    [Fact]
    public void Extra_store_backed_fact_without_verified_entry_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var storeBacked = StoreBacked(verified);
        storeBacked[EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(Guid.CreateVersion7())] =
            new VerifiedDeterministicOutputMaterial(
                verified.Values.Single(),
                new ProtectedPayloadRefV1("prot.eval.fact.forged", new string('f', 64)));

        var context = CreateAssistedContext();
        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            storeBacked);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public void Missing_store_backed_fact_for_verified_entry_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");

        var context = CreateAssistedContext();
        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            storeBackedDeterministicFacts: new Dictionary<string, VerifiedDeterministicOutputMaterial>(StringComparer.Ordinal));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public void Forged_store_backed_protected_ref_digest_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var sourceId = verified.Keys.Single();
        var storeBacked = new Dictionary<string, VerifiedDeterministicOutputMaterial>(StringComparer.Ordinal)
        {
            [sourceId] = new VerifiedDeterministicOutputMaterial(
                verified[sourceId],
                new ProtectedPayloadRefV1("prot.eval.fact.0002", new string('f', 64))),
        };

        var context = CreateAssistedContext();
        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            storeBacked);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public void Duplicate_permitted_evidence_entry_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var context = CreateAssistedContext() with
        {
            PermittedEvidence =
            [
                new EvaluationModelPermittedEvidenceV1(evidenceStableId, "submission.direct_text", new string('b', 64)),
                new EvaluationModelPermittedEvidenceV1(evidenceStableId, "submission.direct_text", new string('b', 64)),
            ],
        };

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            StoreBacked(verified));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("permitted_evidence", result.Field);
    }

    [Fact]
    public void Evidence_binding_superset_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var context = CreateAssistedContext() with
        {
            PermittedEvidenceIdBindings = new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [evidenceStableId] = Guid.Parse("22222222-2222-4222-8222-222222222222"),
                ["evidence.forged.extra"] = Guid.Parse("44444444-4444-4444-8444-444444444444"),
            },
        };

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            StoreBacked(verified));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("permitted_evidence", result.Field);
    }

    [Fact]
    public void Evidence_binding_subset_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        var context = CreateAssistedContext() with
        {
            PermittedEvidenceIdBindings = new Dictionary<string, Guid>(StringComparer.Ordinal),
            PermittedEvidence =
            [
                new EvaluationModelPermittedEvidenceV1(evidenceStableId, "submission.direct_text", new string('b', 64)),
            ],
        };

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            context.PermittedEvidence,
            verified,
            StoreBacked(verified));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("permitted_evidence", result.Field);
    }

    private static EvaluationProcedureCriterionV1 AssistedCriterion() =>
        Procedure.Criteria.Single(item =>
            string.Equals(item.CriterionId, "crit.assisted.structure", StringComparison.Ordinal));

    private static Dictionary<string, EvaluationSafeFactProjection> VerifiedFacts(string json)
    {
        var digest = new string('c', 64);
        var sourceId = EvaluationEvidenceSourceIdentity.DeterministicFactSourceId(DeterministicAttemptId);
        return new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            [sourceId] = new(
                sourceId,
                EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(digest),
                digest,
                Encoding.UTF8.GetBytes(json)),
        };
    }

    private static Dictionary<string, VerifiedDeterministicOutputMaterial> StoreBacked(
        IReadOnlyDictionary<string, EvaluationSafeFactProjection> verifiedFacts)
    {
        var storeBacked = new Dictionary<string, VerifiedDeterministicOutputMaterial>(StringComparer.Ordinal);
        foreach (var (sourceId, fact) in verifiedFacts)
        {
            storeBacked[sourceId] = new VerifiedDeterministicOutputMaterial(
                fact,
                new ProtectedPayloadRefV1("prot.eval.fact.0002", fact.ContentDigest));
        }

        return storeBacked;
    }

    private static EvaluationModelExecutionContext CreateAssistedContext()
    {
        var ownership = EvaluationFixtures.Ownership();
        var requestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
        var invocationAttemptId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc");
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        return new EvaluationModelExecutionContext(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(ownership),
            ownership,
            requestId,
            EvaluationStableOwnershipReferenceFactory.StableRequestId(requestId),
            invocationAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(invocationAttemptId),
            Guid.Parse("11111111-1111-4111-8111-111111111115"),
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
            DeterministicAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableDeterministicInvocationId(DeterministicAttemptId));
    }

    private static SessionOwnershipRefV1 OwnershipRef() =>
        EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(EvaluationFixtures.Ownership());
}
