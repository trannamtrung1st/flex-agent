using FlexAgent.Submissions.Application;

namespace FlexAgent.Submissions.Tests;

public sealed class FrozenAttemptTimingDocumentsTests
{
    private static readonly DateTimeOffset HardEnd = DateTimeOffset.Parse("2026-09-30T17:00:00Z");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"reconstruction":"timed"}""")]
    [InlineData("""{"reconstruction":"something-unknown"}""")]
    [InlineData("""{"reconstruction":"timed","budget_seconds":3600,"warnings":[]}""")]
    [InlineData("""{"reconstruction":"timed","budget_seconds":null,"warnings":[{"code":"approaching","remaining_seconds":900},{"code":"imminent","remaining_seconds":300}],"hard_end_at_utc":"2026-09-30T17:00:00Z"}""")]
    [InlineData("""{"reconstruction":"timed","budget_seconds":3600,"warnings":[{"code":"approaching","remaining_seconds":900},{"code":"imminent","remaining_seconds":300}],"hard_end_at_utc":"not-a-timestamp"}""")]
    public void Invalid_documents_fail_positive_validation(string? documentJson)
    {
        Assert.False(FrozenAttemptTimingDocuments.TryValidateAuthoritative(documentJson, out _));
        Assert.False(FrozenAttemptTimingCaptureResult.FromDocument(documentJson).Succeeded);
    }

    [Fact]
    public void Timed_document_with_required_fields_is_authoritative()
    {
        var document = FrozenAttemptTimingDocuments.ComposeAuthoritativeDocument(
            "timed",
            3600,
            [("approaching", 900), ("imminent", 300)],
            HardEnd);

        Assert.True(FrozenAttemptTimingDocuments.TryValidateAuthoritative(document, out var normalized));
        Assert.Equal(document, normalized);
        Assert.True(FrozenAttemptTimingCaptureResult.FromDocument(document).Succeeded);
    }

    [Fact]
    public void Unbounded_document_with_hard_end_is_authoritative()
    {
        var document = FrozenAttemptTimingDocuments.ComposeAuthoritativeDocument(
            "unbounded",
            null,
            [],
            HardEnd);

        Assert.True(FrozenAttemptTimingDocuments.TryValidateAuthoritative(document, out _));
        Assert.True(FrozenAttemptTimingCaptureResult.FromDocument(document).Succeeded);
    }
}
