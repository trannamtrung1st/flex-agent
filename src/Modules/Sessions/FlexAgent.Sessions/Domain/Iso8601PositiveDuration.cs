using System.Globalization;
using System.Text.RegularExpressions;

namespace FlexAgent.Sessions.Domain;

public sealed partial class Iso8601PositiveDuration : IComparable<Iso8601PositiveDuration>
{
    private const int MinimumSeconds = 1;
    private const int MaximumSeconds = 24 * 60 * 60;

    [GeneratedRegex(@"^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?$", RegexOptions.CultureInvariant)]
    private static partial Regex DurationPattern();

    private Iso8601PositiveDuration(string wireValue, int totalSeconds)
    {
        WireValue = wireValue;
        TotalSeconds = totalSeconds;
    }

    public string WireValue { get; }

    public int TotalSeconds { get; }

    public static bool TryParse(string? duration, out Iso8601PositiveDuration parsed)
    {
        parsed = null!;
        if (string.IsNullOrWhiteSpace(duration))
        {
            return false;
        }

        var match = DurationPattern().Match(duration);
        if (!match.Success)
        {
            return false;
        }

        long totalSeconds = 0;
        if (match.Groups[1].Success
            && !TryAddComponent(match.Groups[1].Value, 3600, ref totalSeconds))
        {
            return false;
        }

        if (match.Groups[2].Success
            && !TryAddComponent(match.Groups[2].Value, 60, ref totalSeconds))
        {
            return false;
        }

        if (match.Groups[3].Success
            && !TryAddComponent(match.Groups[3].Value, 1, ref totalSeconds))
        {
            return false;
        }

        if (totalSeconds is < MinimumSeconds or > MaximumSeconds)
        {
            return false;
        }

        parsed = new Iso8601PositiveDuration(duration, (int)totalSeconds);
        return true;
    }

    public int CompareTo(Iso8601PositiveDuration? other)
    {
        if (other is null)
        {
            return 1;
        }

        return TotalSeconds.CompareTo(other.TotalSeconds);
    }

    private static bool TryAddComponent(string digits, int multiplier, ref long totalSeconds)
    {
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var component))
        {
            return false;
        }

        try
        {
            checked
            {
                totalSeconds += component * multiplier;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return true;
    }
}
