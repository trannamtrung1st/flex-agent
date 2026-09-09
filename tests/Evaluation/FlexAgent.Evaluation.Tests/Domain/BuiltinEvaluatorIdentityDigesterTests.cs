using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class BuiltinEvaluatorIdentityDigesterTests
{
    [Fact]
    public void Builtin_registry_entries_use_truthful_computed_digests()
    {
        var registry = new BuiltinEvaluatorRegistry().TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;

        foreach (var entry in registry.Entries.Values)
        {
            var expected = BuiltinEvaluatorIdentityDigester.ComputeDigests(entry with
            {
                EvaluatorDigest = string.Empty,
                ConfigurationDigest = string.Empty,
                DependencyDigest = string.Empty,
            });

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
        Assert.NotEqual(BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest, BuiltinEvaluatorRegistry.BoundedCalcConfigurationDigest);
    }
}
