using FlexAgent.Sessions.Application;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class HostedSessionCommandEnvelopeValidatorTests
{
    private static readonly string FixtureDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "contracts",
        "fixtures",
        "schema",
        "v1",
        "session",
        "command-envelope");

    [Theory]
    [InlineData("invalid-pause-with-message-payload.json")]
    [InlineData("invalid-unknown-field.json")]
    public async Task Canonical_invalid_fixtures_are_rejected(string fileName)
    {
        var utf8 = await File.ReadAllBytesAsync(
            Path.Combine(FixtureDirectory, fileName),
            TestContext.Current.CancellationToken);

        Assert.False(HostedSessionCommandEnvelopeValidator.IsCanonicalSchemaValid(utf8));
    }

    [Fact]
    public async Task Canonical_valid_pause_fixture_is_accepted()
    {
        var utf8 = await File.ReadAllBytesAsync(
            Path.Combine(FixtureDirectory, "valid-pause.json"),
            TestContext.Current.CancellationToken);

        Assert.True(HostedSessionCommandEnvelopeValidator.IsCanonicalSchemaValid(utf8));
    }

    [Fact]
    public void Http_pause_with_extra_payload_is_rejected()
    {
        var sessionId = Guid.Parse("55555555-5555-4555-8555-555555555555");
        using var document = System.Text.Json.JsonDocument.Parse(
            """
            {
              "schema_version": "v1",
              "command_type": "session.pause.v1",
              "command_id": "cmd.synthetic.0099",
              "idempotency_key": "idem-synthetic-0099",
              "session_locator": { "session_id": "55555555-5555-4555-8555-555555555555" },
              "expected_session_version": 3,
              "payload": { "message_text": "extra" }
            }
            """);

        Assert.False(HostedSessionCommandEnvelopeValidator.TryRead(
            document.RootElement,
            sessionId,
            out _));
    }

    [Fact]
    public void Http_command_id_shorter_than_stable_id_is_rejected()
    {
        var sessionId = Guid.Parse("55555555-5555-4555-8555-555555555555");
        using var document = System.Text.Json.JsonDocument.Parse(
            $$"""
            {
              "schema_version": "v1",
              "command_type": "session.pause.v1",
              "command_id": "cmd.1",
              "idempotency_key": "idem-synthetic-0099",
              "session_locator": { "session_id": "{{sessionId:D}}" },
              "expected_session_version": 3,
              "payload": {}
            }
            """);

        Assert.False(HostedSessionCommandEnvelopeValidator.TryRead(
            document.RootElement,
            sessionId,
            out _));
    }

    [Fact]
    public void Http_pause_with_committed_uuid_locator_is_canonically_valid()
    {
        var sessionId = Guid.Parse("55555555-5555-4555-8555-555555555555");
        var utf8 = System.Text.Encoding.UTF8.GetBytes(
            $$"""
            {
              "schema_version": "v1",
              "command_type": "session.pause.v1",
              "command_id": "cmd.synthetic.0099",
              "idempotency_key": "idem-synthetic-0099",
              "session_locator": { "session_id": "{{sessionId:D}}" },
              "expected_session_version": 3,
              "payload": { "reason_code": "administrator_pause" }
            }
            """);

        Assert.True(HostedSessionCommandEnvelopeValidator.IsCanonicalSchemaValid(utf8));
        using var document = System.Text.Json.JsonDocument.Parse(utf8);
        Assert.True(HostedSessionCommandEnvelopeValidator.TryRead(document.RootElement, sessionId, out var envelope));
        Assert.Equal("administrator_pause", envelope.PauseReasonCode);
    }

    [Fact]
    public void Http_pause_without_reason_code_is_rejected()
    {
        var sessionId = Guid.Parse("55555555-5555-4555-8555-555555555555");
        using var document = System.Text.Json.JsonDocument.Parse(
            $$"""
            {
              "schema_version": "v1",
              "command_type": "session.pause.v1",
              "command_id": "cmd.synthetic.0099",
              "idempotency_key": "idem-synthetic-0099",
              "session_locator": { "session_id": "{{sessionId:D}}" },
              "expected_session_version": 3,
              "payload": {}
            }
            """);

        Assert.False(HostedSessionCommandEnvelopeValidator.TryRead(
            document.RootElement,
            sessionId,
            out _));
    }

    [Fact]
    public void Http_terminate_reads_reason_code()
    {
        var sessionId = Guid.Parse("55555555-5555-4555-8555-555555555555");
        using var document = System.Text.Json.JsonDocument.Parse(
            $$"""
            {
              "schema_version": "v1",
              "command_type": "session.terminate.v1",
              "command_id": "cmd.synthetic.0100",
              "idempotency_key": "idem-synthetic-0100",
              "session_locator": { "session_id": "{{sessionId:D}}" },
              "expected_session_version": 4,
              "payload": { "reason_code": "administrator_stop" }
            }
            """);

        Assert.True(HostedSessionCommandEnvelopeValidator.TryRead(
            document.RootElement,
            sessionId,
            out var envelope));
        Assert.Equal("administrator_stop", envelope.TerminateReasonCode);
    }
}
