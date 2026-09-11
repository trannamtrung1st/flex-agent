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

    [Fact]
    public void Compose_builds_deterministic_facts_from_verified_authority_only()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var context = CreateAssistedContext(ProtectedRefs(verified));

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            context,
            verified);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Value!.DeterministicFacts);
        Assert.Single(result.Value.DeterministicFacts!);
        Assert.Equal("detfact.synthetic.schema", result.Value.DeterministicFacts![0].SourceId);
        Assert.Equal(new string('c', 64), result.Value.DeterministicFacts![0].ContentDigest);
        Assert.Equal("prot.eval.fact.0002", result.Value.DeterministicFacts![0].ProtectedRef.ProtectedRef);
    }

    [Fact]
    public void Extra_unverified_deterministic_fact_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var protectedRefs = ProtectedRefs(verified);
        protectedRefs["detfact.synthetic.forged"] = new ProtectedPayloadRefV1(
            "prot.eval.fact.forged",
            new string('f', 64));

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            CreateAssistedContext(protectedRefs),
            verified);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public void Missing_verified_deterministic_fact_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var protectedRefs = new Dictionary<string, ProtectedPayloadRefV1>(StringComparer.Ordinal);

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            CreateAssistedContext(protectedRefs),
            verified);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("deterministic_facts", result.Field);
    }

    [Fact]
    public void Forged_protected_ref_digest_fails_before_compose()
    {
        var verified = VerifiedFacts("""{"valid":true}""");
        var protectedRefs = new Dictionary<string, ProtectedPayloadRefV1>(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new ProtectedPayloadRefV1(
                "prot.eval.fact.0002",
                new string('f', 64)),
        };

        var result = EvaluationModelRequestComposer.TryCompose(
            AssistedCriterion(),
            CreateAssistedContext(protectedRefs),
            verified);

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
        var context = CreateAssistedContext(ProtectedRefs(verified)) with
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
            verified);

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
        var context = CreateAssistedContext(ProtectedRefs(verified)) with
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
            verified);

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
        var context = CreateAssistedContext(ProtectedRefs(verified)) with
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
            verified);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
        Assert.Equal("permitted_evidence", result.Field);
    }

    private static EvaluationProcedureCriterionV1 AssistedCriterion() =>
        Procedure.Criteria.Single(item =>
            string.Equals(item.CriterionId, "crit.assisted.structure", StringComparison.Ordinal));

    private static Dictionary<string, EvaluationSafeFactProjection> VerifiedFacts(string json) =>
        new(StringComparer.Ordinal)
        {
            ["detfact.synthetic.schema"] = new(
                "detfact.synthetic.schema",
                "detfact.synthetic.schema.v1",
                new string('c', 64),
                Encoding.UTF8.GetBytes(json)),
        };

    private static Dictionary<string, ProtectedPayloadRefV1> ProtectedRefs(
        IReadOnlyDictionary<string, EvaluationSafeFactProjection> verifiedFacts)
    {
        var protectedRefs = new Dictionary<string, ProtectedPayloadRefV1>(StringComparer.Ordinal);
        foreach (var (sourceId, fact) in verifiedFacts)
        {
            protectedRefs[sourceId] = new ProtectedPayloadRefV1("prot.eval.fact.0002", fact.ContentDigest);
        }

        return protectedRefs;
    }

    private static EvaluationModelExecutionContext CreateAssistedContext(
        IReadOnlyDictionary<string, ProtectedPayloadRefV1> protectedRefs)
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        return new EvaluationModelExecutionContext(
            new SessionOwnershipRefV1(
                "org.synthetic.demo",
                "act.synthetic.demo",
                "part.synthetic.demo",
                "att.synthetic.demo",
                "sess.synthetic.demo"),
            "ereq.synthetic.0001",
            "eatt.synthetic.0001",
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
            protectedRefs,
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            "dinv.synthetic.0002");
    }
}
