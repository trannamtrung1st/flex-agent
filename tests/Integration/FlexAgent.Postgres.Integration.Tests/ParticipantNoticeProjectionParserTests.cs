using System.Text;
using FlexAgent.CanonicalJson;
using FlexAgent.Configuration.Domain;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class ParticipantNoticeProjectionParserTests
{
    [Fact]
    public void Missing_participant_notices_is_an_empty_verified_set()
    {
        var (utf8, digest) = Canonical("""{"schema_version":"v1"}""");
        Assert.True(ParticipantNoticeProjectionParser.TryParse(utf8, digest, out var notices, out var failure));
        Assert.Null(failure);
        Assert.Empty(notices);
    }

    [Fact]
    public void Typed_notices_are_bound_to_the_source_digest()
    {
        var noticeId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
        var (utf8, digest) = Canonical(
            $$"""
            {"participant_notices":[{"content_digest":"{{new string('b', 64)}}","notice_id":"{{noticeId:D}}","notice_type":"instructions","protected_content_ref":"notice:instructions","required_outcome":"affirmed"}]}
            """);
        Assert.True(ParticipantNoticeProjectionParser.TryParse(utf8, digest, out var notices, out var failure));
        Assert.Null(failure);
        var notice = Assert.Single(notices);
        Assert.Equal(noticeId, notice.NoticeId);
        Assert.Equal(ParticipantNoticeTypes.Instructions, notice.NoticeType);
        Assert.Equal("affirmed", notice.RequiredOutcome);
        Assert.Equal("notice:instructions", notice.ProtectedContentRef);
    }

    [Fact]
    public void Invalid_notice_array_fails_closed()
    {
        var (utf8, digest) = Canonical("""{"participant_notices":{}}""");
        Assert.False(ParticipantNoticeProjectionParser.TryParse(utf8, digest, out _, out var failure));
        Assert.Equal(ParticipantNoticeProjectionCodes.InvalidField, failure);
    }

    [Fact]
    public void Declared_digest_mismatch_fails_closed()
    {
        var (utf8, _) = Canonical("""{"participant_notices":[]}""");
        Assert.False(ParticipantNoticeProjectionParser.TryParse(utf8, new string('a', 64), out _, out var failure));
        Assert.Equal(ParticipantNoticeProjectionCodes.DigestMismatch, failure);
    }

    private static (ReadOnlyMemory<byte> Utf8, string Digest) Canonical(string json)
    {
        var utf8 = Encoding.UTF8.GetBytes(json);
        var digest = CanonicalJsonProcessor.CanonicalizeSha256Hex(
            utf8,
            new CanonicalJsonLimits(65_536, 64, 4_096, 4_096));
        return (utf8, digest);
    }
}
