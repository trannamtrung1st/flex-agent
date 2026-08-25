using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class EnrollmentTimingEndpointExtensions
{
    public static IEndpointRouteBuilder MapEnrollmentTimingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v2/assessment");
        group.MapGet(
            "/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/timing",
            GetEnrollmentTiming);
        group.MapPost(
            "/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/accommodations",
            GrantAccommodation);
        group.MapPost(
            "/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/accommodations/{accommodationId:guid}/decide",
            DecideAccommodation);
        group.MapPost(
            "/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/accommodations/{accommodationId:guid}/revoke",
            RevokeAccommodation);
        group.MapGet("/my-work/{enrollmentId:guid}/timing", GetMyWorkTiming);
        return endpoints;
    }

    private static async Task GetEnrollmentTiming(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        IEnrollmentTimingQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetEnrollmentTimingAsync(actor, activityId, cohortId, enrollmentId, context.RequestAborted);
        await EnrollmentEndpointExtensions.WriteQuery(context, result, EnrollmentTimingResponseMapper.MapEnrollmentTiming);
    }

    private static async Task GetMyWorkTiming(
        HttpContext context,
        Guid enrollmentId,
        IEnrollmentTimingQueryService queries)
    {
        var actor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, EnrollmentRequestSurfaces.Read);
        if (actor is null)
        {
            return;
        }

        var result = await queries.GetMyWorkTimingAsync(actor, enrollmentId, context.RequestAborted);
        await EnrollmentEndpointExtensions.WriteQuery(context, result, EnrollmentTimingResponseMapper.MapMyWorkTiming);
    }

    private static Task GrantAccommodation(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        IAccommodationCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<GrantAccommodationCommandV2>(context);
            if (!GrantAccommodationRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Grant,
                actor.Organization.OrganizationId,
                activityId,
                cohortId,
                enrollmentId,
                null,
                command.Dimension,
                command.RequestedValue,
                command.ReasonCategory,
                command.FairnessException,
                command.ExpectedRevision,
                command.ExpiresAtUtc);
            return await coordinator.GrantAsync(
                new GrantAccommodationCommand(
                    actor,
                    activityId,
                    cohortId,
                    enrollmentId,
                    command.Dimension,
                    command.RequestedValue,
                    command.ReasonCategory,
                    command.ExpiresAtUtc,
                    command.FairnessException,
                    command.ExpectedRevision,
                    command.IdempotencyKey,
                    digest),
                context.RequestAborted);
        });

    private static Task DecideAccommodation(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        Guid accommodationId,
        IAccommodationCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<DecideAccommodationCommandV2>(context);
            if (!DecideAccommodationRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Decide,
                actor.Organization.OrganizationId,
                activityId,
                cohortId,
                enrollmentId,
                accommodationId,
                null,
                null,
                null,
                command.Approve,
                command.ExpectedRevision);
            return await coordinator.DecideAsync(
                new DecideAccommodationCommand(
                    actor,
                    activityId,
                    cohortId,
                    enrollmentId,
                    accommodationId,
                    command.Approve,
                    command.ExpectedRevision,
                    command.IdempotencyKey,
                    digest),
                context.RequestAborted);
        });

    private static Task RevokeAccommodation(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        Guid accommodationId,
        IAccommodationCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<RevokeAccommodationCommandV2>(context);
            if (!RevokeAccommodationRequestValidator.IsValid(body))
            {
                return null;
            }

            var command = body!;
            var digest = AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Revoke,
                actor.Organization.OrganizationId,
                activityId,
                cohortId,
                enrollmentId,
                accommodationId,
                null,
                null,
                null,
                false,
                command.ExpectedRevision);
            return await coordinator.RevokeAsync(
                new RevokeAccommodationCommand(
                    actor,
                    activityId,
                    cohortId,
                    enrollmentId,
                    accommodationId,
                    command.ExpectedRevision,
                    command.IdempotencyKey,
                    digest),
                context.RequestAborted);
        });

    private static async Task MutateAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        Func<EnrollmentActorContext, Task<AccommodationMutationOutcome?>> commit)
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

        var outcome = await commit(actor);
        if (outcome is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status400BadRequest, EnrollmentFailureCodes.InvalidField);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = outcome.Succeeded
            ? StatusCodes.Status200OK
            : outcome.OutcomeCode switch
            {
                EnrollmentFailureCodes.InvalidField => StatusCodes.Status400BadRequest,
                EnrollmentFailureCodes.Denied => StatusCodes.Status404NotFound,
                EnrollmentFailureCodes.AuditUnavailable or EnrollmentFailureCodes.Unavailable or AccommodationFailureCodes.PolicyUnavailable => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status409Conflict,
            };
        await context.Response.WriteAsJsonAsync(new AccommodationMutationOutcomeV2(
            "v2",
            outcome.Succeeded,
            outcome.OutcomeCode,
            outcome.AccommodationId,
            outcome.EnrollmentId,
            outcome.Status,
            outcome.Revision,
            outcome.PermittedActions));
    }
}
