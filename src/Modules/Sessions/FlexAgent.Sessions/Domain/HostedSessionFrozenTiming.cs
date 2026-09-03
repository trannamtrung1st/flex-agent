using System.Globalization;
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
    IReadOnlyList<HostedTimingWarningThreshold> WarningSchedule)
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
