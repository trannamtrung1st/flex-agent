using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Api;

public static partial class SubmissionEndpointExtensions
{
    internal static async Task GetMyWorkSubmission(
        HttpContext context,
        Guid enrollmentId,
        ISubmissionQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetMyWorkSubmissionAsync(actor, enrollmentId, context.RequestAborted);
        if (!result.Found || result.Value is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, result.OutcomeCode ?? SubmissionFailureCodes.NotFound);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(SubmissionResponseMapper.MapMyWork(result.Value)).ExecuteAsync(context);
    }

    internal static async Task GetIntake(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        ISubmissionQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetIntakeAsync(actor, enrollmentId, intakeId, context.RequestAborted);
        if (!result.Found || result.Value is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, result.OutcomeCode ?? SubmissionFailureCodes.NotFound);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(SubmissionResponseMapper.MapOutcome(result.Value)).ExecuteAsync(context);
    }

    internal static async Task GetAcceptedVersion(
        HttpContext context,
        Guid enrollmentId,
        Guid versionId,
        ISubmissionQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetAcceptedVersionAsync(actor, enrollmentId, versionId, context.RequestAborted);
        if (!result.Found || result.Value is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, result.OutcomeCode ?? SubmissionFailureCodes.NotFound);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(SubmissionResponseMapper.MapAcceptedVersion(result.Value)).ExecuteAsync(context);
    }

    internal static async Task GetItemPreview(
        HttpContext context,
        Guid enrollmentId,
        Guid versionId,
        Guid itemId,
        ISubmissionQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetAcceptedItemPreviewAsync(actor, enrollmentId, versionId, itemId, context.RequestAborted);
        if (!result.Found || result.Value is null)
        {
            var status = string.Equals(result.OutcomeCode, SubmissionFailureCodes.AuditUnavailable, StringComparison.Ordinal)
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status404NotFound;
            await EnrollmentEndpointExtensions.WriteError(context, status, result.OutcomeCode ?? SubmissionFailureCodes.NotFound);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(new ProtectedItemPreviewV2(
            "v2",
            result.Value.VersionId,
            result.Value.ItemId,
            result.Value.Category,
            result.Value.Filename,
            result.Value.ContentType,
            result.Value.Text)).ExecuteAsync(context);
    }

    internal static async Task GetItemDownload(
        HttpContext context,
        Guid enrollmentId,
        Guid versionId,
        Guid itemId,
        ISubmissionQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetAcceptedItemPreviewAsync(
            actor,
            enrollmentId,
            versionId,
            itemId,
            context.RequestAborted,
            SubmissionPermittedActions.DownloadItem);
        if (!result.Found || result.Value is null)
        {
            var status = string.Equals(result.OutcomeCode, SubmissionFailureCodes.AuditUnavailable, StringComparison.Ordinal)
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status404NotFound;
            await EnrollmentEndpointExtensions.WriteError(context, status, result.OutcomeCode ?? SubmissionFailureCodes.NotFound);
            return;
        }

        var filename = string.IsNullOrWhiteSpace(result.Value.Filename) ? "submission-item.txt" : result.Value.Filename;
        var safeName = filename
            .Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = result.Value.ContentType;
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{safeName}\"";
        await context.Response.WriteAsync(result.Value.Text, context.RequestAborted);
    }
}
