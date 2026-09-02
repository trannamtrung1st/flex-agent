using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Configuration.Domain;

public static class ParticipantNoticeTypes
{
    public const string Instructions = "instructions";
    public const string Consent = "consent";
    public const string DataUse = "data_use";
}

public static class ParticipantNoticeProjectionCodes
{
    public const string Recorded = "participant_notice.recorded";
    public const string Unsupported = "participant_notice.unsupported";
    public const string DigestMismatch = "participant_notice.digest_mismatch";
    public const string InvalidField = "participant_notice.invalid_field";
}

public sealed record ParticipantNoticeProjection(
    Guid NoticeId,
    string NoticeType,
    string RequiredOutcome,
    string ProtectedContentRef,
    string ContentDigest);

public static class ParticipantNoticeProjectionParser
{
    private static readonly HashSet<string> KnownTypes =
    [
        ParticipantNoticeTypes.Instructions,
        ParticipantNoticeTypes.Consent,
        ParticipantNoticeTypes.DataUse,
    ];

    public static bool TryParse(
        ReadOnlyMemory<byte> canonicalUtf8,
        string declaredSourceDigest,
        out IReadOnlyList<ParticipantNoticeProjection> notices,
        out string? failureCode)
    {
        notices = [];
        failureCode = null;
        try
        {
            using var document = JsonDocument.Parse(canonicalUtf8);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                failureCode = ParticipantNoticeProjectionCodes.InvalidField;
                return false;
            }

            var recomputed = CanonicalJsonProcessor.CanonicalizeSha256Hex(
                canonicalUtf8.Span,
                new CanonicalJsonLimits(65_536, 64, 4_096, 4_096));
            if (!string.Equals(recomputed, declaredSourceDigest, StringComparison.Ordinal))
            {
                failureCode = ParticipantNoticeProjectionCodes.DigestMismatch;
                return false;
            }

            if (!document.RootElement.TryGetProperty("participant_notices", out var array)
                || array.ValueKind == JsonValueKind.Null)
            {
                notices = [];
                return true;
            }

            if (array.ValueKind != JsonValueKind.Array)
            {
                failureCode = ParticipantNoticeProjectionCodes.InvalidField;
                return false;
            }

            var parsed = new List<ParticipantNoticeProjection>();
            foreach (var item in array.EnumerateArray())
            {
                if (!TryRead(item, out var notice, out failureCode))
                {
                    return false;
                }

                parsed.Add(notice);
            }

            notices = parsed;
            return true;
        }
        catch (JsonException)
        {
            failureCode = ParticipantNoticeProjectionCodes.InvalidField;
            return false;
        }
        catch (CanonicalJsonException)
        {
            failureCode = ParticipantNoticeProjectionCodes.DigestMismatch;
            return false;
        }
    }

    private static bool TryRead(
        JsonElement item,
        out ParticipantNoticeProjection notice,
        out string? failureCode)
    {
        notice = null!;
        failureCode = ParticipantNoticeProjectionCodes.InvalidField;
        if (item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty("notice_id", out var idElement)
            || !Guid.TryParse(idElement.GetString(), out var noticeId)
            || noticeId == Guid.Empty
            || !item.TryGetProperty("notice_type", out var typeElement)
            || typeElement.GetString() is not { } noticeType
            || !KnownTypes.Contains(noticeType)
            || !item.TryGetProperty("required_outcome", out var outcomeElement)
            || outcomeElement.GetString() is not { } requiredOutcome
            || requiredOutcome != "affirmed"
            || !item.TryGetProperty("protected_content_ref", out var refElement)
            || string.IsNullOrWhiteSpace(refElement.GetString())
            || !item.TryGetProperty("content_digest", out var digestElement)
            || digestElement.GetString() is not { Length: 64 } digest
            || digest != digest.ToLowerInvariant())
        {
            return false;
        }

        notice = new ParticipantNoticeProjection(
            noticeId,
            noticeType,
            requiredOutcome,
            refElement.GetString()!,
            digest);
        failureCode = null;
        return true;
    }
}
