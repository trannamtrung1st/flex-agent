using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicEvaluatorRunnerTests
{
    private static readonly RestrictedBuiltinDeterministicEvaluatorRunner Runner = new();
    private static readonly BuiltinEvaluatorRegistry Registry = new();

    [Fact]
    public void Bounded_calculation_succeeds_for_valid_input()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"];
        var inputJson = JsonSerializer.Serialize(new
        {
            schema = "eval.builtin.bounded-calc.input.v1",
            operation = "word_count",
            text = "one two three",
            minimum = 1,
            maximum = 10,
        });
        var input = CreateInput(inputJson);
        var request = CreateRequest(binding with { }, input);

        var result = Runner.TryExecute(registry, request, DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DeterministicInvocationOutcomes.Succeeded, result.Value!.Outcome);
        Assert.NotNull(result.Value.OutputUtf8);
        Assert.NotNull(result.Value.ProtectedOutputRef);
    }

    [Fact]
    public void Canonical_input_digest_mismatch_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"];
        var input = CreateInput("""{"schema":"eval.builtin.bounded-calc.input.v1","operation":"word_count","text":"x","minimum":1,"maximum":2}""") with
        {
            CanonicalInputDigest = new string('9', 64),
        };

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding with { }, input),
            DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("canonical_input_digest", result.Field);
    }

    [Fact]
    public void Forbidden_shell_field_is_rejected_with_invalid_output()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"];
        var input = CreateInput(
            """
            {
              "schema": "eval.builtin.bounded-calc.input.v1",
              "operation": "word_count",
              "text": "safe",
              "minimum": 1,
              "maximum": 2,
              "shell": "/bin/sh -c echo"
            }
            """);

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding with { }, input),
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(DeterministicInvocationOutcomes.InvalidOutput, result.Value!.Outcome);
        Assert.Equal("integrity_failure", result.Value.FailureCategory);
    }

    [Fact]
    public void Path_traversal_in_input_text_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"];
        var input = CreateInput(
            """
            {
              "schema": "eval.builtin.bounded-calc.input.v1",
              "operation": "word_count",
              "text": "../../etc/passwd",
              "minimum": 1,
              "maximum": 10
            }
            """);

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding with { }, input),
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(DeterministicInvocationOutcomes.InvalidOutput, result.Value!.Outcome);
    }

    [Fact]
    public void Output_exceeding_binding_limit_returns_resource_exhausted()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"] with
        {
            OutputLimitBytes = 16,
        };
        var input = CreateInput(
            """
            {
              "schema": "eval.builtin.bounded-calc.input.v1",
              "operation": "word_count",
              "text": "this produces a long json output",
              "minimum": 1,
              "maximum": 100
            }
            """);

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding, input),
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(DeterministicInvocationOutcomes.ResourceExhausted, result.Value!.Outcome);
        Assert.Equal("resource_exhausted", result.Value.FailureCategory);
    }

    [Fact]
    public void Unqualified_binding_is_rejected_before_execution()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"] with
        {
            EvaluatorDigest = new string('9', 64),
        };
        var input = CreateInput(
            """
            {
              "schema": "eval.builtin.bounded-calc.input.v1",
              "operation": "word_count",
              "text": "ok",
              "minimum": 1,
              "maximum": 2
            }
            """);

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding, input),
            DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.UnqualifiedEvaluator, result.OutcomeCode);
    }

    private static DeterministicEvaluatorExecutionRequest CreateRequest(
        EvaluatorRegistryEntry entry,
        DeterministicEvaluatorCanonicalInput input)
    {
        var binding = new DeterministicEvaluatorBindingV1(
            entry.EvaluatorId,
            entry.EvaluatorVersion,
            entry.EvaluatorDigest,
            entry.Operation,
            entry.InputSchemaId,
            entry.OutputSchemaId,
            entry.CanonicalizationProcedure,
            entry.ConfigurationDigest,
            entry.DependencyDigest,
            entry.CpuTimeLimit,
            entry.ElapsedTimeLimit,
            entry.MemoryLimitBytes,
            entry.OutputLimitBytes,
            entry.NetworkEgress,
            entry.ExecutableSelection);
        var ownership = EvaluationFixtures.Ownership();
        return new DeterministicEvaluatorExecutionRequest(
            ownership,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            binding,
            input);
    }

    private static DeterministicEvaluatorCanonicalInput CreateInput(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new DeterministicEvaluatorCanonicalInput(bytes, digest);
    }
}
