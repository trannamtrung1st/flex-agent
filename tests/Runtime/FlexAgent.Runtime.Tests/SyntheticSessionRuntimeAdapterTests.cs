using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FlexAgent.SyntheticBrowser;
using FlexAgent.SyntheticBrowser.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class SyntheticSessionRuntimeAdapterTests : IClassFixture<WebApplicationFactory<ApiProgram>>
{
    private const string HarnessApiKey = "test-harness-key";
    private const string SessionId = "sess.synthetic.001";
    private const string EnrollmentId = "enr.synthetic.001";
    private const string ActivityId = "act.synthetic.campaign-001";
    private readonly WebApplicationFactory<ApiProgram> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly string[] ForbiddenParticipantLeaks =
    [
        "adec.",
        "aout.",
        "tsrev.",
        "next_timer",
        "PT30S",
        "fire_at",
        "payload_ref",
        "decision_type",
        "hidden reasoning",
    ];

    public SyntheticSessionRuntimeAdapterTests(WebApplicationFactory<ApiProgram> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SyntheticBrowser:Enabled"] = "true",
                    ["SyntheticBrowser:HarnessApiKey"] = HarnessApiKey,
                });
            });
        });
    }

    [Fact]
    public async Task Active_session_sse_without_trusted_trigger_emits_no_agent_work()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(instanceId, cancellationToken);

        var events = await ReadSseAsync(participant, cancellationToken);

        Assert.Empty(events);
        Assert.DoesNotContain(events, evt => evt.EventType.StartsWith("session.agent.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Held_session_sse_emits_replay_complete_then_later_events_on_the_same_connection()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(instanceId, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/browser/sessions/{SessionId}/events");
        using var response = await participant.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var replay = await ReadSseUntilReplayCompleteAsync(reader, cancellationToken);
        Assert.Contains(": replay-complete", replay, StringComparison.Ordinal);

        var liveRead = ReadLiveSseEventsUntilAsync(
            reader,
            evt => evt.EventType == "session.agent.complete.v1",
            cancellationToken);
        await PostCommandAsync(
            participant,
            "session.send_message",
            "held-stream",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Hold the stream." });

        var liveEvents = await liveRead.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        Assert.Equal(
            [
                ("session.agent.work.v1", "queued"),
                ("session.agent.work.v1", "working"),
                ("session.agent.fragment.v1", null),
                ("session.agent.complete.v1", null),
            ],
            liveEvents.Select(evt => (evt.EventType, evt.Payload.WorkState)).ToArray());
        Assert.Contains(liveEvents, evt => evt.Payload.TextDelta == "Thank you for your response. ");
    }

    [Fact]
    public async Task Held_session_sse_delivers_timer_triggered_turn_on_the_same_connection()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionTimerVisibleWork);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/browser/sessions/{SessionId}/events");
        using var response = await participant.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var replay = await ReadSseUntilReplayCompleteAsync(reader, cancellationToken);
        Assert.Contains(": replay-complete", replay, StringComparison.Ordinal);

        var liveRead = ReadLiveSseEventsUntilAsync(
            reader,
            evt => evt.EventType == "session.agent.complete.v1",
            cancellationToken);
        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionTimerVisibleWork))
            .EnsureSuccessStatusCode();

        var liveEvents = await liveRead.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        Assert.Equal(
            [
                ("session.agent.work.v1", "queued"),
                ("session.agent.work.v1", "working"),
                ("session.agent.fragment.v1", null),
                ("session.agent.complete.v1", null),
            ],
            liveEvents.Select(evt => (evt.EventType, evt.Payload.WorkState)).ToArray());
        Assert.Contains(liveEvents, evt => evt.Payload.TextDelta == "Checking in on your progress. ");
        Assert.DoesNotContain(
            liveEvents,
            evt => evt.Payload.Summary.Contains("timer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Held_session_sse_completes_when_scenario_access_is_revoked()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(instanceId, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/browser/sessions/{SessionId}/events");
        using var response = await participant.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        _ = await ReadSseUntilReplayCompleteAsync(reader, cancellationToken);
        (await RevokeScenarioAccessAsync(instanceId, cancellationToken)).EnsureSuccessStatusCode();

        var remaining = await ReadSseUntilClosedAsync(reader, cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        Assert.DoesNotContain("session.agent.", remaining, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Participant_message_admits_queued_working_fragment_and_complete()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(instanceId, cancellationToken);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "reply",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Ready." });

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);

        Assert.Contains(events, evt => evt.EventType == "session.agent.work.v1" && evt.Payload.WorkState == "queued");
        Assert.Contains(events, evt => evt.EventType == "session.agent.work.v1" && evt.Payload.WorkState == "working");
        Assert.Contains(events, evt => evt.EventType == "session.agent.fragment.v1" && evt.Payload.TextDelta == "Thank you for your response. ");
        Assert.Contains(events, evt => evt.EventType == "session.agent.complete.v1");

        var session = await participant.GetFromJsonAsync<SessionDto>(
            $"/browser/sessions/{SessionId}",
            JsonOptions,
            cancellationToken);
        Assert.Contains(session!.Transcript, item => item.Role == "participant" && item.Content == "Ready.");
        Assert.Contains(session.Transcript, item => item.Role == "agent" && item.Content.Contains("Thank you", StringComparison.Ordinal));
        Assert.DoesNotContain(session.Transcript, item => item.Content.Contains("no_action", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Participant_no_action_resolves_without_agent_message_or_error_copy()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionParticipantNoAction);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "silent",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Waiting." });

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.complete.v1");
        Assert.Contains(
            events,
            evt => evt.EventType == "session.agent.work.v1"
                && evt.Payload.WorkState == "resolved"
                && evt.Payload.ResolutionCategory == "no_action"
                && evt.Payload.ShowPersistentTurnStatus == true);
        Assert.All(events, evt => Assert.NotEqual("no_action", evt.Payload.Summary));

        var session = await participant.GetFromJsonAsync<SessionDto>(
            $"/browser/sessions/{SessionId}",
            JsonOptions,
            cancellationToken);
        Assert.Contains(session!.Transcript, item => item.Role == "participant");
        Assert.DoesNotContain(session.Transcript, item => item.Role == "agent");
    }

    [Fact]
    public async Task Opening_emits_agent_work_on_attempt_start_without_participant_message()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionOpeningClosing);

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.Contains(events, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.Contains(events, evt => evt.Payload.TextDelta == "Welcome. Let us begin. ");

        var session = await participant.GetFromJsonAsync<SessionDto>(
            $"/browser/sessions/{SessionId}",
            JsonOptions,
            cancellationToken);
        Assert.DoesNotContain(session!.Transcript, item => item.Role == "participant");
        Assert.Contains(session.Transcript, item => item.Role == "agent");
    }

    [Fact]
    public async Task Timer_visible_work_requires_harness_trigger_and_adds_no_participant_message()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionTimerVisibleWork);

        Assert.Empty(await ReadSseAsync(participant, cancellationToken));

        var fire = await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionTimerVisibleWork);
        fire.EnsureSuccessStatusCode();

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.Contains(events, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.Contains(events, evt => evt.Payload.TextDelta == "Checking in on your progress. ");
        Assert.DoesNotContain(
            events,
            evt => evt.Payload.Summary.Contains("timer", StringComparison.OrdinalIgnoreCase)
                || evt.Payload.Summary.Contains("delay", StringComparison.OrdinalIgnoreCase));

        var session = await participant.GetFromJsonAsync<SessionDto>(
            $"/browser/sessions/{SessionId}",
            JsonOptions,
            cancellationToken);
        Assert.DoesNotContain(session!.Transcript, item => item.Role == "participant");
        Assert.Contains(session.Transcript, item => item.Role == "agent");
    }

    [Fact]
    public async Task Timer_no_action_resolves_without_agent_or_participant_message()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionTimerNoAction);

        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionTimerNoAction))
            .EnsureSuccessStatusCode();

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.Contains(
            events,
            evt => evt.Payload.WorkState == "resolved" && evt.Payload.ResolutionCategory == "no_action");

        var session = await participant.GetFromJsonAsync<SessionDto>(
            $"/browser/sessions/{SessionId}",
            JsonOptions,
            cancellationToken);
        Assert.DoesNotContain(session!.Transcript, item => item.Role is "agent" or "participant");
    }

    [Fact]
    public async Task Duplicate_timer_revision_admits_exactly_one_work_stream()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionDuplicateConcurrentRevision);

        var first = FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionDuplicateConcurrentRevision);
        var second = FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionDuplicateConcurrentRevision);
        var results = await Task.WhenAll(first, second);
        Assert.All(results, result => result.EnsureSuccessStatusCode());

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Equal(1, events.Count(evt => evt.EventType == "session.agent.complete.v1"));
        Assert.Equal(1, events.Count(evt => evt.EventType == "session.agent.fragment.v1"));
    }

    [Fact]
    public async Task Rejected_decision_is_suppressed_failure_not_no_action_or_provider_failure()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionRejectedDecision);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "reject",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Hello." });

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.Contains(events, evt => evt.Payload.ResolutionCategory == "suppressed_failure");
        Assert.DoesNotContain(events, evt => evt.Payload.ResolutionCategory == "no_action");
        Assert.DoesNotContain(events, evt => evt.Payload.ResolutionCategory == "execution_failure");
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.All(
            events,
            evt => Assert.DoesNotContain("provider", evt.Payload.Summary, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Execution_failure_uses_execution_failure_category()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionExecutionFailure);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "exec-fail",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Hello." });

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.Payload.ResolutionCategory == "execution_failure");
        Assert.DoesNotContain(events, evt => evt.Payload.ResolutionCategory == "no_action");
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.fragment.v1");
    }

    [Fact]
    public async Task Accepted_effect_failure_is_suppressed_not_no_action()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionAcceptedEffectFailure);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "effect-fail",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Hello." });

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.Payload.ResolutionCategory == "suppressed_failure");
        Assert.DoesNotContain(events, evt => evt.Payload.ResolutionCategory == "no_action");
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.fragment.v1");
    }

    [Fact]
    public async Task Pause_and_resume_emit_state_changes_without_timer_copy()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionPauseResume);

        await PostCommandAsync(participant, "session.pause", "pause", cancellationToken, sessionVersion: 1);
        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionPauseResume))
            .EnsureSuccessStatusCode();

        var paused = await ReadSseAsync(participant, cancellationToken);
        Assert.DoesNotContain(paused, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.Contains(paused, evt => evt.EventType == "session.state.changed.v1" && evt.Payload.Summary.Contains("paused", StringComparison.OrdinalIgnoreCase));

        await PostCommandAsync(participant, "session.resume", "resume", cancellationToken, sessionVersion: 2);
        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionPauseResume))
            .EnsureSuccessStatusCode();

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.Contains(events, evt => evt.EventType == "session.state.changed.v1" && evt.Payload.Summary.Contains("resumed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(events, evt => evt.EventType == "session.agent.fragment.v1");
        Assert.DoesNotContain(
            events,
            evt => evt.Payload.Summary.Contains("timer", StringComparison.OrdinalIgnoreCase)
                || evt.Payload.Summary.Contains("revision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reconnect_replays_from_cursor_without_regenerating_work()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionReconnect);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "reconnect",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Again." });

        var first = await ReadSseAsync(participant, cancellationToken);
        Assert.NotEmpty(first);
        var cursor = first[0].SessionSequence;
        var replay = await ReadSseAsync(participant, cancellationToken, cursor);
        Assert.Equal(first.Count - 1, replay.Count);
        Assert.All(replay, evt => Assert.True(long.Parse(evt.SessionSequence) > long.Parse(cursor)));

        var fullReplay = await ReadSseAsync(participant, cancellationToken);
        Assert.Equal(first.Select(evt => evt.SessionSequence), fullReplay.Select(evt => evt.SessionSequence));
    }

    [Fact]
    public async Task Cutoff_emits_terminal_and_rejects_post_complete_publication()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionCutoff);

        await PostCommandAsync(participant, "session.complete", "cut", cancellationToken, sessionVersion: 1);

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.EventType == "session.terminal.v1");
        Assert.DoesNotContain(events, evt => evt.EventType == "session.agent.fragment.v1");

        var send = await PostCommandRawAsync(
            participant,
            "session.send_message",
            "after-cut",
            cancellationToken,
            resourceId: SessionId,
            expectedVersion: 2,
            payload: new Dictionary<string, string> { ["message_text"] = "Too late." });
        Assert.Equal(HttpStatusCode.Forbidden, send.StatusCode);

        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionCutoff))
            .EnsureSuccessStatusCode();
        var after = await ReadSseAsync(participant, cancellationToken);
        Assert.Equal(events.Select(evt => evt.SessionSequence), after.Select(evt => evt.SessionSequence));
    }

    [Fact]
    public async Task Closing_publishes_agent_message_then_terminals()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionOpeningClosing);

        var session = await participant.GetFromJsonAsync<SessionDto>(
            $"/browser/sessions/{SessionId}",
            JsonOptions,
            cancellationToken);
        await PostCommandAsync(
            participant,
            "session.complete",
            "close",
            cancellationToken,
            sessionVersion: session!.SessionVersion);

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.Payload.TextDelta == "This Session is now complete. ");
        Assert.Contains(events, evt => evt.EventType == "session.terminal.v1");
        var closingIndex = events.FindIndex(evt => evt.Payload.TextDelta == "This Session is now complete. ");
        var terminalIndex = events.FindIndex(evt => evt.EventType == "session.terminal.v1");
        Assert.True(closingIndex >= 0 && terminalIndex > closingIndex);
    }

    [Fact]
    public async Task Closing_from_paused_publishes_agent_message_then_terminals()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionOpeningClosing);

        await PostCommandAsync(participant, "session.pause", "pause-before-close", cancellationToken, sessionVersion: 1);
        await PostCommandAsync(participant, "session.complete", "close-paused", cancellationToken, sessionVersion: 2);

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.Payload.TextDelta == "This Session is now complete. ");
        Assert.Contains(events, evt => evt.EventType == "session.terminal.v1");
        var closingIndex = events.FindIndex(evt => evt.Payload.TextDelta == "This Session is now complete. ");
        var terminalIndex = events.FindIndex(evt => evt.EventType == "session.terminal.v1");
        Assert.True(closingIndex >= 0 && terminalIndex > closingIndex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Harness_timer_fire_rejects_missing_or_blank_revision_id(string? revisionId)
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionTimerVisibleWork);

        var rejected = await FireTimerRawAsync(
            instanceId,
            revisionId,
            cancellationToken,
            SyntheticScenarioIds.SessionTimerVisibleWork);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Empty(await ReadSseAsync(participant, cancellationToken));

        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionTimerVisibleWork))
            .EnsureSuccessStatusCode();
        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.EventType == "session.agent.fragment.v1");
    }

    [Fact]
    public async Task Padded_and_trimmed_revision_ids_are_the_same_timer_identity()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionTimerVisibleWork);

        (await FireTimerAsync(instanceId, " 1 ", cancellationToken, SyntheticScenarioIds.SessionTimerVisibleWork))
            .EnsureSuccessStatusCode();
        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionTimerVisibleWork))
            .EnsureSuccessStatusCode();

        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Equal(1, events.Count(evt => evt.EventType == "session.agent.complete.v1"));
        Assert.Equal(1, events.Count(evt => evt.EventType == "session.agent.fragment.v1"));
    }

    [Theory]
    [InlineData(SyntheticScenarioIds.SessionTimerReplacementAccepted)]
    [InlineData(SyntheticScenarioIds.SessionTimerReplacementRejected)]
    [InlineData(SyntheticScenarioIds.SessionTimerReplacementOmitted)]
    public async Task Timer_replacement_scenarios_keep_schedule_internals_out_of_browser_events(
        string scenarioId)
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(instanceId, cancellationToken, scenarioId);

        await PostCommandAsync(
            participant,
            "session.send_message",
            "replace",
            cancellationToken,
            sessionVersion: 1,
            payload: new Dictionary<string, string> { ["message_text"] = "Continue." });

        (await FireTimerAsync(instanceId, "2", cancellationToken, scenarioId)).EnsureSuccessStatusCode();

        var events = await ReadSseAsync(participant, cancellationToken);
        AssertNoParticipantLeaks(events);
        Assert.Contains(events, evt => evt.EventType == "session.agent.complete.v1");
        Assert.DoesNotContain(
            events,
            evt => evt.Payload.Summary.Contains("accepted", StringComparison.OrdinalIgnoreCase)
                && evt.Payload.Summary.Contains("timer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Default_timer_stays_silent_until_trusted_fire()
    {
        var instanceId = NewInstanceId();
        var cancellationToken = TestContext.Current.CancellationToken;
        var participant = await PrepareActiveSessionAsync(
            instanceId,
            cancellationToken,
            SyntheticScenarioIds.SessionDefaultTimer);

        Assert.Empty(await ReadSseAsync(participant, cancellationToken));
        (await FireTimerAsync(instanceId, "1", cancellationToken, SyntheticScenarioIds.SessionDefaultTimer))
            .EnsureSuccessStatusCode();
        var events = await ReadSseAsync(participant, cancellationToken);
        Assert.Contains(events, evt => evt.EventType == "session.agent.fragment.v1");
    }

    [Fact]
    public async Task Harness_timer_fire_requires_api_key()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await client.PostAsJsonAsync(
            "/browser/harness/session-triggers",
            new
            {
                scenario_id = SyntheticScenarioIds.SessionTimerVisibleWork,
                scenario_instance_id = NewInstanceId(),
                trigger_type = "timer.due",
                revision_id = "1",
            },
            JsonOptions,
            cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> PrepareActiveSessionAsync(
        string instanceId,
        CancellationToken cancellationToken,
        string scenarioId = SyntheticScenarioIds.CampaignFullJourney)
    {
        var admin = await CreateAuthenticatedClientAsync(SyntheticActorStages.Administrator, scenarioId, instanceId);
        await PostCommandAsync(admin, "activity.save_draft", "prep-save", cancellationToken, activityVersion: 1);
        await PostCommandAsync(admin, "activity.activate_cohort", "prep-activate", cancellationToken, activityVersion: 2);
        await PostCommandAsync(admin, "enrollment.assign", "prep-enroll", cancellationToken, activityVersion: 3);

        var participant = await CreateAuthenticatedClientAsync(SyntheticActorStages.Participant, scenarioId, instanceId);
        await PostCommandAsync(
            participant,
            "submission.submit_text",
            "prep-submit",
            cancellationToken,
            payload: new Dictionary<string, string> { ["submission_text"] = "Prep answer." });
        await PostCommandAsync(participant, "attempt.start", "prep-start", cancellationToken);
        return participant;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string actorStage, string scenarioId, string instanceId)
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var grant = await CreateGrantAsync(client, scenarioId, actorStage, instanceId, cancellationToken);
        var exchange = await client.PostAsJsonAsync("/browser/auth/exchange", new { grant_token = grant }, JsonOptions, cancellationToken);
        exchange.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<string> CreateGrantAsync(
        HttpClient client,
        string scenarioId,
        string actorStage,
        string scenarioInstanceId,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/scenario-grants")
        {
            Content = JsonContent.Create(new
            {
                scenario_id = scenarioId,
                actor_stage = actorStage,
                scenario_instance_id = scenarioInstanceId,
            }, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);
        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<GrantDto>(JsonOptions, cancellationToken);
        return body!.GrantToken;
    }

    private async Task<HttpResponseMessage> FireTimerAsync(
        string instanceId,
        string revisionId,
        CancellationToken cancellationToken,
        string scenarioId)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/session-triggers")
        {
            Content = JsonContent.Create(new
            {
                scenario_id = scenarioId,
                scenario_instance_id = instanceId,
                trigger_type = "timer.due",
                revision_id = revisionId,
            }, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> FireTimerRawAsync(
        string instanceId,
        string? revisionId,
        CancellationToken cancellationToken,
        string scenarioId)
    {
        var client = _factory.CreateClient();
        var payload = new Dictionary<string, string?>
        {
            ["scenario_id"] = scenarioId,
            ["scenario_instance_id"] = instanceId,
            ["trigger_type"] = "timer.due",
            ["revision_id"] = revisionId,
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/session-triggers")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> RevokeScenarioAccessAsync(
        string instanceId,
        CancellationToken cancellationToken,
        string scenarioId = SyntheticScenarioIds.CampaignFullJourney)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/browser/harness/scenario-instances/revoke-access")
        {
            Content = JsonContent.Create(new
            {
                scenario_id = scenarioId,
                scenario_instance_id = instanceId,
            }, options: JsonOptions),
        };
        request.Headers.Add(SyntheticBrowserEndpointExtensions.HarnessApiKeyHeaderName, HarnessApiKey);
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<List<SseDto>> ReadSseAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string? lastEventId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/browser/sessions/{SessionId}/events");
        if (!string.IsNullOrEmpty(lastEventId))
        {
            request.Headers.TryAddWithoutValidation("Last-Event-ID", lastEventId);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var body = await ReadSseUntilReplayCompleteAsync(reader, cancellationToken);
        var events = new List<SseDto>();
        foreach (var block in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var dataLine = block.Split('\n').FirstOrDefault(line => line.StartsWith("data: ", StringComparison.Ordinal));
            if (dataLine is null)
            {
                continue;
            }

            var evt = JsonSerializer.Deserialize<SseDto>(dataLine["data: ".Length..], JsonOptions);
            if (evt is not null)
            {
                events.Add(evt);
            }
        }

        return events;
    }

    private static async Task<string> ReadSseUntilReplayCompleteAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var body = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return body.ToString();
            }

            body.Append(line);
            body.Append('\n');
            if (line.Equals(": replay-complete", StringComparison.Ordinal))
            {
                return body.ToString();
            }
        }
    }

    private static async Task<string> ReadSseUntilClosedAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var body = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return body.ToString();
            }

            body.Append(line);
            body.Append('\n');
        }
    }

    private static async Task<List<SseDto>> ReadLiveSseEventsUntilAsync(
        StreamReader reader,
        Func<SseDto, bool> isTerminal,
        CancellationToken cancellationToken)
    {
        var events = new List<SseDto>();
        while (true)
        {
            var evt = await ReadNextSseEventAsync(reader, cancellationToken);
            events.Add(evt);
            if (isTerminal(evt))
            {
                return events;
            }
        }
    }

    private static async Task<SseDto> ReadNextSseEventAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var block = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            Assert.NotNull(line);
            if (line.Length == 0)
            {
                if (block.Length == 0)
                {
                    continue;
                }

                var dataLine = block.ToString()
                    .Split('\n')
                    .FirstOrDefault(candidate => candidate.StartsWith("data: ", StringComparison.Ordinal));
                Assert.NotNull(dataLine);
                var evt = JsonSerializer.Deserialize<SseDto>(dataLine["data: ".Length..], JsonOptions);
                Assert.NotNull(evt);
                return evt;
            }

            if (line.StartsWith(':'))
            {
                continue;
            }

            block.Append(line);
            block.Append('\n');
        }
    }

    private static void AssertNoParticipantLeaks(IEnumerable<SseDto> events)
    {
        var payload = JsonSerializer.Serialize(events, JsonOptions);
        foreach (var leak in ForbiddenParticipantLeaks)
        {
            Assert.DoesNotContain(leak, payload, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task PostCommandAsync(
        HttpClient client,
        string commandType,
        string idempotencyKey,
        CancellationToken cancellationToken,
        int? activityVersion = null,
        int? sessionVersion = null,
        IReadOnlyDictionary<string, string>? payload = null)
    {
        var response = await PostCommandRawAsync(
            client,
            commandType,
            idempotencyKey,
            cancellationToken,
            resourceId: commandType switch
            {
                "activity.save_draft" or "activity.activate_cohort" or "enrollment.assign" => ActivityId,
                "submission.submit_text" or "attempt.start" => EnrollmentId,
                _ => SessionId,
            },
            expectedVersion: commandType switch
            {
                "activity.save_draft" or "activity.activate_cohort" or "enrollment.assign" => activityVersion,
                "session.send_message" or "session.pause" or "session.resume" or "session.complete" => sessionVersion,
                _ => null,
            },
            payload: payload ?? (commandType == "enrollment.assign"
                ? new Dictionary<string, string> { ["participant_id"] = "part.synthetic.001" }
                : null));
        response.EnsureSuccessStatusCode();
    }

    private static Task<HttpResponseMessage> PostCommandRawAsync(
        HttpClient client,
        string commandType,
        string idempotencyKey,
        CancellationToken cancellationToken,
        string? resourceId = null,
        int? expectedVersion = null,
        IReadOnlyDictionary<string, string>? payload = null) =>
        client.PostAsJsonAsync("/browser/commands", new
        {
            schema_version = "v1",
            command_id = Guid.NewGuid().ToString("N"),
            idempotency_key = idempotencyKey,
            command_type = commandType,
            resource_id = resourceId,
            expected_version = expectedVersion,
            payload,
        }, JsonOptions, cancellationToken);

    private static string NewInstanceId() => Guid.NewGuid().ToString("N");

    private sealed record GrantDto(string GrantToken);
    private sealed record SessionDto(int SessionVersion, IReadOnlyList<TranscriptDto> Transcript);
    private sealed record TranscriptDto(string Role, string Content);
    private sealed record SseDto(string EventType, string SessionSequence, SsePayloadDto Payload);
    private sealed record SsePayloadDto(
        string Summary,
        string? TextDelta,
        string? WorkState,
        string? ResolutionCategory,
        bool? ShowPersistentTurnStatus);
}
