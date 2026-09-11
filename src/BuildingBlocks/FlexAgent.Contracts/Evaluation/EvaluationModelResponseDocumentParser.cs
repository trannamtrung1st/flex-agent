using System.Text.Json;
using System.Text.RegularExpressions;
using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Contracts.Evaluation;

public static class EvaluationModelResponseDocumentParser
{
    public const string InvalidDocument = "evaluation_model_response.invalid";

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
        "schema_version",
        "output_schema_id",
        "criterion_id",
        "criterion_version",
        "evaluator_mode",
        "status",
        "score",
        "confidence",
        "uncertainty",
        "rationale",
        "provisional_feedback",
        "evidence_ids",
        "deterministic_invocation_id",
        "response_ref",
    ];

    private static readonly HashSet<string> EvaluatorModes =
    [
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

    public static bool TryParse(
        ReadOnlySpan<byte> canonicalUtf8,
        out EvaluationModelResponseV1 response,
        out string? failureCode)
    {
        response = null!;
        failureCode = InvalidDocument;
        try
        {
            using var document = JsonDocument.Parse(canonicalUtf8.ToArray());
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !HasOnly(document.RootElement, RootProperties)
                || !TryRequiredString(document.RootElement, "schema_version", out var schemaVersion)
                || schemaVersion != "v1"
                || !TryStableId(document.RootElement, "output_schema_id", out var outputSchemaId)
                || !TryStableId(document.RootElement, "criterion_id", out var criterionId)
                || !TryStableId(document.RootElement, "criterion_version", out var criterionVersion)
                || !TryRequiredString(document.RootElement, "evaluator_mode", out var evaluatorMode)
                || !EvaluatorModes.Contains(evaluatorMode)
                || !TryRequiredString(document.RootElement, "status", out var status)
                || !Statuses.Contains(status)
                || !TryScore(document.RootElement, out var score)
                || !TryRequiredString(document.RootElement, "confidence", out var confidence)
                || confidence.Length < 1
                || confidence.Length > 32
                || !TryStringArray(document.RootElement, "uncertainty", 16, out var uncertainty)
                || !TryRequiredString(document.RootElement, "rationale", out var rationale)
                || rationale.Length < 1
                || rationale.Length > 4000
                || !TryOptionalString(document.RootElement, "provisional_feedback", 2000, out var provisionalFeedback)
                || !TryStableIdArray(document.RootElement, "evidence_ids", 64, out var evidenceIds)
                || !TryOptionalStableId(document.RootElement, "deterministic_invocation_id", out var deterministicInvocationId)
                || !TryProtectedRef(document.RootElement, "response_ref", out var responseRef))
            {
                return false;
            }

            response = new EvaluationModelResponseV1(
                schemaVersion,
                outputSchemaId,
                criterionId,
                criterionVersion,
                evaluatorMode,
                status,
                confidence,
                uncertainty,
                rationale,
                evidenceIds,
                responseRef,
                score,
                provisionalFeedback,
                deterministicInvocationId);
            failureCode = null;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool HasOnly(JsonElement element, HashSet<string> allowed)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryRequiredString(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return value.Length > 0;
    }

    private static bool TryOptionalString(
        JsonElement element,
        string name,
        int maxLength,
        out string? value)
    {
        value = null;
        if (!element.TryGetProperty(name, out var property))
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return value is not null && value.Length >= 1 && value.Length <= maxLength;
    }

    private static bool TryStableId(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        return element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String
            && IsStableId(property.GetString(), out value);
    }

    private static bool TryOptionalStableId(JsonElement element, string name, out string? value)
    {
        value = null;
        if (!element.TryGetProperty(name, out var property))
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String
            || !IsStableId(property.GetString(), out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool IsStableId(string? candidate, out string value)
    {
        value = candidate ?? string.Empty;
        return !string.IsNullOrWhiteSpace(candidate)
            && StableId.IsMatch(candidate)
            && !AliasToken.IsMatch(candidate);
    }

    private static bool TryScore(JsonElement element, out object? score)
    {
        score = null;
        if (!element.TryGetProperty("score", out var property))
        {
            return true;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.Number when property.TryGetInt32(out var integer) => AssignScore(score = integer),
            JsonValueKind.String when TryScoreString(property.GetString(), out score) => true,
            _ => false,
        };
    }

    private static bool TryScoreString(string? candidate, out object? score)
    {
        score = null;
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > 64)
        {
            return false;
        }

        score = candidate;
        return true;
    }

    private static bool AssignScore(object? score) => true;

    private static bool TryStringArray(
        JsonElement element,
        string name,
        int maxItems,
        out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (!element.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var length = property.GetArrayLength();
        if (length < 1 || length > maxItems)
        {
            return false;
        }

        var items = new List<string>(length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var text = item.GetString();
            if (text is null || text.Length < 1 || text.Length > 64 || !seen.Add(text))
            {
                return false;
            }

            items.Add(text);
        }

        values = items;
        return true;
    }

    private static bool TryStableIdArray(
        JsonElement element,
        string name,
        int maxItems,
        out IReadOnlyList<string> values)
    {
        values = Array.Empty<string>();
        if (!element.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var length = property.GetArrayLength();
        if (length < 1 || length > maxItems)
        {
            return false;
        }

        var items = new List<string>(length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in property.EnumerateArray())
        {
            if (!IsStableId(item.GetString(), out var stableId)
                || !seen.Add(stableId))
            {
                return false;
            }

            items.Add(stableId);
        }

        values = items;
        return true;
    }

    private static bool TryProtectedRef(JsonElement element, string name, out ProtectedPayloadRefV1 value)
    {
        value = null!;
        if (!element.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.Object
            || !TryRequiredString(property, "protected_ref", out var protectedRef)
            || !IsStableId(protectedRef, out protectedRef)
            || !TryRequiredString(property, "content_digest", out var contentDigest)
            || !Sha256Hex.IsMatch(contentDigest))
        {
            return false;
        }

        value = new ProtectedPayloadRefV1(protectedRef, contentDigest);
        return true;
    }
}

