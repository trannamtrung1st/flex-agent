using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class P0ResolvedSessionConfigurationResolverTests
{
    [Fact]
    public void Resolve_is_deterministic_for_the_same_immutable_inputs()
    {
        var request = Request();
        var first = P0ResolvedSessionConfigurationResolver.Resolve(request);
        var second = P0ResolvedSessionConfigurationResolver.Resolve(request);
        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(first.Value!.ConfigurationDigest, second.Value!.ConfigurationDigest);
        Assert.Equal(first.Value.ManifestDigest, second.Value.ManifestDigest);
        Assert.Contains("\"procedure_id\":\"resolved-session-configuration-jcs-sha256-v1\"", first.Value.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains("\"manifest_id\"", first.Value.InitialManifestJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_rejects_mutable_aliases_digest_drift_and_disabled_capabilities()
    {
        var alias = Request() with
        {
            BaselineSources = [new ResolvedSourceReference("current_harness", Guid.CreateVersion7(), Guid.CreateVersion7(), new string('a', 64))],
        };
        Assert.Equal(
            ResolvedConfigurationOutcomeCodes.MutableAlias,
            P0ResolvedSessionConfigurationResolver.Resolve(alias).OutcomeCode);

        var request = Request();
        var drifted = request with
        {
            RevalidatedSources = request.RevalidatedSources
                .Select(source => source with { ContentDigest = new string('f', 64) })
                .ToArray(),
        };
        Assert.Equal(
            ResolvedConfigurationOutcomeCodes.DigestDrift,
            P0ResolvedSessionConfigurationResolver.Resolve(drifted).OutcomeCode);

        var voice = Request() with { VoiceEnabled = true };
        Assert.Equal(
            ResolvedConfigurationOutcomeCodes.DisabledCapability,
            P0ResolvedSessionConfigurationResolver.Resolve(voice).OutcomeCode);
    }

    [Fact]
    public void Resolve_rejects_unqualified_model_identity()
    {
        var request = Request() with
        {
            ModelDeployment = SessionRuntimeTestFixtures.CreateFrozenDeployment() with
            {
                ProfileVersion = "",
            },
        };
        Assert.Equal(
            ResolvedConfigurationOutcomeCodes.UnqualifiedModel,
            P0ResolvedSessionConfigurationResolver.Resolve(request).OutcomeCode);
    }

    [Fact]
    public void Resolve_requires_rubric_evaluation_source()
    {
        var digest = new string('c', 64);
        var withoutRubric = new[]
        {
            Source("organization_policy", digest),
            Source("agent", digest),
            Source("harness", digest),
            Source("workflow", digest),
            Source("model_deployment", digest),
            Source("task_submission", digest),
            Source("capability", digest),
        };
        var request = Request() with
        {
            BaselineSources = withoutRubric,
            RevalidatedSources = withoutRubric,
        };
        Assert.Equal(
            ResolvedConfigurationOutcomeCodes.MissingSource,
            P0ResolvedSessionConfigurationResolver.Resolve(request).OutcomeCode);
    }

    [Fact]
    public void Resolve_freezes_exact_rubric_and_model_binding_not_profile_name_alone()
    {
        var result = P0ResolvedSessionConfigurationResolver.Resolve(Request());
        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Contains("\"source_key\":\"rubric_evaluation\"", result.Value!.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains($"\"model_profile_id\":\"{result.Value.ModelDeployment.ProfileId}\"", result.Value.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains($"\"model_profile_digest\":\"{result.Value.ModelDeployment.ProfileDigest}\"", result.Value.CanonicalJson, StringComparison.Ordinal);
        Assert.Contains($"\"credential_binding_reference\":\"{result.Value.ModelDeployment.CredentialBindingReference}\"", result.Value.CanonicalJson, StringComparison.Ordinal);
        Assert.DoesNotContain("latest", result.Value.CanonicalJson, StringComparison.OrdinalIgnoreCase);
    }

    private static P0ResolvedConfigurationRequest Request()
    {
        var policy = RuntimePolicyTestFixtures.ResolveEnabledTimerPolicy();
        var sources = RequiredSources();
        return new P0ResolvedConfigurationRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            SessionRuntimeTestFixtures.CreateOwnership(),
            sources,
            sources,
            policy,
            SessionRuntimeTestFixtures.CreateFrozenDeployment(),
            [new ProtectedContentRef("sub:bound-v1", new string('a', 64))],
            false,
            false,
            false,
            false,
            false);
    }

    private static IReadOnlyList<ResolvedSourceReference> RequiredSources()
    {
        var digest = new string('c', 64);
        return
        [
            Source("organization_policy", digest),
            Source("agent", digest),
            Source("harness", digest),
            Source("workflow", digest),
            Source("model_deployment", digest),
            Source("task_submission", digest),
            Source("capability", digest),
            Source("rubric_evaluation", digest),
        ];
    }

    private static ResolvedSourceReference Source(string key, string digest) =>
        new(key, Guid.CreateVersion7(), Guid.CreateVersion7(), digest);
}
