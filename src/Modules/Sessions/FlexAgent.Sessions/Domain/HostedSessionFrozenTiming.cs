using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FlexAgent.Sessions.Domain;

public enum HostedTimingReconstruction
{
    Unbounded,
    Timed,
    Unavailable,
}

public sealed record HostedFrozenTimingPolicy(
    HostedTimingReconstruction Reconstruction,
    int? BudgetSeconds,
    IReadOnlyList<HostedTimingWarningThreshold> WarningSchedule,
    DateTimeOffset? HardEndAtUtc = null)
{
    public static HostedFrozenTimingPolicy UnboundedPolicy { get; } =
        new(HostedTimingReconstruction.Unbounded, null, []);

    public static HostedFrozenTimingPolicy UnavailablePolicy { get; } =
        new(HostedTimingReconstruction.Unavailable, null, []);

    public HostedFrozenTimingPolicy WithEffectiveDuration(int? effectivePerAttemptDurationSeconds)
    {
        if (Reconstruction == HostedTimingReconstruction.Unavailable
            || effectivePerAttemptDurationSeconds is not > 0)
        {
            return this;
        }

        return this with
        {
            Reconstruction = HostedTimingReconstruction.Timed,
            BudgetSeconds = effectivePerAttemptDurationSeconds,
        };
    }
}

public static class HostedSessionFrozenTiming
{
    public static HostedFrozenTimingPolicy Resolve(
        string? perAttemptDurationValue,
        IReadOnlyDictionary<string, string>? timingValues = null)
    {
        if (string.IsNullOrWhiteSpace(perAttemptDurationValue)
            || string.Equals(perAttemptDurationValue, "unbounded", StringComparison.OrdinalIgnoreCase))
        {
            return HostedFrozenTimingPolicy.UnboundedPolicy with
            {
                WarningSchedule = ReadWarnings(timingValues),
            };
        }

        if (!int.TryParse(
                perAttemptDurationValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed)
            || parsed <= 0)
        {
            return HostedFrozenTimingPolicy.UnavailablePolicy;
        }

        return new HostedFrozenTimingPolicy(
            HostedTimingReconstruction.Timed,
            parsed,
            ReadWarnings(timingValues));
    }

