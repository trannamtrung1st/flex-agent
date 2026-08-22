using System.Globalization;
using System.Security.Cryptography;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed record EnrollmentListCursorScope(
    string QueryKind,
    Guid OrganizationId,
    Guid ActorId,
    Guid ActivityId,
    Guid CohortId)
{
    public const string MyWork = "my-work";
    public const string Enrollments = "enrollments";

    public static EnrollmentListCursorScope ForMyWork(EnrollmentActorContext actor) =>
        new(MyWork, actor.Organization.OrganizationId, actor.Actor.ActorId, Guid.Empty, Guid.Empty);

    public static EnrollmentListCursorScope ForEnrollments(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId) =>
        new(Enrollments, actor.Organization.OrganizationId, actor.Actor.ActorId, activityId, cohortId);
}

public interface IEnrollmentCursorSigner
{
    string Sign(ReadOnlySpan<byte> payload);

    bool Verify(ReadOnlySpan<byte> payload, string tag);
}

public sealed class HmacEnrollmentCursorSigner : IEnrollmentCursorSigner
{
    private readonly byte[] _key;

    public HmacEnrollmentCursorSigner(byte[]? key = null)
    {
        _key = key ?? RandomNumberGenerator.GetBytes(32);
        if (_key.Length < 32)
        {
            throw new ArgumentException("Enrollment cursor keys must be at least 32 bytes.", nameof(key));
        }
    }

    public string Sign(ReadOnlySpan<byte> payload) =>
        Encode(HMACSHA256.HashData(_key, payload));

    public bool Verify(ReadOnlySpan<byte> payload, string tag)
    {
        if (!TryDecode(tag, out var expected))
        {
            return false;
        }

        var actual = HMACSHA256.HashData(_key, payload);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    internal static string Encode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal static bool TryDecode(string value, out byte[] bytes)
    {
        bytes = [];
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public static class EnrollmentListCursor
{
    public static string Issue(
        EnrollmentListCursorScope scope,
        DateTimeOffset updatedAt,
        Guid enrollmentId,
        IEnrollmentCursorSigner signer)
    {
        var payload = Payload(scope, updatedAt.UtcTicks, enrollmentId);
        return $"v1.{HmacEnrollmentCursorSigner.Encode(payload)}.{signer.Sign(payload)}";
    }

    public static bool TryOpen(
        string? cursor,
        EnrollmentListCursorScope expected,
        IEnrollmentCursorSigner signer,
        out DateTimeOffset? updatedAt,
        out Guid? enrollmentId)
    {
        updatedAt = null;
        enrollmentId = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        if (cursor.Length > EnrollmentPageBounds.MaximumCursorLength)
        {
            return false;
        }

        var parts = cursor.Split('.', 3);
        if (parts.Length != 3
            || !string.Equals(parts[0], "v1", StringComparison.Ordinal)
            || !HmacEnrollmentCursorSigner.TryDecode(parts[1], out var payload)
            || !signer.Verify(payload, parts[2]))
        {
            return false;
        }

        var fields = System.Text.Encoding.UTF8.GetString(payload).Split('|');
        if (fields.Length != 7
            || !string.Equals(fields[0], expected.QueryKind, StringComparison.Ordinal)
            || !Guid.TryParse(fields[1], out var organizationId)
            || organizationId != expected.OrganizationId
            || !Guid.TryParse(fields[2], out var actorId)
            || actorId != expected.ActorId
            || !Guid.TryParse(fields[3], out var activityId)
            || activityId != expected.ActivityId
            || !Guid.TryParse(fields[4], out var cohortId)
            || cohortId != expected.CohortId
            || !long.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParse(fields[6], out var id))
        {
            return false;
        }

        updatedAt = new DateTimeOffset(ticks, TimeSpan.Zero);
        enrollmentId = id;
        return true;
    }

    private static byte[] Payload(EnrollmentListCursorScope scope, long ticks, Guid enrollmentId) =>
        System.Text.Encoding.UTF8.GetBytes(string.Create(
            CultureInfo.InvariantCulture,
            $"{scope.QueryKind}|{scope.OrganizationId:D}|{scope.ActorId:D}|{scope.ActivityId:D}|{scope.CohortId:D}|{ticks}|{enrollmentId:D}"));
}
