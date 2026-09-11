namespace FlexAgent.Evaluation.Domain;

internal static class EvaluationUntrustedModelOutputPolicy
{
    internal static bool ContainsProhibitedContent(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (value.Contains('\0', StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var pattern in ProhibitedPatterns)
        {
            if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly string[] ProhibitedPatterns =
    [
        "hidden_prompt",
        "<script",
        "ignore prior instructions",
        "ignore previous instructions",
        "disregard the rubric",
        "change the rubric",
        "bypass citation",
        "execute tool",
        "write to memory",
        "release the result",
        "expected-answer key",
        "credential.bind",
        "system instructions:",
    ];
}
