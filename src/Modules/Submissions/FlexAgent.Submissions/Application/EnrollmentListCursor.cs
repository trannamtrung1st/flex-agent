using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed record EnrollmentListCursorScope(
    string QueryKind,
    Guid OrganizationId,
    Guid ActorId,
    Guid ActivityId,
    Guid CohortId,
    string Prefix)
{
    public const string MyWork = "my-work";
    public const string Enrollments = "enrollments";
    public const string ParticipantOptions = "participant-options";

    public static EnrollmentListCursorScope ForMyWork(EnrollmentActorContext actor) =>
        new(MyWork, actor.Organization.OrganizationId, actor.Actor.ActorId, Guid.Empty, Guid.Empty, string.Empty);

    public static EnrollmentListCursorScope ForEnrollments(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId) =>
        new(Enrollments, actor.Organization.OrganizationId, actor.Actor.ActorId, activityId, cohortId, string.Empty);

    public static EnrollmentListCursorScope ForParticipantOptions(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        string? prefix) =>
        new(
            ParticipantOptions,
            actor.Organization.OrganizationId,
            actor.Actor.ActorId,
            activityId,
            cohortId,
            NormalizePrefix(prefix));

    public static string NormalizePrefix(string? prefix) =>
        string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim();
}

public sealed record EnrollmentCursorSigningKey(string KeyId, byte[] Material);

public static class EnrollmentCursorKeyResolver
{
    public const int MinimumKeyBytes = 32;
    public const int MaximumKeyIdLength = 32;

    public static bool IsValidKeyId(string keyId) =>
        !string.IsNullOrWhiteSpace(keyId)
        && keyId.Length <= MaximumKeyIdLength
        && keyId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    public static EnrollmentCursorSigningKey Materialize(string keyId, string secret)
    {
        if (!IsValidKeyId(keyId))
        {
            throw new ArgumentException("Enrollment cursor key IDs must be short ASCII tokens.", nameof(keyId));
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("Enrollment cursor secrets must not be empty.", nameof(secret));
        }

        var material = Encoding.UTF8.GetBytes(secret);
        if (material.Length < MinimumKeyBytes)
        {
            material = SHA256.HashData(material);
        }

        return new EnrollmentCursorSigningKey(keyId, material);
    }
}

public interface IEnrollmentCursorSigner
{
    string CurrentKeyId { get; }

    string Sign(ReadOnlySpan<byte> payload);

    bool Verify(ReadOnlySpan<byte> payload, string keyId, string tag);
}

public sealed class HmacEnrollmentCursorSigner : IEnrollmentCursorSigner
{
    private readonly EnrollmentCursorSigningKey _current;
    private readonly EnrollmentCursorSigningKey? _previous;

    public HmacEnrollmentCursorSigner(EnrollmentCursorSigningKey current, EnrollmentCursorSigningKey? previous = null)
    {
        if (!EnrollmentCursorKeyResolver.IsValidKeyId(current.KeyId) || current.Material.Length < EnrollmentCursorKeyResolver.MinimumKeyBytes)
        {
            throw new ArgumentException("Enrollment cursor keys must have a valid ID and at least 32 bytes.", nameof(current));
        }

        if (previous is not null
            && (!EnrollmentCursorKeyResolver.IsValidKeyId(previous.KeyId)
                || previous.Material.Length < EnrollmentCursorKeyResolver.MinimumKeyBytes
                || string.Equals(previous.KeyId, current.KeyId, StringComparison.Ordinal)))
        {
            throw new ArgumentException("The previous Enrollment cursor key must be a distinct valid key.", nameof(previous));
        }

        _current = current;
        _previous = previous;
        CurrentKeyId = current.KeyId;
    }

    public string CurrentKeyId { get; }

    public string Sign(ReadOnlySpan<byte> payload) =>
        Encode(HMACSHA256.HashData(_current.Material, payload));

    public bool Verify(ReadOnlySpan<byte> payload, string keyId, string tag)
    {
        var key = Resolve(keyId);
        if (key is null || !TryDecode(tag, out var expected))
        {
            return false;
        }

        var actual = HMACSHA256.HashData(key.Material, payload);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private EnrollmentCursorSigningKey? Resolve(string keyId)
    {
        if (string.Equals(keyId, _current.KeyId, StringComparison.Ordinal))
        {
            return _current;
        }

        return _previous is not null && string.Equals(keyId, _previous.KeyId, StringComparison.Ordinal)
            ? _previous
            : null;
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
        IEnrollmentCursorSigner signer) =>
        Format(scope, updatedAt.UtcTicks, enrollmentId, signer);

    public static string IssueAfterActor(
        EnrollmentListCursorScope scope,
        Guid afterActorId,
        IEnrollmentCursorSigner signer) =>
        Format(scope, 0, afterActorId, signer);

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

        var parts = cursor.Split('.', 4);
        if (parts.Length != 4
            || !string.Equals(parts[0], "v1", StringComparison.Ordinal)
            || !EnrollmentCursorKeyResolver.IsValidKeyId(parts[1])
            || !HmacEnrollmentCursorSigner.TryDecode(parts[2], out var payload)
            || !signer.Verify(payload, parts[1], parts[3]))
        {
            return false;
        }

        var fields = Encoding.UTF8.GetString(payload).Split('|');
        if (fields.Length != 8
            || !string.Equals(fields[0], expected.QueryKind, StringComparison.Ordinal)
            || !Guid.TryParse(fields[1], out var organizationId)
            || organizationId != expected.OrganizationId
            || !Guid.TryParse(fields[2], out var actorId)
            || actorId != expected.ActorId
            || !Guid.TryParse(fields[3], out var activityId)
            || activityId != expected.ActivityId
            || !Guid.TryParse(fields[4], out var cohortId)
            || cohortId != expected.CohortId
            || !HmacEnrollmentCursorSigner.TryDecode(fields[5], out var prefixBytes)
            || !string.Equals(Encoding.UTF8.GetString(prefixBytes), expected.Prefix, StringComparison.Ordinal)
            || !long.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParse(fields[7], out var id))
        {
            return false;
        }

        updatedAt = ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        enrollmentId = id;
        return true;
    }

    private static string Format(
        EnrollmentListCursorScope scope,
        long ticks,
        Guid id,
        IEnrollmentCursorSigner signer)
    {
        var payload = Payload(scope, ticks, id);
        return $"v1.{signer.CurrentKeyId}.{HmacEnrollmentCursorSigner.Encode(payload)}.{signer.Sign(payload)}";
    }

    private static byte[] Payload(EnrollmentListCursorScope scope, long ticks, Guid id) =>
        Encoding.UTF8.GetBytes(string.Create(
            CultureInfo.InvariantCulture,
            $"{scope.QueryKind}|{scope.OrganizationId:D}|{scope.ActorId:D}|{scope.ActivityId:D}|{scope.CohortId:D}|{HmacEnrollmentCursorSigner.Encode(Encoding.UTF8.GetBytes(scope.Prefix))}|{ticks}|{id:D}"));
}
