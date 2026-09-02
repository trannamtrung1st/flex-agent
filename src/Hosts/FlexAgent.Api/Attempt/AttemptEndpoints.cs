using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static partial class SubmissionEndpointExtensions
{
    public static IEndpointRouteBuilder MapAttemptEndpoints(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints.ServiceProvider.GetService<IAttemptReadinessQuery>() is null)
        {
            return endpoints;
        }

        var group = endpoints.MapGroup("/v2/assessment");
        group.MapGet("/my-work/{enrollmentId:guid}/attempt", GetAttemptReadiness);
        group.MapPost("/my-work/{enrollmentId:guid}/attempt/acknowledgments", AcknowledgeAttemptNotice);
        group.MapPost("/my-work/{enrollmentId:guid}/attempt/start", StartAttempt);
        group.MapPost("/my-work/{enrollmentId:guid}/attempt/reconcile", ReconcileAttempt);
        return endpoints;
    }

    internal static async Task GetAttemptReadiness(
        HttpContext context,
        Guid enrollmentId,
        IAttemptReadinessQuery queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetAsync(actor, enrollmentId, context.RequestAborted);
        var telemetry = context.RequestServices.GetRequiredService<IEnrollmentTelemetry>();
        telemetry.RecordMutation(
            AttemptOperationKinds.Readiness,
            EnrollmentTelemetryLabels.ClassifyMutation(result.Found && result.Value is not null, result.OutcomeCode ?? AttemptFailureCodes.Denied),
            TimeSpan.Zero);
        if (!result.Found || result.Value is null)
        {
            await EnrollmentEndpointExtensions.WriteError(
                context,
                StatusCodes.Status404NotFound,
                result.OutcomeCode ?? AttemptFailureCodes.Denied);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(AttemptResponseMapper.MapReadiness(result.Value)).ExecuteAsync(context);
    }

    internal static Task AcknowledgeAttemptNotice(
        HttpContext context,
        Guid enrollmentId,
        IAttemptAcknowledgmentCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAcknowledgmentAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<AcknowledgeAttemptNoticeCommandV2>(context);
            if (!AttemptRequestValidators.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = AcknowledgmentCommandDigest.Compute(
                actor.Organization.OrganizationId,
                enrollmentId,
                actor.Actor.ActorId,
                command.NoticeId,
                command.SourceVersionId,
                command.Outcome);
            return await coordinator.RecordAsync(
                new AcknowledgeAttemptNoticeCommand(
                    actor,
                    enrollmentId,
                    command.NoticeId,
                    command.SourceVersionId,
                    command.Outcome,
                    command.IdempotencyKey,
                    digest),
                context.RequestAborted);
        });

    internal static Task StartAttempt(
        HttpContext context,
        Guid enrollmentId,
        IAttemptStartCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateStartAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<StartAttemptCommandV2>(context);
            if (!AttemptRequestValidators.IsValid(body))
            {
                return null;
            }

            var command = body!;
            return await coordinator.StartAsync(
                new StartAttemptCommand(actor, enrollmentId, command.IdempotencyKey, command.TrustedCommandDigest),
                context.RequestAborted);
        });

    internal static Task ReconcileAttempt(
        HttpContext context,
        Guid enrollmentId,
        IAttemptStartCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateStartAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<StartAttemptCommandV2>(context);
            if (!AttemptRequestValidators.IsValid(body))
            {
                return null;
            }

            var command = body!;
            return await coordinator.ReconcileAsync(
                new StartAttemptCommand(actor, enrollmentId, command.IdempotencyKey, command.TrustedCommandDigest),
                context.RequestAborted);
        });

    private static async Task MutateAcknowledgmentAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        Func<EnrollmentActorContext, Task<AcknowledgmentMutationOutcome?>> action)
    {
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Mutation);
        if (actor is null)
        {
            return;
        }

        var outcome = await action(actor);
        if (outcome is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status400BadRequest, AttemptFailureCodes.InvalidField);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = outcome.Succeeded
            ? StatusCodes.Status200OK
            : MapAttemptStatus(outcome.OutcomeCode);
        context.RequestServices.GetRequiredService<IEnrollmentTelemetry>().RecordMutation(
            AttemptOperationKinds.Acknowledge,
            EnrollmentTelemetryLabels.ClassifyMutation(outcome.Succeeded, outcome.OutcomeCode),
            TimeSpan.Zero);
        await Results.Json(AttemptResponseMapper.MapAcknowledgment(outcome)).ExecuteAsync(context);
    }

    private static async Task MutateStartAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        Func<EnrollmentActorContext, Task<StartAttemptOutcome?>> action)
    {
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Mutation);
        if (actor is null)
        {
            return;
        }

        var outcome = await action(actor);
        if (outcome is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status400BadRequest, AttemptFailureCodes.InvalidField);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = outcome.Succeeded
            ? StatusCodes.Status200OK
            : MapAttemptStatus(outcome.OutcomeCode);
        context.RequestServices.GetRequiredService<IEnrollmentTelemetry>().RecordMutation(
            context.Request.Path.Value?.Contains("/reconcile", StringComparison.Ordinal) == true
                ? AttemptOperationKinds.Reconcile
                : AttemptOperationKinds.Start,
            EnrollmentTelemetryLabels.ClassifyMutation(outcome.Succeeded, outcome.OutcomeCode),
            TimeSpan.Zero);
        await Results.Json(AttemptResponseMapper.MapStart(outcome)).ExecuteAsync(context);
    }

    private static int MapAttemptStatus(string outcomeCode) =>
        outcomeCode switch
        {
            AttemptFailureCodes.InvalidField => StatusCodes.Status400BadRequest,
            AttemptFailureCodes.Denied => StatusCodes.Status404NotFound,
            AttemptFailureCodes.AuditUnavailable or AttemptFailureCodes.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status409Conflict,
        };
}

