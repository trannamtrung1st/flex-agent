using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public static class EnrollmentIdempotencyKey
{
    public const int MaxLength = 128;
    public static readonly TimeSpan Retention = TimeSpan.FromDays(90);

    public static string? Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return EnrollmentFailureCodes.InvalidField;
        }

        for (var index = 0; index < value.Length; index++)
        {
            if (!IsAllowed(value[index]))
            {
                return EnrollmentFailureCodes.InvalidField;
            }
        }

        return null;
    }

    private static bool IsAllowed(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '.' or '_' or ':' or '-';
}
