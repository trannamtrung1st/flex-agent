using System.Globalization;
using System.Text.Json;

namespace FlexAgent.Sessions.Domain;

public sealed record HostedFrozenTimingPolicy(
    int BudgetSeconds,
    IReadOnlyList<HostedTimingWarningThreshold> WarningSchedule);

public static class HostedSessionFrozenTiming
{
    public static HostedFrozenTimingPolicy Resolve(
        string? perAttemptDurationValue,
        IReadOnlyDictionary<string, string>? timingValues = null)
    {
        int? frozen = null;
        if (int.TryParse(
                perAttemptDurationValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed)
            && parsed > 0)
        {
            frozen = parsed;
        }

        var warnings = new List<HostedTimingWarningThreshold>();
        if (timingValues is not null)
        {
            TryAddWarning(warnings, timingValues, "warning_imminent_remaining_seconds", "imminent");
            TryAddWarning(warnings, timingValues, "warning_approaching_remaining_seconds", "approaching");
        }

        return new HostedFrozenTimingPolicy(HostedSessionTiming.ResolveBudget(frozen), warnings);
    }

    public static HostedFrozenTimingPolicy FromActivationBaselineDocument(string? documentJson)
    {
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return Resolve(null);
        }

        try
        {
            using var document = JsonDocument.Parse(documentJson);
            if (!document.RootElement.TryGetProperty("fairness_domains", out var domains)
                || domains.ValueKind != JsonValueKind.Array)
            {
                return Resolve(null);
            }

            foreach (var domain in domains.EnumerateArray())
            {
                if (!domain.TryGetProperty("domain_key", out var key)
                    || key.GetString() != "timing"
                    || !domain.TryGetProperty("effective_value", out var values)
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

                map.TryGetValue("per_attempt_duration_seconds", out var duration);
                return Resolve(duration, map);
            }
        }
        catch (JsonException)
        {
            return Resolve(null);
        }

        return Resolve(null);
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
