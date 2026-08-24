using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Submissions.Domain;

public static class AccommodationCommandDigest
{
    public static string Compute(
        string operationKind,
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        Guid? accommodationId,
        string? dimension,
        string? requestedValue,
        string? reasonCategory,
        bool fairnessException,
        long? expectedRevision,
        DateTimeOffset? expiresAtUtc = null)
    {
        var payload = string.Join(
            '\n',
            operationKind,
            organizationId.ToString("D"),
            activityId.ToString("D"),
            cohortId.ToString("D"),
            enrollmentId.ToString("D"),
            accommodationId?.ToString("D") ?? string.Empty,
            dimension ?? string.Empty,
            requestedValue ?? string.Empty,
            reasonCategory ?? string.Empty,
            fairnessException ? "1" : "0",
            expectedRevision?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            expiresAtUtc is { } expiry
                ? AccommodationPolicyNormalizer.FormatCanonicalInstant(expiry)
                : string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
