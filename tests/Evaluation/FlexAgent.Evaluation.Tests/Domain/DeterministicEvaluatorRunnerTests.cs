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
        Assert.NotNull(result.Value.OutputContentDigest);
        Assert.Equal(
            DeterministicInvocationProvenance.ProtectedOutputRef(result.Value.OutputContentDigest),
            result.Value.ProtectedOutputRef);
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

    [Fact]
    public void Oversized_canonical_input_is_rejected_before_execution()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"] with
        {
            MemoryLimitBytes = 32,
        };
        var input = CreateInput(
            """
            {
              "schema": "eval.builtin.bounded-calc.input.v1",
              "operation": "word_count",
              "text": "this canonical input exceeds the configured memory limit",
              "minimum": 1,
              "maximum": 10
            }
            """);

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding, input),
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(DeterministicInvocationOutcomes.ResourceExhausted, result.Value!.Outcome);
    }

    [Fact]
    public void Excessive_json_nesting_is_rejected()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"];
        var builder = new StringBuilder(
            """
            {
              "schema": "eval.builtin.bounded-calc.input.v1",
              "operation": "word_count",
              "text": "x",
              "minimum": 1,
              "maximum": 2,
              "nested":
            """);
        builder.Append('{', DeterministicExecutionBounds.MaxJsonDepth + 2);
        builder.Append("\"x\":1");
        builder.Append('}', DeterministicExecutionBounds.MaxJsonDepth + 2);
        builder.Append('}');
        var input = CreateInput(builder.ToString());

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding with { }, input),
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal(DeterministicInvocationOutcomes.InvalidOutput, result.Value!.Outcome);
    }

    [Fact]
    public void Expired_wall_clock_deadline_before_execution_returns_timeout()
    {
        var registry = Registry.TryGetRegistry(EvaluatorRegistryVersions.P0).Value!;
        var binding = registry.Entries["eval.builtin.bounded-calc"];
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
        var startedAt = DateTimeOffset.UtcNow.AddSeconds(-11);

        var result = Runner.TryExecute(
            registry,
            CreateRequest(binding with { }, input),
            startedAt);

        Assert.True(result.Succeeded);
        Assert.Equal(DeterministicInvocationOutcomes.Timeout, result.Value!.Outcome);
        Assert.Equal("provider_timeout", result.Value.FailureCategory);
    }

    [Fact]
    public void Word_count_checks_wall_clock_deadline_during_scalar_iteration()
    {
        var deadline = new DeterministicExecutionDeadline(DateTimeOffset.UtcNow.AddSeconds(-1));
        var text = new string('a', InProcessDeterministicExecutionContract.DeadlineCheckIntervalScalars + 1);

        var completed = TryCountWordsForTest(text, deadline, out var count);

        Assert.False(completed);
    }

    private static bool TryCountWordsForTest(
        string text,
        DeterministicExecutionDeadline deadline,
        out int count)
    {
        count = 0;
        var inWord = false;
        var scalarIndex = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            scalarIndex++;
            if (InProcessDeterministicExecutionContract.ShouldAbortScalarLoop(scalarIndex)
                && !deadline.TryCheck(out _))
            {
                return false;
            }

            if (Rune.IsWhiteSpace(rune))
            {
                inWord = false;
                continue;
            }

            if (!inWord)
            {
                count++;
                inWord = true;
            }
        }

        return true;
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
