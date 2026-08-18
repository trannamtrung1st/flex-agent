using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Transport;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Microsoft.AspNetCore.Http.Features;

namespace FlexAgent.Api;

public static class SessionEventEndpointExtensions
{
    public const string TestActorHeaderName = "X-Flex-Test-Actor-Id";

    public const string TestHarnessKeyHeaderName = "X-Flex-Session-Events-Test-Key";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IEndpointRouteBuilder MapProductionSessionEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/sessions/{sessionId}/events", GetSessionEvents);
        return endpoints;
    }

    private static async Task GetSessionEvents(HttpContext context, string sessionId)
    {
        var identity = context.RequestServices.GetRequiredService<ISessionEventIdentityAdapter>();
        var handler = context.RequestServices.GetRequiredService<ISubscribeAuthorizedSessionEventsHandler>();
        var options = context.RequestServices.GetRequiredService<SessionEventSubscriptionOptions>();
        var cancellationToken = context.RequestAborted;

        var actor = await identity.TryAuthenticateAsync(context.Request, cancellationToken);
        if (actor is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(sessionId, out var untrustedSessionId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var lastEventId = context.Request.Headers["Last-Event-ID"].FirstOrDefault();
        var command = new SubscribeAuthorizedSessionEventsCommand(
            actor,
            untrustedSessionId,
            lastEventId);

        var authorization = await handler.AuthorizeAsync(command, cancellationToken);
        if (!authorization.IsPermitted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var replay = await handler.ReplayAsync(command, cancellationToken);
        if (!await WriteReplayOrCompleteAsync(context, replay, cancellationToken))
        {
            return;
        }

        var cursor = lastEventId;
        if (replay.Events.Count > 0)
        {
            cursor = replay.Events[^1].SessionSequence;
        }

        while (replay.HasMore)
        {
            command = command with { UntrustedLastEventId = cursor };
            replay = await handler.ReplayAsync(command, cancellationToken);
            if (!await WriteReplayOrCompleteAsync(context, replay, cancellationToken))
            {
                return;
            }

            if (replay.Events.Count > 0)
            {
                cursor = replay.Events[^1].SessionSequence;
            }
        }

        await context.Response.WriteAsync(": replay-complete\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        var nextHeartbeatAt = DateTimeOffset.UtcNow + options.HeartbeatInterval;
        var nextRevalidateAt = DateTimeOffset.UtcNow + options.AuthorizationRevalidationInterval;
        var nextPollAt = DateTimeOffset.UtcNow + options.PollInterval;
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var delay = MinPositive(
                nextHeartbeatAt - now,
                nextRevalidateAt - now,
                nextPollAt - now);
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            now = DateTimeOffset.UtcNow;
            if (now >= nextRevalidateAt)
            {
                var held = command with { UntrustedLastEventId = cursor };
                var reauthorization = await handler.AuthorizeAsync(held, cancellationToken);
                if (!reauthorization.IsPermitted)
                {
                    await context.Response.WriteAsync(": access-revoked\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                    return;
                }

                nextRevalidateAt = now + options.AuthorizationRevalidationInterval;
            }

            if (now >= nextPollAt)
            {
                while (true)
                {
                    command = command with { UntrustedLastEventId = cursor };
                    replay = await handler.ReplayAsync(command, cancellationToken);
                    if (!replay.Succeeded)
                    {
                        var comment = IsDenied(replay.OutcomeCode)
                            ? ": access-revoked\n\n"
                            : ": reconcile\n\n";
                        await context.Response.WriteAsync(comment, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                        return;
                    }

                    await WriteEventsAsync(context, replay.Events, cancellationToken);
                    if (replay.Events.Count > 0)
                    {
                        cursor = replay.Events[^1].SessionSequence;
                    }

                    if (!replay.HasMore)
                    {
                        break;
                    }
                }

                nextPollAt = now + options.PollInterval;
            }

            if (now >= nextHeartbeatAt)
            {
                await context.Response.WriteAsync(": keep-alive\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
                nextHeartbeatAt = now + options.HeartbeatInterval;
            }
        }
    }

    private static async Task<bool> WriteReplayOrCompleteAsync(
        HttpContext context,
        AuthorizedSessionEventReplayResult replay,
        CancellationToken cancellationToken)
    {
        if (replay.Succeeded)
        {
            await WriteEventsAsync(context, replay.Events, cancellationToken);
            return true;
        }

        var comment = IsDenied(replay.OutcomeCode) ? ": access-revoked\n\n" : ": reconcile\n\n";
        await context.Response.WriteAsync(comment, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
        return false;
    }

    private static bool IsDenied(string outcomeCode) =>
        outcomeCode is SessionEventReplayOutcomeCodes.Denied
            or SessionEventReplayOutcomeCodes.OwnershipMismatch;

    private static async Task WriteEventsAsync(
        HttpContext context,
        IReadOnlyList<AuthorizedSessionProjectionEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var evt in events)
        {
            var payload = new SseSessionEventV1(
                "v1",
                evt.EventType,
                evt.SessionId,
                evt.SessionSequence,
                evt.OccurredAt,
                new SseSessionEventPayloadV1(
                    evt.Summary,
                    evt.FragmentSequence,
                    evt.AgentMessageId,
                    evt.TextDelta,
                    AssembledContentDigest: evt.AssembledContentDigest,
                    FragmentCount: evt.FragmentCount));
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            await context.Response.WriteAsync($"id: {evt.SessionSequence}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        }

        if (events.Count > 0)
        {
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static TimeSpan MinPositive(params TimeSpan[] values)
    {
        var min = TimeSpan.MaxValue;
        foreach (var value in values)
        {
            if (value < min)
            {
                min = value;
            }
        }

        return min < TimeSpan.Zero ? TimeSpan.Zero : min;
    }
}
