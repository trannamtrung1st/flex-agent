using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Submissions.Domain;

public static class EnrollmentCommandDigest
{
    public static string Compute(
        string operationKind,
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid? enrollmentId,
        Guid? participantActorId,
        string? reasonCode,
        long? expectedRevision)
    {
        var payload = string.Join(
            '\n',
            operationKind,
            organizationId.ToString("D"),
            activityId.ToString("D"),
            cohortId.ToString("D"),
            enrollmentId?.ToString("D") ?? string.Empty,
            participantActorId?.ToString("D") ?? string.Empty,
            reasonCode ?? string.Empty,
            expectedRevision?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
