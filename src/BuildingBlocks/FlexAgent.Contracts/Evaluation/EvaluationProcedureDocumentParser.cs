using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlexAgent.Contracts.Evaluation;

public static class EvaluationProcedureDocumentParser
{
    public const string ProcedureSchema = "evaluation-procedure.v1";

    public const string InvalidDocument = "evaluation_procedure.invalid";

    private static readonly Regex StableId = new(
        "^[a-z][a-z0-9._-]{7,127}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AliasToken = new(
        "(^|[._-])(latest|current)([._-]|$)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Sha256Hex = new(
        "^[0-9a-f]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> RootProperties =
    [
        "procedure_schema",
        "procedure_id",
        "procedure_version",
        "criteria",
        "aggregation",
        "insufficiency_behavior",
        "not_applicable_behavior",
        "conflict_behavior",
        "retry_bounds",
        "resource_bounds",
        "review_policy_ref",
        "replacement_policy_ref",
        "lifecycle_policy_ref",
    ];

    private static readonly HashSet<string> CriterionProperties =
    [
        "criterion_id",
        "criterion_version",
        "display_label",
        "evaluator_mode",
        "permitted_statuses",
        "score_field",
        "confidence_field",
        "uncertainty_categories",
        "evidence_requirements",
        "deterministic_evaluator",
        "agent_io",
        "rationale_max_unicode_scalars",
        "provisional_feedback_permitted",
    ];

    private static readonly HashSet<string> EvaluatorProperties =
    [
        "evaluator_id",
        "evaluator_version",
        "evaluator_digest",
        "operation",
        "input_schema_id",
        "output_schema_id",
        "canonicalization_procedure",
        "configuration_digest",
        "dependency_digest",
        "cpu_time_limit",
        "elapsed_time_limit",
        "memory_limit_bytes",
        "output_limit_bytes",
        "network_egress",
        "executable_selection",
    ];

    private static readonly HashSet<string> EvaluatorModes =
    [
        "deterministic",
        "agent_assisted",
        "agent_judgment",
    ];

    private static readonly HashSet<string> Statuses =
    [
        "satisfied",
        "not_satisfied",
        "insufficient_evidence",
        "not_applicable",
        "conflict",
    ];

    private static readonly HashSet<string> SourceTypes =
    [
        "submission.direct_text",
        "submission.text_attachment",
        "session.transcript_item",
        "session.work_trace",
        "configuration.fact",
        "manifest.fact",
        "deterministic.fact",
    ];

    private static readonly HashSet<string> Operations =
    [
        "exact_compare",
        "schema_validate",
        "citation_validate",
        "bounded_calculation",
        "rubric_aggregate",
    ];

    public static readonly HashSet<string> AllowlistedEvaluatorIds =
    [
        "eval.builtin.exact-compare",
        "eval.builtin.schema-validate",
        "eval.builtin.citation-validate",
        "eval.builtin.bounded-calc",
        "eval.builtin.rubric-aggregate",
    ];

    public static bool TryParse(
        ReadOnlyMemory<byte> canonicalUtf8,
        out EvaluationProcedureV1 procedure,
        out string? failureCode)
    {
        procedure = null!;
        failureCode = InvalidDocument;
        try
        {
            using var document = JsonDocument.Parse(canonicalUtf8);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !HasOnly(document.RootElement, RootProperties)
                || !TryRequiredString(document.RootElement, "procedure_schema", out var schema)
                || schema != ProcedureSchema
                || !TryStableId(document.RootElement, "procedure_id", out var procedureId)
                || !TryStableId(document.RootElement, "procedure_version", out var procedureVersion)
                || !TryRequiredArray(document.RootElement, "criteria", out var criteriaElement)
                || criteriaElement.GetArrayLength() is < 1 or > 64
                || !TryAggregation(document.RootElement, out var aggregation)
                || !TryRequiredEnum(document.RootElement, "insufficiency_behavior",
                    ["complete_insufficient_and_block_aggregation", "complete_insufficient_and_continue"],
                    out var insufficiency)
                || !TryRequiredEnum(document.RootElement, "not_applicable_behavior",
                    ["exclude_from_aggregation", "treat_as_satisfied"],
                    out var notApplicable)
                || !TryRequiredEnum(document.RootElement, "conflict_behavior",
                    ["review_required_preserve_deterministic_fact", "reject_agent_claim"],
                    out var conflict)
                || !TryRetryBounds(document.RootElement, out var retry)
                || !TryResourceBounds(document.RootElement, out var resources)
                || !TryStableId(document.RootElement, "review_policy_ref", out var reviewPolicy)
                || !TryStableId(document.RootElement, "replacement_policy_ref", out var replacementPolicy)
                || !TryStableId(document.RootElement, "lifecycle_policy_ref", out var lifecyclePolicy))
            {
                return false;
            }

            var criteria = new List<EvaluationProcedureCriterionV1>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in criteriaElement.EnumerateArray())
            {
                if (!TryCriterion(item, out var criterion)
                    || !seen.Add($"{criterion.CriterionId}:{criterion.CriterionVersion}"))
                {
                    return false;
                }

                criteria.Add(criterion);
            }

            procedure = new EvaluationProcedureV1(
                schema,
                procedureId,
                procedureVersion,
                criteria,
                aggregation,
                insufficiency,
                notApplicable,
                conflict,
                retry,
                resources,
                reviewPolicy,
                replacementPolicy,
                lifecyclePolicy);
            failureCode = null;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryCriterion(JsonElement item, out EvaluationProcedureCriterionV1 criterion)
    {
        criterion = null!;
        if (item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, CriterionProperties)
            || !TryStableId(item, "criterion_id", out var id)
            || !TryStableId(item, "criterion_version", out var version)
            || !TryRequiredString(item, "display_label", out var label)
            || label.Length is < 1 or > 200
            || !TryRequiredEnum(item, "evaluator_mode", EvaluatorModes, out var mode)
            || !TryUniqueEnums(item, "permitted_statuses", Statuses, 1, 8, out var statuses)
            || !TryScoreField(item, out var score)
            || !TryConfidence(item, out var confidence)
            || !TryUniqueStrings(item, "uncertainty_categories", 1, 16, 64, out var uncertainty)
            || !TryEvidence(item, out var evidence)
            || !TryRequiredInt(item, "rationale_max_unicode_scalars", 1, 4000, out var rationaleMax)
            || !TryRequiredBool(item, "provisional_feedback_permitted", out var provisional))
        {
            return false;
        }

        var hasEvaluator = item.TryGetProperty("deterministic_evaluator", out var evaluatorElement);
        var hasAgent = item.TryGetProperty("agent_io", out var agentElement);
        DeterministicEvaluatorBindingV1? evaluator = null;
        EvaluationAgentIoV1? agent = null;
        if (hasEvaluator && !TryEvaluator(evaluatorElement, out evaluator))
        {
            return false;
        }

        if (hasAgent && !TryAgentIo(agentElement, out agent))
        {
            return false;
        }

        var modeValid = mode switch
        {
            "deterministic" => hasEvaluator && !hasAgent,
            "agent_assisted" => hasEvaluator && hasAgent,
            "agent_judgment" => hasAgent && !hasEvaluator,
            _ => false,
        };
        if (!modeValid)
        {
            return false;
        }

        criterion = new EvaluationProcedureCriterionV1(
            id,
            version,
            label,
            mode,
            statuses,
            score,
            confidence,
            uncertainty,
            evidence,
            rationaleMax,
            provisional,
            evaluator,
            agent);
        return true;
    }

