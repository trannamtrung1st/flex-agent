using System.Net;
using System.Text;
using FlexAgent.Api;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ApiProgram = FlexAgent.Api.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class ProductionSessionEventRuntimeTests
{
    [Fact]
    public async Task Production_events_route_is_not_the_synthetic_browser_path()
    {
        await using var factory = new WebApplicationFactory<ApiProgram>();
        var client = factory.CreateClient();
        var sessionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/sessions/{sessionId}/events");
        request.Headers.TryAddWithoutValidation("Last-Event-ID", "12");
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal("/sessions/" + sessionId.ToString("D") + "/events", request.RequestUri?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain("Hel", body, StringComparison.Ordinal);
        Assert.DoesNotContain("session.agent.fragment.v1", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stolen_cursor_without_actor_does_not_authorize_or_leak_events()
    {
        var harness = CreateHarness();
        await using var factory = harness.Factory;
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/sessions/{harness.SessionId:D}/events");
        request.Headers.TryAddWithoutValidation("Last-Event-ID", "4");
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, harness.Handler.AuthorizeCalls);
        Assert.Equal(0, harness.Handler.ReplayCalls);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain("secret-fragment", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Guessed_session_id_with_actor_does_not_leak_events()
    {
        var harness = CreateHarness();
        harness.Handler.PermitSessionId = harness.SessionId;
        await using var factory = harness.Factory;
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var guessed = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/sessions/{guessed:D}/events");
        request.Headers.TryAddWithoutValidation(SessionEventEndpointExtensions.TestActorHeaderName, harness.ActorId.ToString("D"));
        request.Headers.TryAddWithoutValidation("Last-Event-ID", "4");
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, harness.Handler.ReplayCalls);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain("secret-fragment", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reviewer_actor_does_not_receive_participant_fragments()
    {
        var harness = CreateHarness(reviewer: true);
        await using var factory = harness.Factory;
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/sessions/{harness.SessionId:D}/events");
        request.Headers.TryAddWithoutValidation(SessionEventEndpointExtensions.TestActorHeaderName, harness.ActorId.ToString("D"));
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, harness.Handler.ReplayCalls);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.DoesNotContain("secret-fragment", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authorized_participant_replays_committed_fragments_from_the_command()
    {
        var harness = CreateHarness();
        await using var factory = harness.Factory;
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/sessions/{harness.SessionId:D}/events");
        request.Headers.TryAddWithoutValidation(SessionEventEndpointExtensions.TestActorHeaderName, harness.ActorId.ToString("D"));
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var body = await ReadUntilReplayCompleteAsync(reader, cancellationToken);

        Assert.Contains("secret-fragment", body, StringComparison.Ordinal);
        Assert.Contains("session.agent.fragment.v1", body, StringComparison.Ordinal);
        Assert.Contains("id: 4", body, StringComparison.Ordinal);
        Assert.True(harness.Handler.AuthorizeCalls >= 1);
        Assert.True(harness.Handler.ReplayCalls >= 1);
        Assert.Equal(harness.ActorId, harness.Handler.LastCommand?.Actor.ActorId);
        Assert.Equal(harness.SessionId, harness.Handler.LastCommand?.UntrustedSessionId);
        Assert.Null(harness.Handler.LastCommand?.UntrustedLastEventId);
    }

    [Fact]
    public async Task Malformed_and_future_cursors_do_not_leak_earlier_text()
    {
        var harness = CreateHarness(replayOutcome: SessionEventReplayOutcomeCodes.Reconcile);
        await using var factory = harness.Factory;
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/sessions/{harness.SessionId:D}/events");
        request.Headers.TryAddWithoutValidation(SessionEventEndpointExtensions.TestActorHeaderName, harness.ActorId.ToString("D"));
        request.Headers.TryAddWithoutValidation("Last-Event-ID", "not-a-sequence");
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var body = await ReadUntilClosedAsync(reader, cancellationToken);

        Assert.Contains(": reconcile", body, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-fragment", body, StringComparison.Ordinal);
        Assert.Equal("not-a-sequence", harness.Handler.LastCommand?.UntrustedLastEventId);
        Assert.Equal(harness.ActorId, harness.Handler.LastCommand?.Actor.ActorId);
    }

    [Fact]
    public async Task Held_connection_completes_after_revocation_revalidation()
    {
        var harness = CreateHarness(
            revalidation: TimeSpan.FromMilliseconds(80),
            poll: TimeSpan.FromMilliseconds(40),
            heartbeat: TimeSpan.FromSeconds(30));
        harness.Handler.PermitUntilAuthorizeCall = 1;
        await using var factory = harness.Factory;
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/sessions/{harness.SessionId:D}/events");
        request.Headers.TryAddWithoutValidation(SessionEventEndpointExtensions.TestActorHeaderName, harness.ActorId.ToString("D"));
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var body = await ReadUntilClosedAsync(reader, cancellationToken);

        Assert.Contains("secret-fragment", body, StringComparison.Ordinal);
        Assert.True(harness.Handler.AuthorizeCalls >= 2, $"kernel calls: {harness.Handler.AuthorizeCalls}");
        Assert.Contains(": access-revoked", body, StringComparison.Ordinal);
    }

    private static Harness CreateHarness(
        bool reviewer = false,
        string replayOutcome = SessionEventReplayOutcomeCodes.Succeeded,
        TimeSpan? revalidation = null,
        TimeSpan? poll = null,
        TimeSpan? heartbeat = null)
    {
        var organizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var participantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var sessionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var actorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var directory = new MemoryTrustedInteractiveActorDirectory();
        directory.Register(new TrustedInteractiveActor(
            actorId,
            "synthetic.test_actor",
            organizationId,
            participantId,
            reviewer
                ? SessionEventSubscriptionRelationships.Reviewer
                : SessionEventSubscriptionRelationships.Participant));
        var handler = new FakeSubscribeHandler
        {
            PermitSessionId = sessionId,
            Result = replayOutcome == SessionEventReplayOutcomeCodes.Succeeded
                ? new AuthorizedSessionEventReplayResult(
                    true,
                    SessionEventReplayOutcomeCodes.Succeeded,
                    [
                        new AuthorizedSessionProjectionEvent(
                            AuthorizedSessionEventTypes.AgentFragment,
                            sessionId.ToString("D"),
                            "4",
                            "2026-08-17T00:00:00Z",
                            "Agent response fragment published.",
                            1,
                            "msg.agent.1",
                            "secret-fragment"),
                    ])
                : new AuthorizedSessionEventReplayResult(false, replayOutcome, []),
        };
        var factory = new WebApplicationFactory<ApiProgram>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISubscribeAuthorizedSessionEventsHandler>(handler);
                services.AddSingleton<ITrustedInteractiveActorDirectory>(directory);
                services.AddSingleton(new SessionEventSubscriptionOptions
                {
                    AuthorizationRevalidationInterval = revalidation ?? TimeSpan.FromSeconds(60),
                    PollInterval = poll ?? TimeSpan.FromSeconds(1),
                    HeartbeatInterval = heartbeat ?? TimeSpan.FromSeconds(15),
                });
            });
        });

        return new Harness(factory, sessionId, actorId, handler);
    }

    private static async Task<string> ReadUntilReplayCompleteAsync(StreamReader reader, CancellationToken cancellationToken)
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

    private static async Task<string> ReadUntilClosedAsync(StreamReader reader, CancellationToken cancellationToken)
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

    private sealed record Harness(
        WebApplicationFactory<ApiProgram> Factory,
        Guid SessionId,
        Guid ActorId,
        FakeSubscribeHandler Handler);

    private sealed class FakeSubscribeHandler : ISubscribeAuthorizedSessionEventsHandler
    {
        public int AuthorizeCalls { get; private set; }

        public int ReplayCalls { get; private set; }

        public Guid PermitSessionId { get; set; }

        public int? PermitUntilAuthorizeCall { get; set; }

        public SubscribeAuthorizedSessionEventsCommand? LastCommand { get; private set; }

        public required AuthorizedSessionEventReplayResult Result { get; set; }

        public Task<SessionEventSubscriptionAuthorization> AuthorizeAsync(
            SubscribeAuthorizedSessionEventsCommand command,
            CancellationToken cancellationToken = default)
        {
            AuthorizeCalls++;
            LastCommand = command;
            var permitted = command.UntrustedSessionId == PermitSessionId
                && command.Relationship == SessionEventSubscriptionRelationships.Participant
                && (PermitUntilAuthorizeCall is null || AuthorizeCalls <= PermitUntilAuthorizeCall);
            return Task.FromResult(new SessionEventSubscriptionAuthorization(permitted));
        }

        public Task<AuthorizedSessionEventReplayResult> ReplayAsync(
            SubscribeAuthorizedSessionEventsCommand command,
            CancellationToken cancellationToken = default)
        {
            ReplayCalls++;
            LastCommand = command;
            return Task.FromResult(Result);
        }
    }
}
