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
    public void Implementation_manifest_changes_evaluator_digest()
    {
        var registry = new BuiltinEvaluatorRegistry().TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var entry = registry.Entries["eval.builtin.bounded-calc"] with
        {
            EvaluatorDigest = string.Empty,
            ConfigurationDigest = string.Empty,
            DependencyDigest = string.Empty,
        };

        var baseline = BuiltinEvaluatorIdentityDigester.ComputeDigests(
            entry,
            BuiltinEvaluatorImplementationBinding.Identity);
        var altered = BuiltinEvaluatorIdentityDigester.ComputeDigests(
            entry,
            BuiltinEvaluatorImplementationBinding.Identity with
            {
                RunnerSourceArtifactDigest = new string('a', 64),
            });

        Assert.NotEqual(baseline.EvaluatorDigest, altered.EvaluatorDigest);
        Assert.NotEqual(baseline.DependencyDigest, altered.DependencyDigest);
    }
}
