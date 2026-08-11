using System.Text.RegularExpressions;

namespace FlexAgent.Contract.Tests.Harness;

internal static partial class Iso8601DurationSemantics
{
    private const int MinimumSeconds = 1;
    private const int MaximumSeconds = 24 * 60 * 60;

    [GeneratedRegex(@"^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?$", RegexOptions.CultureInvariant)]
    private static partial Regex DurationPattern();

    public static bool TryParseTotalSeconds(string duration, out int totalSeconds)
    {
        totalSeconds = 0;
        var match = DurationPattern().Match(duration);
        if (!match.Success)
        {
            return false;
        }

        if (match.Groups[1].Success)
        {
            totalSeconds += int.Parse(match.Groups[1].Value) * 3600;
        }

        if (match.Groups[2].Success)
        {
            totalSeconds += int.Parse(match.Groups[2].Value) * 60;
        }

        if (match.Groups[3].Success)
        {
            totalSeconds += int.Parse(match.Groups[3].Value);
        }

        return totalSeconds > 0;
    }

    public static bool IsWithinTimerPolicyBounds(string duration)
    {
        return TryParseTotalSeconds(duration, out var totalSeconds)
            && totalSeconds is >= MinimumSeconds and <= MaximumSeconds;
    }
}