    public static HostedFrozenTimingPolicy FromActivationBaselineDocument(string? documentJson)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return HostedFrozenTimingPolicy.UnavailablePolicy;
        }

        try
        {
            using var document = JsonDocument.Parse(documentJson);
            if (!TryGetProperty(document.RootElement, "fairness_domains", out var domains)
                || domains.ValueKind != JsonValueKind.Array)
            {
                return HostedFrozenTimingPolicy.UnavailablePolicy;
            }

            foreach (var domain in domains.EnumerateArray())
            {
                if (!TryGetProperty(domain, "domain_key", out var key)
                    || key.GetString() != "timing"
                    || !TryGetProperty(domain, "effective_value", out var values)
                    || values.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var property in values.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String
                        && property.Value.GetString() is { } text)
                    {
                        map[property.Name] = text;
                    }
                }

                if (!map.TryGetValue("per_attempt_duration_seconds", out var duration))
                {
                    return HostedFrozenTimingPolicy.UnavailablePolicy;
                }

                return Resolve(duration, map);
            }
        }
        catch (JsonException)
        {
            return HostedFrozenTimingPolicy.UnavailablePolicy;
        }

        return HostedFrozenTimingPolicy.UnavailablePolicy;
    }

    public static HostedFrozenTimingPolicy Compose(
        string? baselineDocumentJson,
        int? effectivePerAttemptDurationSeconds,
        bool applyEffectiveDuration)
    {
        var policy = FromActivationBaselineDocument(baselineDocumentJson);
        return applyEffectiveDuration
            ? policy.WithEffectiveDuration(effectivePerAttemptDurationSeconds)
            : policy;
    }

    public static DateTimeOffset ResolveHardEndAtUtc(
        DateTimeOffset effectiveAttemptStartExclusiveEndUtc,
        DateTimeOffset effectiveSubmissionExclusiveEndUtc) =>
        effectiveAttemptStartExclusiveEndUtc <= effectiveSubmissionExclusiveEndUtc
            ? effectiveAttemptStartExclusiveEndUtc
            : effectiveSubmissionExclusiveEndUtc;

    public static HostedFrozenTimingPolicy ComposeFromEffective(
        string? baselineDocumentJson,
        int? effectivePerAttemptDurationSeconds,
        bool applyEffectiveDuration,
        DateTimeOffset effectiveAttemptStartExclusiveEndUtc,
        DateTimeOffset effectiveSubmissionExclusiveEndUtc)
    {
        var policy = Compose(baselineDocumentJson, effectivePerAttemptDurationSeconds, applyEffectiveDuration);
        return policy with
        {
            HardEndAtUtc = ResolveHardEndAtUtc(
                effectiveAttemptStartExclusiveEndUtc,
                effectiveSubmissionExclusiveEndUtc),
        };
    }

    public static string ToDocumentJson(HostedFrozenTimingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("reconstruction", policy.Reconstruction.ToString().ToLowerInvariant());
            if (policy.BudgetSeconds is int budget)
            {
                writer.WriteNumber("budget_seconds", budget);
            }
            else
            {
                writer.WriteNull("budget_seconds");
            }

            writer.WritePropertyName("warnings");
            writer.WriteStartArray();
            foreach (var warning in policy.WarningSchedule)
            {
                writer.WriteStartObject();
                writer.WriteString("code", warning.Code);
                writer.WriteNumber("remaining_seconds", warning.RemainingSecondsThreshold);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            if (policy.HardEndAtUtc is DateTimeOffset hardEnd)
            {
                writer.WriteString("hard_end_at_utc", hardEnd.ToString("O"));
            }
            else
            {
                writer.WriteNull("hard_end_at_utc");
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static HostedFrozenTimingPolicy FromDocumentJson(string? documentJson)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return HostedFrozenTimingPolicy.UnavailablePolicy;
        }

        try
        {
            using var document = JsonDocument.Parse(documentJson);
            var root = document.RootElement;
            if (!TryGetProperty(root, "reconstruction", out var reconstruction)
                || reconstruction.GetString() is not { } kind)
            {
                return HostedFrozenTimingPolicy.UnavailablePolicy;
            }

            int? budget = null;
            if (TryGetProperty(root, "budget_seconds", out var budgetElement)
                && budgetElement.ValueKind == JsonValueKind.Number
                && budgetElement.TryGetInt32(out var parsedBudget)
                && parsedBudget > 0)
            {
                budget = parsedBudget;
            }

            var warnings = new List<HostedTimingWarningThreshold>();
            if (TryGetProperty(root, "warnings", out var warningElement)
                && warningElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in warningElement.EnumerateArray())
                {
                    if (TryGetProperty(item, "code", out var code)
                        && TryGetProperty(item, "remaining_seconds", out var seconds)
                        && code.GetString() is { Length: > 0 } warningCode
                        && seconds.TryGetInt32(out var remaining)
                        && remaining > 0)
                    {
                        warnings.Add(new HostedTimingWarningThreshold(warningCode, remaining));
                    }
                }
            }

            DateTimeOffset? hardEnd = null;
            if (TryGetProperty(root, "hard_end_at_utc", out var hardEndElement)
                && hardEndElement.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(
                    hardEndElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsedHardEnd))
            {
                hardEnd = parsedHardEnd.ToUniversalTime();
            }

            return kind switch
            {
                "unbounded" => HostedFrozenTimingPolicy.UnboundedPolicy with
                {
                    WarningSchedule = warnings,
                    HardEndAtUtc = hardEnd,
                },
                "timed" when budget is > 0 => new HostedFrozenTimingPolicy(
                    HostedTimingReconstruction.Timed,
                    budget,
                    warnings,
                    hardEnd),
                _ => HostedFrozenTimingPolicy.UnavailablePolicy,
            };
        }
        catch (JsonException)
        {
            return HostedFrozenTimingPolicy.UnavailablePolicy;
        }
    }

    private static bool TryGetProperty(JsonElement element, string snakeName, out JsonElement value)
    {
        if (element.TryGetProperty(snakeName, out value))
        {
            return true;
        }

        var camel = ToCamelCase(snakeName);
        return !string.Equals(camel, snakeName, StringComparison.Ordinal)
            && element.TryGetProperty(camel, out value);
    }

    private static string ToCamelCase(string snakeName)
    {
        var parts = snakeName.Split('_');
        if (parts.Length == 1)
        {
            return snakeName;
        }

        return parts[0] + string.Concat(parts.Skip(1).Select(static part =>
            part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static IReadOnlyList<HostedTimingWarningThreshold> ReadWarnings(
        IReadOnlyDictionary<string, string>? timingValues)
    {
        var warnings = new List<HostedTimingWarningThreshold>();
        if (timingValues is null)
        {
            return warnings;
        }

        TryAddWarning(warnings, timingValues, "warning_imminent_remaining_seconds", "imminent");
        TryAddWarning(warnings, timingValues, "warning_approaching_remaining_seconds", "approaching");
        return warnings;
    }

    private static void TryAddWarning(
        List<HostedTimingWarningThreshold> warnings,
        IReadOnlyDictionary<string, string> timingValues,
        string key,
        string code)
    {
        if (timingValues.TryGetValue(key, out var raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            && seconds > 0)
        {
            warnings.Add(new HostedTimingWarningThreshold(code, seconds));
        }
    }
}
