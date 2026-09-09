using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class BuiltinEvaluatorIdentityDigesterTests
{
    [Fact]
    public void Builtin_registry_entries_use_truthful_computed_digests()
    {
        var registry = new BuiltinEvaluatorRegistry().TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var implementation = BuiltinEvaluatorImplementationBinding.Identity;

        foreach (var entry in registry.Entries.Values)
        {
            var expected = BuiltinEvaluatorIdentityDigester.ComputeDigests(
                entry with
                {
                    EvaluatorDigest = string.Empty,
                    ConfigurationDigest = string.Empty,
                    DependencyDigest = string.Empty,
                },
                implementation);

            Assert.Equal(expected.EvaluatorDigest, entry.EvaluatorDigest);
            Assert.Equal(expected.ConfigurationDigest, entry.ConfigurationDigest);
            Assert.Equal(expected.DependencyDigest, entry.DependencyDigest);
            Assert.DoesNotContain('A', entry.EvaluatorDigest);
            Assert.Equal(64, entry.EvaluatorDigest.Length);
        }
    }

    [Fact]
    public void Bounded_calc_digests_are_stable()
    {
        var first = BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest;
        var second = BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest;

        Assert.Equal(first, second);
        Assert.NotEqual(
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest,
            BuiltinEvaluatorRegistry.BoundedCalcConfigurationDigest);
    }

    [Fact]
    public void Runner_source_artifact_change_changes_evaluator_and_dependency_digests()
    {
        var entry = EmptyDigestEntry();
        var baseline = BuiltinEvaluatorIdentityDigester.ComputeDigests(
            entry,
            BuiltinEvaluatorImplementationBinding.Identity);
        var altered = BuiltinEvaluatorIdentityDigester.ComputeDigests(
            entry,
            AlterSourceArtifact("runner", new string('a', 64)));

        Assert.NotEqual(baseline.EvaluatorDigest, altered.EvaluatorDigest);
        Assert.NotEqual(baseline.DependencyDigest, altered.DependencyDigest);
    }

    [Fact]
    public void Dependency_source_artifact_change_changes_evaluator_and_dependency_digests()
    {
        var entry = EmptyDigestEntry();
        var baseline = BuiltinEvaluatorIdentityDigester.ComputeDigests(
            entry,
            BuiltinEvaluatorImplementationBinding.Identity);
        var altered = BuiltinEvaluatorIdentityDigester.ComputeDigests(
            entry,
            AlterSourceArtifact("evaluation_identity", new string('b', 64)));

        Assert.NotEqual(baseline.EvaluatorDigest, altered.EvaluatorDigest);
        Assert.NotEqual(baseline.ConfigurationDigest, altered.ConfigurationDigest);
        Assert.NotEqual(baseline.DependencyDigest, altered.DependencyDigest);
    }

    private static EvaluatorRegistryEntry EmptyDigestEntry()
    {
        var registry = new BuiltinEvaluatorRegistry().TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        return registry.Entries["eval.builtin.bounded-calc"] with
        {
            EvaluatorDigest = string.Empty,
            ConfigurationDigest = string.Empty,
            DependencyDigest = string.Empty,
        };
    }

    private static BuiltinEvaluatorImplementationIdentity AlterSourceArtifact(
        string artifactKey,
        string artifactDigest)
    {
        var identity = BuiltinEvaluatorImplementationBinding.Identity;
        var sourceArtifacts = new Dictionary<string, string>(identity.SourceArtifactDigests, StringComparer.Ordinal)
        {
            [artifactKey] = artifactDigest,
        };

        return BuiltinEvaluatorImplementationManifest.CreateIdentity(sourceArtifacts);
    }
}
