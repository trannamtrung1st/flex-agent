using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluatorRegistryTests
{
    private static readonly IEvaluatorRegistry Registry = new BuiltinEvaluatorRegistry();

    [Theory]
    [InlineData(EvaluatorRegistryVersions.P0)]
    [InlineData(EvaluatorRegistryVersions.BuiltinAlias)]
    public void Known_registry_versions_return_five_qualified_builtin_evaluators(string registryVersion)
    {
        var result = Registry.TryGetRegistry(registryVersion);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(registryVersion, result.Value!.RegistryVersion);
        Assert.Equal(5, result.Value.Entries.Count);
        Assert.All(
            result.Value.Entries.Values,
            entry => Assert.Equal(EvaluatorQualificationStates.Qualified, entry.QualificationState));
    }

    [Fact]
    public void Unknown_registry_version_is_rejected()
    {
        var result = Registry.TryGetRegistry("evalreg.unknown.v1");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("evaluator_registry_version", result.Field);
    }

    [Fact]
    public void Mutable_registry_version_alias_is_rejected()
    {
        var result = Registry.TryGetRegistry("evalreg.p0.latest");

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.MutableAlias, result.OutcomeCode);
        Assert.Equal("evaluator_registry_version", result.Field);
    }

    [Fact]
    public void Synthetic_procedure_deterministic_bindings_validate_against_registry()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();

        foreach (var criterion in procedure.Criteria)
        {
            if (criterion.DeterministicEvaluator is null)
            {
                continue;
            }

            var validated = EvaluatorBindingValidator.TryValidateBinding(
                registry,
                criterion.DeterministicEvaluator);

            Assert.True(validated.Succeeded, validated.OutcomeCode);
        }
    }

    [Fact]
    public void Jcs_canonical_synthetic_procedure_deterministic_bindings_validate_against_registry()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var procedure = EvaluationProcedureTestFixtures.LoadP0TextSynthetic();

        foreach (var criterion in procedure.Criteria)
        {
            if (criterion.DeterministicEvaluator is null)
            {
                continue;
            }

            var validated = EvaluatorBindingValidator.TryValidateBinding(
                registry,
                criterion.DeterministicEvaluator);

            Assert.True(validated.Succeeded, validated.OutcomeCode);
        }
    }

    [Fact]
    public void Parser_allowlisted_evaluator_ids_are_registered_in_p0_registry()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;

        Assert.Equal(
            EvaluationProcedureDocumentParser.AllowlistedEvaluatorIds.Count,
            registry.Entries.Count);

        foreach (var evaluatorId in EvaluationProcedureDocumentParser.AllowlistedEvaluatorIds)
        {
            Assert.True(registry.Entries.ContainsKey(evaluatorId));
        }
    }

    [Fact]
    public void P0_and_builtin_alias_registries_expose_identical_evaluator_entries()
    {
        var p0 = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var alias = Registry.TryGetRegistry(EvaluatorRegistryVersions.BuiltinAlias).Value!;

        Assert.Equal(p0.Entries.Keys, alias.Entries.Keys);

        foreach (var evaluatorId in p0.Entries.Keys)
        {
            var p0Entry = p0.Entries[evaluatorId];
            var aliasEntry = alias.Entries[evaluatorId];

            Assert.Equal(p0Entry with { }, aliasEntry with { });
        }
    }

    [Fact]
    public void Tighter_duration_within_registry_limit_is_accepted()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            CpuTimeLimit = "PT5S",
            ElapsedTimeLimit = "PT5S",
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    [Fact]
    public void Mutable_evaluator_id_alias_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc.latest",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest);

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.MutableAlias, result.OutcomeCode);
        Assert.Equal("evaluator_id", result.Field);
    }

    [Fact]
    public void Unknown_evaluator_id_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.external.unknown",
            "eval.external.unknown.v1",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("evaluator_id", result.Field);
    }

    [Fact]
    public void Mutable_evaluator_version_alias_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.latest",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest);

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.MutableAlias, result.OutcomeCode);
        Assert.Equal("evaluator_version", result.Field);
    }

    [Fact]
    public void Changed_evaluator_digest_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            new string('9', 64));

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("evaluator_digest", result.Field);
    }

    [Fact]
    public void Changed_configuration_digest_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            ConfigurationDigest = new string('9', 64),
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("configuration_digest", result.Field);
    }

    [Fact]
    public void Changed_dependency_digest_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            DependencyDigest = new string('9', 64),
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("dependency_digest", result.Field);
    }

    [Fact]
    public void Operation_mismatch_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            Operation = "schema_validate",
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("operation", result.Field);
    }

    [Fact]
    public void Network_egress_not_prohibited_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            NetworkEgress = "permitted",
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("network_egress", result.Field);
    }

    [Fact]
    public void Binding_exceeding_registry_memory_limit_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            MemoryLimitBytes = 268435456,
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("memory_limit_bytes", result.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_memory_limit_is_rejected(int memoryLimitBytes)
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            MemoryLimitBytes = memoryLimitBytes,
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("memory_limit_bytes", result.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_output_limit_is_rejected(int outputLimitBytes)
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            OutputLimitBytes = outputLimitBytes,
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("output_limit_bytes", result.Field);
    }

    [Theory]
    [InlineData("PT")]
    [InlineData("PT0S")]
    [InlineData("PTgarbage")]
    [InlineData("PT5Sextra")]
    public void Malformed_cpu_duration_is_rejected(string cpuTimeLimit)
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            CpuTimeLimit = cpuTimeLimit,
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("cpu_time_limit", result.Field);
    }

    [Theory]
    [InlineData("PT")]
    [InlineData("PT0S")]
    [InlineData("PTgarbage")]
    public void Malformed_elapsed_duration_is_rejected(string elapsedTimeLimit)
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = CreateBinding(
            "eval.builtin.bounded-calc",
            "eval.builtin.bounded-calc.v1",
            BuiltinEvaluatorRegistry.BoundedCalcEvaluatorDigest) with
        {
            ElapsedTimeLimit = elapsedTimeLimit,
        };

        var result = EvaluatorBindingValidator.TryValidateBinding(registry, binding);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
        Assert.Equal("elapsed_time_limit", result.Field);
    }

    private static DeterministicEvaluatorBindingV1 CreateBinding(
        string evaluatorId,
        string evaluatorVersion,
        string evaluatorDigest) =>
        new(
            evaluatorId,
            evaluatorVersion,
            evaluatorDigest,
            "bounded_calculation",
            "eval.builtin.bounded-calc.input.v1",
            "eval.builtin.bounded-calc.output.v1",
            "jcs-sha256-v1",
            BuiltinEvaluatorRegistry.BoundedCalcConfigurationDigest,
            BuiltinEvaluatorRegistry.BoundedCalcDependencyDigest,
            "PT5S",
            "PT10S",
            16_777_216,
            4096,
            "prohibited",
            "prohibited");
}
