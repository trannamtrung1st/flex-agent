using System.Globalization;

namespace FlexAgent.Submissions.Application;

public static class EnrollmentListCursor
{
    public static string Format(DateTimeOffset updatedAt, Guid enrollmentId) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{updatedAt.UtcTicks}:{enrollmentId:D}")));

    public static bool TryParse(string? cursor, out DateTimeOffset? updatedAt, out Guid? enrollmentId)
    {
        updatedAt = null;
        enrollmentId = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split(':', 2);
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
                && Guid.TryParse(parts[1], out var id)
                && ticks >= DateTimeOffset.MinValue.UtcTicks
                && ticks <= DateTimeOffset.MaxValue.UtcTicks)
            {
                updatedAt = new DateTimeOffset(ticks, TimeSpan.Zero);
                enrollmentId = id;
                return true;
            }
        }
        catch (FormatException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        return false;
    }
}