    private static bool TryEvaluator(JsonElement item, out DeterministicEvaluatorBindingV1 evaluator)
    {
        evaluator = null!;
        if (item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, EvaluatorProperties)
            || !TryStableId(item, "evaluator_id", out var evaluatorId)
            || !AllowlistedEvaluatorIds.Contains(evaluatorId)
            || !TryStableId(item, "evaluator_version", out var evaluatorVersion)
            || !TryRequiredString(item, "evaluator_digest", out var evaluatorDigest)
            || !Sha256Hex.IsMatch(evaluatorDigest)
            || !TryRequiredEnum(item, "operation", Operations, out var operation)
            || !TryStableId(item, "input_schema_id", out var inputSchema)
            || !TryStableId(item, "output_schema_id", out var outputSchema)
            || !TryRequiredString(item, "canonicalization_procedure", out var canon)
            || canon != "jcs-sha256-v1"
            || !TryRequiredString(item, "configuration_digest", out var configurationDigest)
            || !Sha256Hex.IsMatch(configurationDigest)
            || !TryRequiredString(item, "dependency_digest", out var dependencyDigest)
            || !Sha256Hex.IsMatch(dependencyDigest)
            || !TryRequiredString(item, "cpu_time_limit", out var cpu)
            || !EvaluationPositiveDuration.IsValid(cpu)
            || !TryRequiredString(item, "elapsed_time_limit", out var elapsed)
            || !EvaluationPositiveDuration.IsValid(elapsed)
            || !TryRequiredInt(item, "memory_limit_bytes", 1, 268435456, out var memory)
            || !TryRequiredInt(item, "output_limit_bytes", 1, 1048576, out var output)
            || !TryRequiredString(item, "network_egress", out var network)
            || network != "prohibited"
            || !TryRequiredString(item, "executable_selection", out var executable)
            || executable != "prohibited")
        {
            return false;
        }

