namespace FlexAgent.Evaluation.Domain;

internal static class EvaluationProhibitedModelOutputDisclosurePolicy
{
    internal static bool ContainsProhibitedDisclosure(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (value.Contains('\0', StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var pattern in ProhibitedDisclosurePatterns)
        {
            if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly string[] ProhibitedDisclosurePatterns =
    [
        "hidden_prompt",
        "<script",
        "expected-answer key",
        "credential.bind",
        "chain-of-thought",
        "chain_of_thought",
    ];
}
