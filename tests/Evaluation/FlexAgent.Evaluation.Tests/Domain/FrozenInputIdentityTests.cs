using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class FrozenInputIdentityTests
{
    [Fact]
    public void Frozen_input_requires_exact_rubric_and_model_identity()
    {
        var result = EvaluationFixtures.CreateFrozenInput();

        Assert.True(result.Succeeded);
        Assert.Equal("handoff.synthetic.0001", result.Value!.HandoffId);
        Assert.Equal(EvaluationFixtures.RubricDigest, result.Value.Rubric.ContentDigest);
        Assert.Equal("mdl.p0.text.synthetic", result.Value.Model.ProfileId);
        Assert.Equal("manifest-jcs-sha256-v2", result.Value.ManifestSealProcedureId);
    }

    [Fact]
    public void Mutable_handoff_alias_is_rejected()
    {
        var result = FrozenInputIdentity.TryCreate(
            "handoff.latest",
            EvaluationFixtures.Ownership(),
            Guid.NewGuid(),
            "completed",
            42,
            "manifest-jcs-sha256-v2",
            EvaluationFixtures.TerminalSealDigest,
            Guid.NewGuid(),
            EvaluationFixtures.ConfigurationDigest,
            Guid.NewGuid(),
            EvaluationFixtures.ManifestDigest,
            EvaluationFixtures.Rubric(),
            EvaluationFixtures.Submission(),
            "evalreg.p0.v1",
            EvaluationFixtures.Model(),
            "lifecycle.activity-closure-365d.v1");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.MutableAlias, result.OutcomeCode);
    }

    [Fact]
    public void Mutable_rubric_alias_is_rejected()
    {
        var result = ExactSourceIdentity.TryCreate(
            "latest_rubric",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('b', 64));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.MutableAlias, result.OutcomeCode);
    }

    [Fact]
    public void Rubric_source_key_must_be_rubric_evaluation()
    {
        var ownership = EvaluationFixtures.Ownership();
        var model = EvaluationFixtures.Model();
        var agent = ExactSourceIdentity.TryCreate(
            "agent_profile",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('b', 64)).Value!;
        var result = FrozenInputIdentity.TryCreate(
            "handoff.synthetic.invalid-rubric",
            ownership,
            Guid.NewGuid(),
            "completed",
            42,
            "manifest-jcs-sha256-v2",
            EvaluationFixtures.TerminalSealDigest,
            Guid.NewGuid(),
            EvaluationFixtures.ConfigurationDigest,
            Guid.NewGuid(),
            EvaluationFixtures.ManifestDigest,
            agent,
            EvaluationFixtures.Submission(),
            "evalreg.p0.v1",
            model,
            "lifecycle.activity-closure-365d.v1");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }
}