        evaluator = new DeterministicEvaluatorBindingV1(
            evaluatorId,
            evaluatorVersion,
            evaluatorDigest,
            operation,
            inputSchema,
            outputSchema,
            canon,
            configurationDigest,
            dependencyDigest,
            cpu,
            elapsed,
            memory,
            output,
            network,
            executable);
        return true;
    }

    private static bool TryAgentIo(JsonElement item, out EvaluationAgentIoV1 agent)
    {
        agent = null!;
        if (item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, ["input_schema_id", "output_schema_id", "max_context_unicode_scalars"])
            || !TryStableId(item, "input_schema_id", out var input)
            || !TryStableId(item, "output_schema_id", out var output)
            || !TryRequiredInt(item, "max_context_unicode_scalars", 1, 16384, out var maxContext))
        {
            return false;
        }

        agent = new EvaluationAgentIoV1(input, output, maxContext);
        return true;
    }

    private static bool TryScoreField(JsonElement parent, out object score)
    {
        score = null!;
        if (!parent.TryGetProperty("score_field", out var item) || item.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryRequiredString(item, "field_kind", out var kind))
        {
            return false;
        }

        switch (kind)
        {
            case "none":
                if (!HasOnly(item, ["field_kind"]))
                {
                    return false;
                }

                score = new ScoreFieldNoneV1(kind);
                return true;
            case "integer_range":
                if (!HasOnly(item, ["field_kind", "minimum", "maximum"])
                    || !TryRequiredInt(item, "minimum", -1_000_000, 1_000_000, out var min)
                    || !TryRequiredInt(item, "maximum", -1_000_000, 1_000_000, out var max)
                    || min > max)
                {
                    return false;
                }

                score = new IntegerRangeScoreFieldV1(kind, min, max);
                return true;
            case "enumerated_decision":
                if (!HasOnly(item, ["field_kind", "permitted_values"])
                    || !TryUniqueStrings(item, "permitted_values", 2, 16, 64, out var values))
                {
                    return false;
                }

                score = new EnumeratedDecisionScoreFieldV1(kind, values);
                return true;
            default:
                return false;
        }
    }

    private static bool TryConfidence(JsonElement parent, out EvaluationConfidenceFieldV1 confidence)
    {
        confidence = null!;
        if (!parent.TryGetProperty("confidence_field", out var item)
            || item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, ["field_kind", "permitted_values"])
            || !TryRequiredString(item, "field_kind", out var kind)
            || kind != "qualitative"
            || !TryUniqueStrings(item, "permitted_values", 2, 8, 32, out var values))
        {
            return false;
        }

        confidence = new EvaluationConfidenceFieldV1(kind, values);
        return true;
    }

    private static bool TryEvidence(JsonElement parent, out EvaluationEvidenceRequirementsV1 evidence)
    {
        evidence = null!;
        if (!parent.TryGetProperty("evidence_requirements", out var item)
            || item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, ["minimum_items", "maximum_items", "permitted_source_types", "whole_item_fallback_permitted"])
            || !TryRequiredInt(item, "minimum_items", 0, 32, out var min)
            || !TryRequiredInt(item, "maximum_items", 1, 32, out var max)
            || min > max
            || !TryUniqueEnums(item, "permitted_source_types", SourceTypes, 1, 7, out var types)
            || !TryRequiredBool(item, "whole_item_fallback_permitted", out var wholeItem))
        {
            return false;
        }

        evidence = new EvaluationEvidenceRequirementsV1(min, max, types, wholeItem);
        return true;
    }

    private static bool TryAggregation(JsonElement parent, out EvaluationAggregationV1 aggregation)
    {
        aggregation = null!;
        if (!parent.TryGetProperty("aggregation", out var item)
            || item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, ["aggregation_kind"])
            || !TryRequiredEnum(item, "aggregation_kind",
                ["none", "all_required_satisfied", "integer_sum"],
                out var kind))
        {
            return false;
        }

        aggregation = new EvaluationAggregationV1(kind);
        return true;
    }

    private static bool TryRetryBounds(JsonElement parent, out EvaluationRetryBoundsV1 retry)
    {
        retry = null!;
        if (!parent.TryGetProperty("retry_bounds", out var item)
            || item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, ["max_attempts", "backoff", "attempt_timeout"])
            || !TryRequiredInt(item, "max_attempts", 1, 8, out var maxAttempts)
            || !TryRequiredString(item, "backoff", out var backoff)
            || !EvaluationPositiveDuration.IsValid(backoff)
            || !TryRequiredString(item, "attempt_timeout", out var timeout)
            || !EvaluationPositiveDuration.IsValid(timeout))
        {
            return false;
        }

        retry = new EvaluationRetryBoundsV1(maxAttempts, backoff, timeout);
        return true;
    }

    private static bool TryResourceBounds(JsonElement parent, out EvaluationResourceBoundsV1 resources)
    {
        resources = null!;
        if (!parent.TryGetProperty("resource_bounds", out var item)
            || item.ValueKind != JsonValueKind.Object
            || !HasOnly(item, ["elapsed_timeout", "max_evidence_items", "max_provider_output_bytes"])
            || !TryRequiredString(item, "elapsed_timeout", out var elapsed)
            || !EvaluationPositiveDuration.IsValid(elapsed)
            || !TryRequiredInt(item, "max_evidence_items", 1, 128, out var evidence)
            || !TryRequiredInt(item, "max_provider_output_bytes", 1, 65536, out var output))
        {
            return false;
        }

        resources = new EvaluationResourceBoundsV1(elapsed, evidence, output);
        return true;
    }

    private static bool HasOnly(JsonElement obj, HashSet<string> allowed)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryStableId(JsonElement obj, string name, out string value)
    {
        value = "";
        return TryRequiredString(obj, name, out value)
            && value.Length is >= 8 and <= 128
            && StableId.IsMatch(value)
            && !AliasToken.IsMatch(value);
    }

    private static bool TryRequiredString(JsonElement obj, string name, out string value)
    {
        value = "";
        return obj.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            && element.GetString() is { } text
            && text.Length > 0
            && (value = text).Length > 0;
    }

    private static bool TryRequiredEnum(JsonElement obj, string name, HashSet<string> allowed, out string value) =>
        TryRequiredString(obj, name, out value) && allowed.Contains(value);

    private static bool TryRequiredInt(JsonElement obj, string name, int min, int max, out int value)
    {
        value = 0;
        return obj.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out value)
            && value >= min
            && value <= max;
    }

    private static bool TryRequiredBool(JsonElement obj, string name, out bool value)
    {
        value = false;
        if (!obj.TryGetProperty(name, out var element)
            || element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = element.GetBoolean();
        return true;
    }

    private static bool TryRequiredArray(JsonElement obj, string name, out JsonElement array)
    {
        array = default;
        if (!obj.TryGetProperty(name, out array) || array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return true;
    }

    private static bool TryUniqueEnums(
        JsonElement obj,
        string name,
        HashSet<string> allowed,
        int min,
        int max,
        out IReadOnlyList<string> values)
    {
        values = [];
        if (!TryRequiredArray(obj, name, out var array)
            || array.GetArrayLength() < min
            || array.GetArrayLength() > max)
        {
            return false;
        }

        var parsed = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String
                || item.GetString() is not { } text
                || !allowed.Contains(text)
                || !seen.Add(text))
            {
                return false;
            }

            parsed.Add(text);
        }

        values = parsed;
        return true;
    }

    private static bool TryUniqueStrings(
        JsonElement obj,
        string name,
        int min,
        int max,
        int itemMax,
        out IReadOnlyList<string> values)
    {
        values = [];
        if (!TryRequiredArray(obj, name, out var array)
            || array.GetArrayLength() < min
            || array.GetArrayLength() > max)
        {
            return false;
        }

        var parsed = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String
                || item.GetString() is not { } text
                || text.Length is < 1
                || text.Length > itemMax
                || !seen.Add(text))
            {
                return false;
            }

            parsed.Add(text);
        }

        values = parsed;
        return true;
    }
}
