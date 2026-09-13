using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Evaluation.Domain;
using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.Api;

public static class ReviewEndpointExtensions
{
    public static IServiceCollection AddReview(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddSingleton<IActiveReviewAssignmentPort, FlexAgent.Evaluation.Infrastructure.Review.PostgresActiveReviewAssignmentPort>();
        services.AddSingleton<IAssignedReviewQueryService, FlexAgent.Evaluation.Infrastructure.Review.PostgresAssignedReviewQueryService>();
        return services;
    }

    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/review");
        group.MapGet("/work", ListWork);
        group.MapGet("/cases/{reviewCaseId:guid}", GetCase);
        group.MapGet("/cases/{reviewCaseId:guid}/criteria/{criterionId}", GetCriterion);
        group.MapGet("/cases/{reviewCaseId:guid}/evidence/{evidenceId}", OpenEvidence);
        return endpoints;
    }

    internal static async Task ListWork(
        HttpContext context,
        string? cursor,
        int? limit)
    {
        var actor = await TryReviewActorAsync(context);
        if (actor is null)
        {
            return;
        }

        if (context.RequestServices.GetService<IAssignedReviewQueryService>() is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, ReviewFailureCodes.Denied);
            return;
        }

        var queries = context.RequestServices.GetRequiredService<IAssignedReviewQueryService>();
        var result = await queries.ListWorkAsync(
            actor,
            new AssignedReviewWorkListRequest(cursor, AssignedReviewAdmission.NormalizeWorkLimit(limit)),
            context.RequestAborted);
        await WriteReviewQuery(context, result, page => new
        {
            schema_version = "v1",
            items = page.Items,
            next_cursor = page.NextCursor,
            has_more = page.HasMore,
        });
    }

    internal static async Task GetCase(HttpContext context, Guid reviewCaseId)
    {
        var actor = await TryReviewActorAsync(context);
        if (actor is null)
        {
            return;
        }

        if (context.RequestServices.GetService<IAssignedReviewQueryService>() is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, ReviewFailureCodes.Denied);
            return;
        }

        var queries = context.RequestServices.GetRequiredService<IAssignedReviewQueryService>();
        var result = await queries.GetCaseAsync(actor, reviewCaseId, context.RequestAborted);
        await WriteReviewQuery(context, result, value => value);
    }

    internal static async Task GetCriterion(HttpContext context, Guid reviewCaseId, string criterionId)
    {
        var actor = await TryReviewActorAsync(context);
        if (actor is null)
        {
            return;
        }

        if (context.RequestServices.GetService<IAssignedReviewQueryService>() is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, ReviewFailureCodes.Denied);
            return;
        }

        var queries = context.RequestServices.GetRequiredService<IAssignedReviewQueryService>();
        var result = await queries.GetCriterionAsync(actor, reviewCaseId, criterionId, context.RequestAborted);
        await WriteReviewQuery(context, result, value => value);
    }

    internal static async Task OpenEvidence(HttpContext context, Guid reviewCaseId, string evidenceId)
    {
        var actor = await TryReviewActorAsync(context);
        if (actor is null)
        {
            return;
        }

        if (context.RequestServices.GetService<IAssignedReviewQueryService>() is null)
        {
            await EnrollmentEndpointExtensions.WriteError(context, StatusCodes.Status404NotFound, ReviewFailureCodes.Denied);
            return;
        }

        var queries = context.RequestServices.GetRequiredService<IAssignedReviewQueryService>();
        var result = await queries.OpenEvidenceAsync(actor, reviewCaseId, evidenceId, context.RequestAborted);
        await WriteReviewQuery(context, result, value => value);
    }

    private static async Task<AssignedReviewActorContext?> TryReviewActorAsync(HttpContext context)
    {
        var enrollmentActor = await EnrollmentEndpointExtensions.AcceptAuthenticatedAsync(context, "review.read");
        if (enrollmentActor is null)
        {
            return null;
        }

        return new AssignedReviewActorContext(
            enrollmentActor.Organization.OrganizationId,
            enrollmentActor.Actor.ActorId,
            enrollmentActor.Relationship,
            enrollmentActor.GrantedActions);
    }

    private static async Task WriteReviewQuery<T>(
        HttpContext context,
        EvaluationDecision<T> result,
        Func<T, object> projector)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!result.Succeeded || result.Value is null)
        {
            await EnrollmentEndpointExtensions.WriteError(
                context,
                result.OutcomeCode == ReviewFailureCodes.InvalidField
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status404NotFound,
                result.OutcomeCode);
            return;
        }

        await context.Response.WriteAsJsonAsync(projector(result.Value));
    }
}
