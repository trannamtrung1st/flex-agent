using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class ProviderSafeInvocationContextSerializerTests
{
    [Fact]
    public void Serialization_includes_participant_transcript_and_excludes_ownership_identifiers()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var participantText = "What is the capital of the assessment question?";
        var context = new InvocationContext(
            ownership,
            new string('a', 64),
            new string('b', 64),
            [new ProtectedContentRef("sub:bound-v1", new string('c', 64))],
            [],
            [],
            [
                new VisibleTranscriptItemRef(
                    "msg.p.1",
                    TranscriptAuthorTypes.Participant,
                    "turn.1",
                    new ProtectedContentRef("msg:msg.p.1", new string('d', 64)),
                    participantText),
            ],
            [InvocationContextFactCategories.TranscriptItem, InvocationContextFactCategories.SubmissionRef]);

        var payload = ProviderSafeInvocationContextSerializer.Serialize("ainv.00000001", context);

        Assert.Contains(participantText, payload, StringComparison.Ordinal);
        Assert.Contains("ainv.00000001", payload, StringComparison.Ordinal);
        Assert.Contains("sub:bound-v1", payload, StringComparison.Ordinal);
        Assert.DoesNotContain(ownership.OrganizationId.ToString(), payload, StringComparison.Ordinal);
        Assert.DoesNotContain(ownership.SessionId.ToString(), payload, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("credential", payload, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ApprovedHttpsOriginTests
{
    [Fact]
    public void Canonical_origin_rejects_path_query_and_fragment()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApprovedHttpsOrigin.Canonicalize(new Uri("https://api.openai.com/v1/chat/completions")));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApprovedHttpsOrigin.Canonicalize(new Uri("https://api.openai.com/?x=1")));
        var canonical = ApprovedHttpsOrigin.Canonicalize(new Uri("https://api.openai.com/"));
        Assert.Equal("https://api.openai.com/", canonical.AbsoluteUri);
        Assert.Equal("https://api.openai.com", ApprovedHttpsOrigin.DigestSource(canonical));
    }
}
