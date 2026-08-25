using System.Text;
using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static partial class SubmissionEndpointExtensions
{
    internal static Task BeginIntake(
        HttpContext context,
        Guid enrollmentId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<BeginIntakeCommandV2>(context);
            if (!BeginIntakeRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Begin,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"));
            return await coordinator.BeginAsync(new BeginIntakeCommand(
                actor,
                enrollmentId,
                command.IdempotencyKey,
                digest));
        });

    internal static Task CompleteItem(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<CompleteIntakeItemCommandV2>(
                context,
                EnrollmentHttpLimits.MaximumSubmissionItemBodyBytes);
            if (!CompleteIntakeItemRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var content = Encoding.UTF8.GetBytes(command.Content);
            var digest = MaterialContentValidator.Sha256Hex(content);
            var trusted = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.CompleteItem,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                command.ExpectedRevision.ToString(),
                digest);
            return await coordinator.CompleteItemAsync(new CompleteIntakeItemCommand(
                actor,
                enrollmentId,
                intakeId,
                Guid.Empty,
                command.Category,
                command.Filename,
                command.DeclaredMimeType,
                content,
                digest,
                command.ExpectedRevision,
                command.IdempotencyKey,
                trusted));
        });

    internal static Task CancelIntake(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<IntakeRevisionCommandV2>(context);
            if (!IntakeRevisionRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Cancel,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                command.ExpectedRevision.ToString());
            return await coordinator.CancelAsync(new CancelIntakeCommand(
                actor,
                enrollmentId,
                intakeId,
                command.ExpectedRevision,
                command.IdempotencyKey,
                digest));
        });

    internal static Task FinalizeIntake(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<IntakeRevisionCommandV2>(context);
            if (!IntakeRevisionRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Finalize,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                command.ExpectedRevision.ToString());
            return await coordinator.FinalizeAsync(new FinalizeIntakeCommand(
                actor,
                enrollmentId,
                intakeId,
                command.ExpectedRevision,
                command.IdempotencyKey,
                digest));
        });

    internal static async Task MutateAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        Func<EnrollmentActorContext, Task<IntakeMutationOutcome?>> action)
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
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status400BadRequest, "invalid_request");
            return;
        }

        var status = outcome.Succeeded
            ? StatusCodes.Status200OK
            : outcome.OutcomeCode switch
            {
                SubmissionFailureCodes.Unauthorized
                    or SubmissionFailureCodes.NotFound
                    or SubmissionFailureCodes.EnrollmentUnavailable => StatusCodes.Status404NotFound,
                SubmissionFailureCodes.AuditUnavailable
                    or SubmissionFailureCodes.StorageUnavailable
                    or SubmissionFailureCodes.PolicyUnavailable => StatusCodes.Status503ServiceUnavailable,
                SubmissionFailureCodes.InvalidCategory
                    or SubmissionFailureCodes.InvalidEncoding
                    or SubmissionFailureCodes.InvalidContentType
                    or SubmissionFailureCodes.Oversized
                    or SubmissionFailureCodes.TooManyItems
                    or SubmissionFailureCodes.AggregateOversized => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status409Conflict,
            };
        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(SubmissionResponseMapper.MapOutcome(outcome), statusCode: status).ExecuteAsync(context);
    }
}
