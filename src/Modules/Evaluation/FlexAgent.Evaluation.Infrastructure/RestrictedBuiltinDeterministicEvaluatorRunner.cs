using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class RestrictedBuiltinDeterministicEvaluatorRunner : IDeterministicEvaluatorRunner
{
    private static readonly HashSet<string> ForbiddenPropertyNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "shell",
            "script",
            "executable",
            "env",
            "environment",
            "secret",
            "code",
            "command",
            "process",
            "file_path",
            "path",
        };

    public EvaluationDecision<DeterministicEvaluatorExecutionResult> TryExecute(
        EvaluatorRegistrySnapshot registry,
        DeterministicEvaluatorExecutionRequest request,
        DateTimeOffset startedAt)
    {
        var bindingValidation = EvaluatorBindingValidator.TryValidateBinding(registry, request.Binding);
        if (!bindingValidation.Succeeded || bindingValidation.Value is null)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                bindingValidation.OutcomeCode,
                bindingValidation.Field);
        }

        if (!TryValidateCanonicalInputDigest(request.Input, out var inputFailureField))
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Fail(
                EvaluationFailureCodes.CitationIntegrity,
                inputFailureField);
        }

        var attemptId = DeterministicInvocationIdentity.ComputeAttemptId(
            request.RequestId,
            request.InvocationAttemptId,
            request.CriterionId,
            request.CriterionVersion,
            request.Binding,
            request.Input.CanonicalInputDigest);
        var inputRef = DeterministicInvocationProvenance.ProtectedInputRef(request.Input.CanonicalInputDigest);

        if (!DeterministicExecutionBounds.TryValidateCanonicalInputSize(
                request.Input.CanonicalUtf8,
                request.Binding.MemoryLimitBytes,
                out var inputSizeField))
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                FailedResult(
                    attemptId,
                    inputRef,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    DeterministicInvocationOutcomes.ResourceExhausted,
                    "resource_exhausted",
                    inputSizeField));
        }

        if (!DeterministicExecutionBounds.TryParseBoundedJson(
                request.Input.CanonicalUtf8,
                out var document,
                out var jsonField)
            || document is null)
        {
            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                FailedResult(
                    attemptId,
                    inputRef,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    DeterministicInvocationOutcomes.InvalidOutput,
                    "schema_invalid",
                    jsonField));
        }

        using (document)
        {
            if (!TryScanInputForForbiddenContent(document.RootElement, out var forbiddenField))
            {
                return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                    FailedResult(
                        attemptId,
                        inputRef,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        DeterministicInvocationOutcomes.InvalidOutput,
                        "integrity_failure",
                        forbiddenField));
            }

            if (!DeterministicExecutionBounds.TryValidateElapsedLimit(
                    startedAt,
                    request.Binding.ElapsedTimeLimit,
                    out var elapsedField))
            {
                return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                    FailedResult(
                        attemptId,
                        inputRef,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        DeterministicInvocationOutcomes.Timeout,
                        "provider_timeout",
                        elapsedField));
            }

            var execution = ExecuteBuiltin(request.Binding, document.RootElement);
            if (execution.OutputUtf8 is { } output
                && output.Length > request.Binding.OutputLimitBytes)
            {
                return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                    FailedResult(
                        attemptId,
                        inputRef,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        DeterministicInvocationOutcomes.ResourceExhausted,
                        "resource_exhausted",
                        "output_limit_bytes"));
            }

            if (!DeterministicExecutionBounds.TryValidateElapsedLimit(
                    startedAt,
                    request.Binding.CpuTimeLimit,
                    out var cpuField))
            {
                return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                    FailedResult(
                        attemptId,
                        inputRef,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        DeterministicInvocationOutcomes.Timeout,
                        "provider_timeout",
                        cpuField));
            }

            var outputRef = DeterministicInvocationProvenance.ProtectedOutputRef(execution.OutputContentDigest);
            var finishedAt = DateTimeOffset.UtcNow;

            return EvaluationDecision<DeterministicEvaluatorExecutionResult>.Ok(
                new DeterministicEvaluatorExecutionResult(
                    attemptId,
                    execution.Outcome,
                    execution.FailureCategory,
                    execution.OutputUtf8,
                    execution.OutputContentDigest,
                    inputRef,
                    outputRef,
                    startedAt,
                    finishedAt));
        }
    }

    private static BuiltinExecutionAttempt ExecuteBuiltin(
        DeterministicEvaluatorBindingV1 binding,
        JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
        {
            return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
        }

        return binding.Operation switch
        {
            EvaluatorOperations.BoundedCalculation => ExecuteBoundedCalculation(binding, input),
            EvaluatorOperations.ExactCompare => ExecuteExactCompare(input),
            EvaluatorOperations.SchemaValidate => ExecuteSchemaValidate(input),
            EvaluatorOperations.CitationValidate => ExecuteCitationValidate(input),
            EvaluatorOperations.RubricAggregate => ExecuteRubricAggregate(input),
            _ => FailedAttempt(DeterministicInvocationOutcomes.Failed, "integrity_failure"),
        };
    }

    private static BuiltinExecutionAttempt ExecuteBoundedCalculation(
        DeterministicEvaluatorBindingV1 binding,
        JsonElement input)
    {
        if (!input.TryGetProperty("schema", out var schema)
            || schema.GetString() != binding.InputSchemaId
            || !input.TryGetProperty("operation", out var operation)
            || operation.GetString() != "word_count"
            || !input.TryGetProperty("text", out var textElement)
            || textElement.ValueKind != JsonValueKind.String
            || !input.TryGetProperty("minimum", out var minimumElement)
            || minimumElement.TryGetInt32(out var minimum) is false
            || !input.TryGetProperty("maximum", out var maximumElement)
            || maximumElement.TryGetInt32(out var maximum) is false
            || minimum > maximum)
        {
            return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
        }

        var text = textElement.GetString() ?? string.Empty;
        var wordCount = CountWords(text);
        var withinRange = wordCount >= minimum && wordCount <= maximum;
        var outputJson = JsonSerializer.Serialize(new
        {
            schema = binding.OutputSchemaId,
            value = wordCount,
            within_range = withinRange,
        });
        return SuccessAttempt(outputJson);
    }

    private static BuiltinExecutionAttempt ExecuteExactCompare(JsonElement input)
    {
        if (!input.TryGetProperty("left_digest", out var left)
            || !input.TryGetProperty("right_digest", out var right)
            || left.ValueKind != JsonValueKind.String
            || right.ValueKind != JsonValueKind.String)
        {
            return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
        }

        var matches = string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal);
        return SuccessAttempt(JsonSerializer.Serialize(new { matches }));
    }

    private static BuiltinExecutionAttempt ExecuteSchemaValidate(JsonElement input)
    {
        if (!input.TryGetProperty("schema", out var schema)
            || schema.ValueKind != JsonValueKind.String
            || !input.TryGetProperty("payload", out var payload)
            || payload.ValueKind != JsonValueKind.Object)
        {
            return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
        }

        return SuccessAttempt(JsonSerializer.Serialize(new { valid = true, schema = schema.GetString() }));
    }

    private static BuiltinExecutionAttempt ExecuteCitationValidate(JsonElement input)
    {
        if (!input.TryGetProperty("citations", out var citations)
            || citations.ValueKind != JsonValueKind.Array)
        {
            return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
        }

        foreach (var citation in citations.EnumerateArray())
        {
            if (citation.ValueKind != JsonValueKind.String
                || !EvaluationIdentity.IsStableId(citation.GetString()))
            {
                return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
            }
        }

        return SuccessAttempt(JsonSerializer.Serialize(new { valid = true, count = citations.GetArrayLength() }));
    }

    private static BuiltinExecutionAttempt ExecuteRubricAggregate(JsonElement input)
    {
        if (!input.TryGetProperty("scores", out var scores)
            || scores.ValueKind != JsonValueKind.Array
            || scores.GetArrayLength() is < 1 or > 32)
        {
            return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
        }

        var total = 0;
        foreach (var score in scores.EnumerateArray())
        {
            if (!score.TryGetInt32(out var value) || value < 0 || value > 100)
            {
                return FailedAttempt(DeterministicInvocationOutcomes.InvalidOutput, "schema_invalid");
            }

            total += value;
        }

        return SuccessAttempt(JsonSerializer.Serialize(new { total }));
    }

    private static bool TryValidateCanonicalInputDigest(
        DeterministicEvaluatorCanonicalInput input,
        out string? field)
    {
        field = null;
        if (!EvaluationIdentity.IsSha256Hex(input.CanonicalInputDigest))
        {
            field = "canonical_input_digest";
            return false;
        }

        var computed = Convert.ToHexString(SHA256.HashData(input.CanonicalUtf8.Span)).ToLowerInvariant();
        if (!string.Equals(computed, input.CanonicalInputDigest, StringComparison.Ordinal))
        {
            field = "canonical_input_digest";
            return false;
        }

        return true;
    }

    private static bool TryScanInputForForbiddenContent(JsonElement root, out string field)
    {
        field = "canonical_input";
        return !ContainsForbiddenContent(root, ref field);
    }

    private static bool ContainsForbiddenContent(JsonElement element, ref string field)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (ForbiddenPropertyNames.Contains(property.Name))
                    {
                        field = property.Name;
                        return true;
                    }

                    if (property.Value.ValueKind == JsonValueKind.String
                        && ContainsPathTraversal(property.Value.GetString()))
                    {
                        field = property.Name;
                        return true;
                    }

                    if (ContainsForbiddenContent(property.Value, ref field))
                    {
                        return true;
                    }
                }

                return false;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (ContainsForbiddenContent(item, ref field))
                    {
                        return true;
                    }
                }

                return false;
            case JsonValueKind.String:
                if (ContainsPathTraversal(element.GetString()))
                {
                    field = "value";
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static bool ContainsPathTraversal(string? value) =>
        !string.IsNullOrEmpty(value)
        && (value.Contains("../", StringComparison.Ordinal)
            || value.Contains("..\\", StringComparison.Ordinal)
            || value.StartsWith('/')
            || value.StartsWith('\\'));

    private static int CountWords(string text)
    {
        var count = 0;
        var inWord = false;
        foreach (var rune in text.EnumerateRunes())
        {
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

        return count;
    }

    private static string DigestUtf8(ReadOnlyMemory<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes.Span)).ToLowerInvariant();

    private static BuiltinExecutionAttempt SuccessAttempt(string outputJson)
    {
        var output = Encoding.UTF8.GetBytes(outputJson);
        return new BuiltinExecutionAttempt(
            DeterministicInvocationOutcomes.Succeeded,
            null,
            output,
            DigestUtf8(output));
    }

    private static BuiltinExecutionAttempt FailedAttempt(string outcome, string failureCategory) =>
        new(outcome, failureCategory, null, null);

    private static DeterministicEvaluatorExecutionResult FailedResult(
        Guid attemptId,
        string inputRef,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        string outcome,
        string failureCategory,
        string? detailField) =>
        new(
            attemptId,
            outcome,
            failureCategory,
            null,
            null,
            inputRef,
            null,
            startedAt,
            finishedAt);
}

internal sealed record BuiltinExecutionAttempt(
    string Outcome,
    string? FailureCategory,
    ReadOnlyMemory<byte>? OutputUtf8,
    string? OutputContentDigest);
