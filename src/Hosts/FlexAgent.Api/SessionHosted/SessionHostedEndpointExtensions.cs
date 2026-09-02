using System.Globalization;
using System.Text.Json;
using FlexAgent.Contracts.Session;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class SessionHostedEndpointExtensions
{
    public static IEndpointRouteBuilder MapHostedSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/sessions");
        group.MapGet("/{sessionId:guid}", GetSnapshot);
        group.MapPost("/{sessionId:guid}/commands", SubmitCommand);
        return endpoints;
    }

    internal static async Task GetSnapshot(
        HttpContext context,
        Guid sessionId,
        IHostedSessionSnapshotQuery queries)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var telemetry = context.RequestServices.GetService<IHostedSessionTelemetry>();
        var identity = context.RequestServices.GetRequiredService<ISessionEventIdentityAdapter>();
        var actor = await identity.TryAuthenticateAsync(context.Request, context.RequestAborted);
        if (actor is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            telemetry?.RecordSnapshot("unauthenticated", StopwatchElapsed(started));
            return;
        }

        if (context.RequestServices.GetService<IHostedSessionSnapshotQuery>() is null)
        {
            telemetry?.RecordSnapshot("denied", StopwatchElapsed(started));
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, "session.denied");
            return;
        }

        var result = await queries.GetAsync(actor, sessionId, context.RequestAborted);
        if (!result.Found || result.Snapshot is null)
        {
            telemetry?.RecordSnapshot("denied", StopwatchElapsed(started));
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, "session.denied");
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        telemetry?.RecordSnapshot("loaded", StopwatchElapsed(started));
        await Results.Json(MapSnapshot(result.Snapshot)).ExecuteAsync(context);
    }

    internal static async Task SubmitCommand(
        HttpContext context,
        Guid sessionId,
        IHostedSessionCommandCoordinator coordinator,
        IAntiforgery antiforgery)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var telemetry = context.RequestServices.GetService<IHostedSessionTelemetry>();
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
        {
            telemetry?.RecordCommand("unknown", "csrf", StopwatchElapsed(started));
            return;
        }

        var identity = context.RequestServices.GetRequiredService<ISessionEventIdentityAdapter>();
        var actor = await identity.TryAuthenticateAsync(context.Request, context.RequestAborted);
        if (actor is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            telemetry?.RecordCommand("unknown", "unauthenticated", StopwatchElapsed(started));
            return;
        }

        using var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
        if (!TryReadEnvelope(document.RootElement, sessionId, out var envelope))
        {
            telemetry?.RecordCommand("unknown", "invalid", StopwatchElapsed(started));
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status400BadRequest, "session.command.invalid");
            return;
        }

        var outcome = await coordinator.SubmitAsync(
            actor,
            sessionId,
            envelope.CommandType,
            envelope.CommandId,
            envelope.IdempotencyKey,
            envelope.ExpectedSessionVersion,
            envelope.MessageText,
            envelope.TerminateReasonCode,
            context.RequestAborted);
        if (outcome is null)
        {
            telemetry?.RecordCommand(envelope.CommandType, "invalid", StopwatchElapsed(started));
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status400BadRequest, "session.command.invalid");
            return;
        }

        if (outcome.OutcomeCode == "session.denied")
        {
            telemetry?.RecordCommand(envelope.CommandType, "denied", StopwatchElapsed(started));
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, "session.denied");
            return;
        }

        telemetry?.RecordCommand(envelope.CommandType, outcome.OutcomeCategory, StopwatchElapsed(started));
        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = outcome.OutcomeCategory == "conflict"
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status200OK;
        await Results.Json(new SessionCommandOutcomeV1(
            "v1",
            outcome.Succeeded,
            outcome.OutcomeCategory,
            outcome.OutcomeCode,
            envelope.CommandId,
            envelope.CommandType,
            sessionId,
            outcome.PermittedRecoveryAction,
            outcome.PermittedActions,
            outcome.SessionVersion is null ? null : checked((int)outcome.SessionVersion.Value),
            outcome.SessionSequence?.ToString(CultureInfo.InvariantCulture),
            outcome.AcceptedMessageId)).ExecuteAsync(context);
    }

    internal static SessionSnapshotV1 MapSnapshot(HostedSessionSnapshot snapshot)
    {
        SessionTranscriptPageV1? transcript = null;
        SessionBoundSubmissionSummaryV1? submission = null;
        SessionAgentIdentityV1? agent = null;
        if (snapshot.ProjectionKind is HostedSessionProjectionKinds.Participant
            or HostedSessionProjectionKinds.Historical)
        {
            transcript = new SessionTranscriptPageV1(
                snapshot.Transcript.Select(item => new SessionSnapshotTranscriptItemV1(
                    item.ItemId,
                    item.Author,
                    item.Status,
                    item.SequenceStart,
                    item.SequenceEnd,
                    item.Content,
                    item.OccurredAt,
                    item.TurnId)).ToArray(),
                snapshot.OlderAvailable,
                snapshot.Transcript.Count == 0
                    ? null
                    : snapshot.Transcript.Min(item => long.Parse(item.SequenceStart, CultureInfo.InvariantCulture))
                        .ToString(CultureInfo.InvariantCulture),
                snapshot.Transcript.Count == 0
                    ? null
                    : snapshot.Transcript.Max(item => long.Parse(item.SequenceEnd, CultureInfo.InvariantCulture))
                        .ToString(CultureInfo.InvariantCulture));
            submission = new SessionBoundSubmissionSummaryV1("Bound Submission", snapshot.BoundSubmissionCount);
            if (snapshot.AgentDisplayName is not null)
            {
                agent = new SessionAgentIdentityV1(snapshot.AgentDisplayName);
            }
        }

        return new SessionSnapshotV1(
            "v1",
            snapshot.ProjectionKind,
            snapshot.SessionId,
            snapshot.LifecycleState,
            checked((int)snapshot.SessionVersion),
            snapshot.SessionSequence.ToString(CultureInfo.InvariantCulture),
            HostedSessionSnapshotProjector.FormatUtc(snapshot.AuthoritativeObservedAt),
            snapshot.PermittedActions,
            snapshot.RecoveryCategory,
            snapshot.CutoffSequence?.ToString(CultureInfo.InvariantCulture),
            agent,
            new SessionTimingProjectionV1("disabled", null, "none", null),
            submission,
            transcript,
            new SessionActivityProjectionV1(snapshot.ActivityWorkState, snapshot.ActivityTurnId, null));
    }

    private static bool TryReadEnvelope(JsonElement root, Guid routeSessionId, out HostedCommandEnvelope envelope)
    {
        envelope = default;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("schema_version", out var version)
            || version.GetString() != "v1"
            || !root.TryGetProperty("command_type", out var typeEl)
            || typeEl.GetString() is not { } commandType
            || !root.TryGetProperty("command_id", out var idEl)
            || idEl.GetString() is not { Length: >= 8 } commandId
            || !root.TryGetProperty("idempotency_key", out var idemEl)
            || idemEl.GetString() is not { Length: >= 8 } idempotency
            || !root.TryGetProperty("session_locator", out var locator)
            || !locator.TryGetProperty("session_id", out var locatorId)
            || locatorId.GetString() is not { } locatorSession
            || !Guid.TryParse(locatorSession, out var bodySession)
            || bodySession != routeSessionId
            || !root.TryGetProperty("expected_session_version", out var versionEl)
            || !versionEl.TryGetInt32(out var expectedVersion)
            || expectedVersion < 0)
        {
            return false;
        }

        string? messageText = null;
        string? reason = null;
        if (commandType == "session.message.send.v1")
        {
            if (!root.TryGetProperty("payload", out var payload)
                || !payload.TryGetProperty("message_text", out var text)
                || text.GetString() is not { Length: > 0 } message)
            {
                return false;
            }

            messageText = message;
        }
        else if (commandType == "session.terminate.v1")
        {
            if (!root.TryGetProperty("payload", out var payload)
                || !payload.TryGetProperty("reason_code", out var reasonEl)
                || reasonEl.GetString() is not { Length: > 0 } reasonCode)
            {
                return false;
            }

            reason = reasonCode;
        }

        envelope = new HostedCommandEnvelope(commandType, commandId, idempotency, expectedVersion, messageText, reason);
        return true;
    }

    private static TimeSpan StopwatchElapsed(long started) =>
        TimeSpan.FromSeconds((System.Diagnostics.Stopwatch.GetTimestamp() - started) / (double)System.Diagnostics.Stopwatch.Frequency);

    private readonly record struct HostedCommandEnvelope(
        string CommandType,
        string CommandId,
        string IdempotencyKey,
        int ExpectedSessionVersion,
        string? MessageText,
        string? TerminateReasonCode);
}
