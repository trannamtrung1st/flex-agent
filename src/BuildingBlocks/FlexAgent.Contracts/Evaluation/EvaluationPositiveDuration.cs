using System.Text.RegularExpressions;

namespace FlexAgent.Contracts.Evaluation;

public static class EvaluationPositiveDuration
{
    private static readonly Regex Pattern = new(
        "^PT(?:(?:[1-9]|[1-5][0-9])S|(?:[1-9]|[1-5][0-9])M|(?:[1-9]|1[0-9]|2[0-3])H|24H|(?:[1-9]|1[0-9]|2[0-3])H(?:[1-9]|[1-5][0-9])M|(?:[1-9]|[1-5][0-9])M(?:[1-9]|[1-5][0-9])S|(?:[1-9]|1[0-9]|2[0-3])H(?:[1-9]|[1-5][0-9])S|(?:[1-9]|1[0-9]|2[0-3])H(?:[1-9]|[1-5][0-9])M(?:[1-9]|[1-5][0-9])S)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value);

    public static bool TryParseTotalSeconds(string? value, out int totalSeconds)
    {
        totalSeconds = 0;
        if (!IsValid(value))
        {
            return false;
        }

        var index = 2;
        while (index < value!.Length)
        {
            var start = index;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                index++;
            }

            if (start == index || index >= value.Length)
            {
                totalSeconds = 0;
                return false;
            }

            if (!int.TryParse(value[start..index], out var amount) || amount <= 0)
            {
                totalSeconds = 0;
                return false;
            }

            var added = value[index] switch
            {
                'H' => checked(amount * 3600),
                'M' => checked(amount * 60),
                'S' => amount,
                _ => -1,
            };
            if (added < 0)
            {
                totalSeconds = 0;
                return false;
            }

            totalSeconds = checked(totalSeconds + added);
            index++;
        }

        return totalSeconds > 0;
    }
}
