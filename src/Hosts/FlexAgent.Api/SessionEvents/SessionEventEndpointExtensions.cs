using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Session;
using FlexAgent.Contracts.Transport;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
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
        endpoints.MapGet("/v1/sessions/{sessionId}/events", GetHostedSessionEvents);
        return endpoints;
    }

    private static Task GetHostedSessionEvents(HttpContext context, string sessionId) =>
        StreamSessionEventsAsync(context, sessionId, hosted: true);

    private static Task GetSessionEvents(HttpContext context, string sessionId) =>
        StreamSessionEventsAsync(context, sessionId, hosted: false);

    private static async Task StreamSessionEventsAsync(HttpContext context, string sessionId, bool hosted)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var telemetry = context.RequestServices.GetService<IHostedSessionTelemetry>();
        var identity = context.RequestServices.GetRequiredService<ISessionEventIdentityAdapter>();
        var handler = context.RequestServices.GetRequiredService<ISubscribeAuthorizedSessionEventsHandler>();
        var options = context.RequestServices.GetRequiredService<SessionEventSubscriptionOptions>();
        var cancellationToken = context.RequestAborted;

        var actor = await identity.TryAuthenticateAsync(context.Request, cancellationToken, advanceActivity: true);
        if (actor is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            telemetry?.RecordSubscribe("unauthenticated", SubscribeElapsed(started));
            return;
        }

        if (!Guid.TryParse(sessionId, out var untrustedSessionId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            telemetry?.RecordSubscribe("denied", SubscribeElapsed(started));
            return;
        }

        var lastEventId = context.Request.Headers["Last-Event-ID"].FirstOrDefault();
        var command = new SubscribeAuthorizedSessionEventsCommand(
            actor,
            untrustedSessionId,
            lastEventId,
            UseHostedProjection: hosted);

        var authorization = await handler.AuthorizeAsync(command, cancellationToken);
        if (!authorization.IsPermitted
            || !HasRequiredAuthenticationStrength(context, authorization.Relationship)
            || !MatchesBoundOrganization(context, authorization.OrganizationId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            telemetry?.RecordSubscribe("denied", SubscribeElapsed(started));
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var cursor = lastEventId;
        var replay = await handler.ReplayAsync(command, cancellationToken);
        if (!await WriteReplayOrCompleteAsync(context, replay, hosted, sessionId, cursor, cancellationToken))
        {
            return;
        }

        if (replay.Events.Count > 0)
        {
            cursor = HostedEventId(replay.Events[^1], hosted);
        }

        while (replay.HasMore)
        {
            command = command with { UntrustedLastEventId = cursor };
            replay = await handler.ReplayAsync(command, cancellationToken);
            if (!await WriteReplayOrCompleteAsync(context, replay, hosted, sessionId, cursor, cancellationToken))
            {
                return;
            }

            if (replay.Events.Count > 0)
            {
                cursor = HostedEventId(replay.Events[^1], hosted);
            }
        }

        await context.Response.WriteAsync(": replay-complete\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
        telemetry?.RecordSubscribe("opened", SubscribeElapsed(started));

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
                var heldActor = await identity.TryAuthenticateAsync(
                    context.Request,
                    cancellationToken,
                    advanceActivity: false);
                var held = command with { UntrustedLastEventId = cursor };
                var reauthorization = heldActor is null
                    ? new SessionEventSubscriptionAuthorization(false)
                    : await handler.AuthorizeAsync(held with { Actor = heldActor }, cancellationToken);
                if (!reauthorization.IsPermitted
                    || !HasRequiredAuthenticationStrength(context, reauthorization.Relationship)
                    || !MatchesBoundOrganization(context, reauthorization.OrganizationId))
                {
                    await WriteHostedTerminalSignalAsync(
                        context,
                        sessionId,
                        hosted,
                        HostedSessionEventTypes.AccessChanged,
                        "Session access changed.",
                        recoveryCategory: "sign_in",
                        accessState: "revoked",
                        HostedStreamCursors.Parse(cursor) + 1,
                        cancellationToken);
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
                        await WriteHostedTerminalSignalAsync(
                            context,
                            sessionId,
                            hosted,
                            IsDenied(replay.OutcomeCode)
                                ? HostedSessionEventTypes.AccessChanged
                                : HostedSessionEventTypes.ReconcileRequired,
                            IsDenied(replay.OutcomeCode)
                                ? "Session access changed."
                                : "Session snapshot reconciliation required.",
                            IsDenied(replay.OutcomeCode) ? null : "reconcile_snapshot",
                            IsDenied(replay.OutcomeCode) ? "revoked" : null,
                            HostedStreamCursors.Parse(cursor) + 1,
                            cancellationToken);
                        var comment = IsDenied(replay.OutcomeCode)
                            ? ": access-revoked\n\n"
                            : ": reconcile\n\n";
                        await context.Response.WriteAsync(comment, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                        return;
                    }

                    await WriteEventsAsync(context, replay.Events, hosted, cancellationToken);
                    if (replay.Events.Count > 0)
                    {
                        cursor = HostedEventId(replay.Events[^1], hosted);
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
        bool hosted,
        string sessionId,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (replay.Succeeded)
        {
            await WriteEventsAsync(context, replay.Events, hosted, cancellationToken);
            return true;
        }

        await WriteHostedTerminalSignalAsync(
            context,
            sessionId,
            hosted,
            IsDenied(replay.OutcomeCode)
                ? HostedSessionEventTypes.AccessChanged
                : HostedSessionEventTypes.ReconcileRequired,
            IsDenied(replay.OutcomeCode)
                ? "Session access changed."
                : "Session snapshot reconciliation required.",
            IsDenied(replay.OutcomeCode) ? null : "reconcile_snapshot",
            IsDenied(replay.OutcomeCode) ? "revoked" : null,
            HostedStreamCursors.Parse(cursor) + 1,
            cancellationToken);

        var comment = IsDenied(replay.OutcomeCode) ? ": access-revoked\n\n" : ": reconcile\n\n";
        await context.Response.WriteAsync(comment, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
        return false;
    }

    private static async Task WriteHostedTerminalSignalAsync(
        HttpContext context,
        string sessionId,
        bool hosted,
        string eventType,
        string summary,
        string? recoveryCategory,
        string? accessState,
        long streamCursor,
        CancellationToken cancellationToken)
    {
        if (!hosted || streamCursor < 1)
        {
            return;
        }

        var sequence = Math.Max(1L, streamCursor / HostedStreamCursors.SlotsPerSequence);
        var evt = new AuthorizedSessionProjectionEvent(
            eventType,
            sessionId,
            sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            summary,
            RecoveryCategory: recoveryCategory,
            AccessState: accessState,
            StreamCursor: streamCursor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await WriteEventsAsync(context, [evt], hosted: true, cancellationToken);
    }

    private static bool MatchesBoundOrganization(HttpContext context, Guid? organizationId)
    {
        if (context.Items[nameof(AuthenticatedApplicationSession)] is not AuthenticatedApplicationSession session)
        {
            return true;
        }

        return organizationId is not null && organizationId == session.OrganizationId;
    }

    private static bool HasRequiredAuthenticationStrength(HttpContext context, string? relationship)
    {
        var options = context.RequestServices.GetService<HumanAuthenticationHostOptions>();
        if (options is null || !AuthenticationStrengthEvaluator.RequiresMfa(relationship, AuthorizationActions.SubscribeSessionEvents))
        {
            return true;
        }

        var session = context.Items[nameof(AuthenticatedApplicationSession)] as AuthenticatedApplicationSession;
        if (session is null)
        {
            return true;
        }

        return AuthenticationStrengthEvaluator.Evaluate(
            session.Strength,
            relationship,
            AuthorizationActions.SubscribeSessionEvents,
            options.AcceptedAcr,
            options.AcceptedAmr) is null;
    }

    private static bool IsDenied(string outcomeCode) =>
        outcomeCode is SessionEventReplayOutcomeCodes.Denied
            or SessionEventReplayOutcomeCodes.OwnershipMismatch;

    private static async Task WriteEventsAsync(
        HttpContext context,
        IReadOnlyList<AuthorizedSessionProjectionEvent> events,
        bool hosted,
        CancellationToken cancellationToken)
    {
        foreach (var evt in events)
        {
            var json = hosted ? SerializeHosted(evt) : SerializeCompatibility(evt);
            await context.Response.WriteAsync($"id: {HostedEventId(evt, hosted)}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        }

        if (events.Count > 0)
        {
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static string SerializeCompatibility(AuthorizedSessionProjectionEvent evt) =>
        JsonSerializer.Serialize(
            new SseSessionEventV1(
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
                    FragmentCount: evt.FragmentCount)),
            JsonOptions);

    private static string SerializeHosted(AuthorizedSessionProjectionEvent evt)
    {
        var hostedType = evt.EventType switch
        {
            AuthorizedSessionEventTypes.AgentFragment => HostedSessionEventTypes.AgentFragment,
            AuthorizedSessionEventTypes.AgentComplete => HostedSessionEventTypes.AgentComplete,
            _ => evt.EventType,
        };
        if (!Guid.TryParse(evt.SessionId, out var sessionId))
        {
            sessionId = Guid.Empty;
        }

        return JsonSerializer.Serialize(
            new SessionHostedEventEnvelopeV1(
                "v1",
                hostedType,
                sessionId,
                evt.SessionSequence,
                checked((int)Math.Max(0, evt.SessionVersion)),
                evt.OccurredAt,
                new SessionHostedEventPayloadV1(
                    evt.Summary,
                    AgentMessageId: evt.AgentMessageId,
                    FragmentSequence: evt.FragmentSequence,
                    TextDelta: evt.TextDelta,
                    AssembledContentDigest: evt.AssembledContentDigest,
                    FragmentCount: evt.FragmentCount,
                    WorkState: evt.WorkState,
                    ResolutionCategory: evt.ResolutionCategory,
                    TurnId: evt.TurnId,
                    MessageId: evt.MessageId,
                    MessageText: evt.MessageText,
                    LifecycleState: evt.LifecycleState,
                    RecoveryCategory: evt.RecoveryCategory,
                    AccessState: evt.AccessState,
                    RemainingSeconds: evt.RemainingSeconds,
                    WarningCode: evt.WarningCode,
                    CutoffSequence: evt.CutoffSequence,
                    ItemStatus: evt.ItemStatus),
                evt.StreamCursor),
            JsonOptions);
    }

    private static string HostedEventId(AuthorizedSessionProjectionEvent evt, bool hosted) =>
        hosted && !string.IsNullOrWhiteSpace(evt.StreamCursor)
            ? evt.StreamCursor
            : evt.SessionSequence;

    private static TimeSpan SubscribeElapsed(long started) =>
        TimeSpan.FromSeconds((System.Diagnostics.Stopwatch.GetTimestamp() - started) / (double)System.Diagnostics.Stopwatch.Frequency);

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
