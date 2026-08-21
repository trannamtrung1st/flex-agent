using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public static class AssessmentIdempotencyKey
{
    public const int MaxLength = 128;

    public static string? Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return AssessmentFailureCodes.InvalidField;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (!IsAllowed(value[index]))
            {
                return AssessmentFailureCodes.InvalidField;
            }
        }

        return null;
    }

    public static int StatusForActivation(bool succeeded, string outcomeCode)
    {
        if (succeeded)
        {
            return 200;
        }

        return string.Equals(outcomeCode, AssessmentFailureCodes.InvalidField, StringComparison.Ordinal)
            ? 400
            : 409;
    }

    private static bool IsAllowed(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '.' or '_' or ':' or '-';
}
