using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class FrozenModelIdentityTests
{
    [Fact]
    public void Exact_profile_id_version_and_digest_are_required()
    {
        var result = FrozenModelIdentity.TryCreate(
            "mdl.p0.text.synthetic",
            "mdl.p0.text.synthetic.v1",
            new string('a', 64),
            "provider.synthetic",
            "organization_byok",
            "cred.bind.synthetic",
            "cred.bind.synthetic.v1");

        Assert.True(result.Succeeded);
        Assert.Equal("mdl.p0.text.synthetic", result.Value!.ProfileId);
        Assert.Equal(new string('a', 64), result.Value.ProfileDigest);
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("current")]
    [InlineData("mdl.latest.prod")]
    [InlineData("gpt-4o")]
    public void Profile_name_or_mutable_alias_is_rejected(string profileId)
    {
        var result = FrozenModelIdentity.TryCreate(
            profileId,
            "mdl.p0.text.synthetic.v1",
            new string('a', 64),
            "provider.synthetic",
            "organization_byok",
            "cred.bind.synthetic",
            "cred.bind.synthetic.v1");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedModel, result.OutcomeCode);
    }

    [Fact]
    public void Missing_credential_binding_version_is_rejected()
    {
        var result = FrozenModelIdentity.TryCreate(
            "mdl.p0.text.synthetic",
            "mdl.p0.text.synthetic.v1",
            new string('a', 64),
            "provider.synthetic",
            "organization_byok",
            "cred.bind.synthetic",
            "");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedModel, result.OutcomeCode);
    }
}
