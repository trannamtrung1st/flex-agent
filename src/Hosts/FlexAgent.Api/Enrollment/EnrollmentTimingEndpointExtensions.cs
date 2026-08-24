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
        await EnrollmentEndpointExtensions.WriteQuery(context, result, ProjectEnrollmentTiming);
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
        await EnrollmentEndpointExtensions.WriteQuery(context, result, ProjectMyWorkTiming);
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
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
                || !EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision)
                || string.IsNullOrWhiteSpace(body.Dimension)
                || string.IsNullOrWhiteSpace(body.RequestedValue)
                || string.IsNullOrWhiteSpace(body.ReasonCategory))
            {
                return null;
            }

            var digest = AccommodationCommandDigest.Compute(
                AccommodationOperationKinds.Grant,
                actor.Organization.OrganizationId,
                activityId,
                cohortId,
                enrollmentId,
                null,
                body.Dimension,
                body.RequestedValue,
                body.ReasonCategory,
                body.FairnessException,
                body.ExpectedRevision,
                body.ExpiresAtUtc);
            return await coordinator.GrantAsync(
                new GrantAccommodationCommand(
                    actor,
                    activityId,
                    cohortId,
                    enrollmentId,
                    body.Dimension,
                    body.RequestedValue,
                    body.ReasonCategory,
                    body.ExpiresAtUtc,
                    body.FairnessException,
                    body.ExpectedRevision,
                    body.IdempotencyKey,
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
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
                || !EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision))
            {
                return null;
            }

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
                body.Approve,
                body.ExpectedRevision);
            return await coordinator.DecideAsync(
                new DecideAccommodationCommand(
                    actor,
                    activityId,
                    cohortId,
                    enrollmentId,
                    accommodationId,
                    body.Approve,
                    body.ExpectedRevision,
                    body.IdempotencyKey,
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
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
                || !EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision))
            {
                return null;
            }

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
                body.ExpectedRevision);
            return await coordinator.RevokeAsync(
                new RevokeAccommodationCommand(
                    actor,
                    activityId,
                    cohortId,
                    enrollmentId,
                    accommodationId,
                    body.ExpectedRevision,
                    body.IdempotencyKey,
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

    private static EnrollmentTimingV2 ProjectEnrollmentTiming(EnrollmentTimingDetail detail) =>
        new(
            "v2",
            new EnrollmentTimingEnrollmentV2(
                detail.Summary.EnrollmentId,
                detail.Summary.Status,
                detail.Summary.Revision,
                detail.Summary.Visibility,
                detail.Summary.PermittedActions),
            ProjectBaseline(detail.Baseline),
            ProjectEffective(detail.Timing),
            ProjectCurrent(detail.Timing),
            detail.PolicyAvailable,
            detail.PermittedAccommodationDimensions,
            detail.PermittedReasonCategories,
            detail.History.Select(ProjectHistory).ToArray());

    private static MyWorkTimingV2 ProjectMyWorkTiming(AssignmentTimingSummary detail) =>
        new(
            "v2",
            new MyWorkTimingAssignmentV2(
                detail.Assignment.EnrollmentId,
                detail.Assignment.Status,
                detail.Assignment.Visibility,
                detail.Assignment.ActivityTitle,
                detail.Assignment.TaskTitle,
                detail.Assignment.TimeZoneId,
                EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.StartsAtUtc),
                EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.EndsAtUtc),
                EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.DeadlineUtc),
                detail.Assignment.SummaryAvailable,
                detail.Assignment.PermittedActions),
            detail.Timing is null ? null : ProjectEffective(detail.Timing),
            detail.ParticipantConsequenceCode);

    private static TimingBaselineV2 ProjectBaseline(BaselineTiming baseline) =>
        new(
            RequiredUtc(baseline.StartsAtUtc),
            RequiredUtc(baseline.EndsAtUtc),
            RequiredUtc(baseline.DeadlineUtc),
            baseline.TimeZoneId,
            baseline.AttemptLimit,
            baseline.PerAttemptDurationSeconds);

    private static IReadOnlyList<CurrentAccommodationEffectV2> ProjectCurrent(EffectiveTiming timing) =>
        timing.CurrentAccommodations
            .Select(item => new CurrentAccommodationEffectV2(
                item.AccommodationId,
                item.Dimension,
                item.ConsequenceCode))
            .ToArray();

    private static TimingEffectiveWindowV2 ProjectEffective(EffectiveTiming timing) =>
        new(
            RequiredUtc(timing.EffectiveSubmissionStartUtc),
            RequiredUtc(timing.EffectiveSubmissionExclusiveEndUtc),
            RequiredUtc(timing.EffectiveAttemptStartUtc),
            RequiredUtc(timing.EffectiveAttemptStartExclusiveEndUtc),
            timing.EffectivePerAttemptDurationSeconds,
            RequiredUtc(timing.EvaluatedAtUtc),
            timing.EligibilityState,
            timing.IsAuthoritativeEligibility,
            timing.TimeZoneId,
            timing.ParticipantConsequenceCode);

    private static AccommodationHistoryItemV2 ProjectHistory(Accommodation item) =>
        new(
            item.AccommodationId,
            item.Dimension,
            item.Status,
            item.NormalizedValue,
            item.ReasonCategory,
            item.FairnessException,
            item.Revision,
            RequiredUtc(item.CreatedAtUtc),
            EnrollmentEndpointExtensions.FormatUtc(item.DecidedAtUtc),
            item.ExpiresAtUtc is null
                ? null
                : AccommodationPolicyNormalizer.FormatCanonicalInstant(item.ExpiresAtUtc.Value));

    private static string RequiredUtc(DateTimeOffset value) =>
        EnrollmentEndpointExtensions.FormatUtc(value)
        ?? throw new InvalidOperationException("Timing projection requires a UTC instant.");
}