internal static class AttemptRequestValidators
{
    public static bool IsValid(AcknowledgeAttemptNoticeCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && body.NoticeId != Guid.Empty
        && body.SourceVersionId != Guid.Empty
        && body.Outcome is "affirmed" or "declined" or "withdrawn"
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null;

    public static bool IsValid(StartAttemptCommandV2? body) =>
        body is not null
        && string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
        && EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is null
        && body.TrustedCommandDigest.Length == 64
        && body.TrustedCommandDigest.All(static ch => char.IsAsciiHexDigitLower(ch));
}

internal static class AttemptResponseMapper
{
    public static MyWorkAttemptReadinessV2 MapReadiness(AttemptReadinessProjection projection) =>
        new(
            "v2",
            projection.EnrollmentId,
            projection.ReadinessState,
            projection.NextOrdinal,
            projection.RemainingEntitlement,
            projection.EntitlementSource,
            projection.BaselineAttemptLimit,
            projection.ActiveAttemptId,
            projection.ActiveSessionId,
            projection.StartCommandDigest,
            projection.BoundVersionCandidates.Select(version => new AcceptedVersionSummaryV2(
                version.VersionId,
                version.VersionNumber,
                EnrollmentEndpointExtensions.FormatUtc(version.AcceptedAtUtc)!,
                version.ItemCount)).ToArray(),
            projection.History.Select(item => new AttemptHistoryItemV2(
                item.AttemptId,
                item.Ordinal,
                item.Status,
                item.Consumed,
                item.SessionId,
                EnrollmentEndpointExtensions.FormatUtc(item.StartedAtUtc)!,
                EnrollmentEndpointExtensions.FormatUtc(item.TerminalAtUtc),
                item.TerminalReasonCategory)).ToArray(),
            projection.RequiredNotices.Select(notice => new AttemptNoticeV2(
                notice.NoticeId,
                notice.NoticeType,
                notice.RequiredOutcome,
                notice.ProtectedContentRef,
                notice.SourceVersionId,
                notice.ContentDigest,
                notice.SourceId,
                notice.CurrentOutcome)).ToArray(),
            projection.PermittedActions);

    public static AcknowledgmentMutationOutcomeV2 MapAcknowledgment(AcknowledgmentMutationOutcome outcome) =>
        new("v2", outcome.Succeeded, outcome.OutcomeCode, outcome.RecordId, outcome.Outcome);

    public static StartAttemptOutcomeV2 MapStart(StartAttemptOutcome outcome) =>
        new(
            "v2",
            outcome.Succeeded,
            outcome.OutcomeCode,
            outcome.ReadinessState,
            outcome.AttemptId,
            outcome.Ordinal,
            outcome.SessionId,
            outcome.RemainingEntitlement,
            outcome.PermittedActions);
}
