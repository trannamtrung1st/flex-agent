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
        await EnrollmentEndpointExtensions.WriteQuery(context, result, detail => new
        {
            schema_version = "v2",
            enrollment = new
            {
                enrollment_id = detail.Summary.EnrollmentId,
                status = detail.Summary.Status,
                revision = detail.Summary.Revision,
                visibility = detail.Summary.Visibility,
                permitted_actions = detail.Summary.PermittedActions,
            },
            baseline = ProjectBaseline(detail.Baseline),
            effective = ProjectEffective(detail.Timing),
            current_accommodation_id = detail.Timing.CurrentAccommodationId,
            policy_available = detail.PolicyAvailable,
            permitted_dimensions = detail.PermittedAccommodationDimensions,
            permitted_reason_categories = detail.PermittedReasonCategories,
            history = detail.History.Select(ProjectHistory),
        });
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
        await EnrollmentEndpointExtensions.WriteQuery(context, result, detail => new
        {
            schema_version = "v2",
            assignment = new
            {
                enrollment_id = detail.Assignment.EnrollmentId,
                status = detail.Assignment.Status,
                visibility = detail.Assignment.Visibility,
                activity_title = detail.Assignment.ActivityTitle,
                task_title = detail.Assignment.TaskTitle,
                time_zone_id = detail.Assignment.TimeZoneId,
                starts_at_utc = EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.StartsAtUtc),
                ends_at_utc = EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.EndsAtUtc),
                deadline_utc = EnrollmentEndpointExtensions.FormatUtc(detail.Assignment.DeadlineUtc),
                summary_available = detail.Assignment.SummaryAvailable,
                permitted_actions = detail.Assignment.PermittedActions,
            },
            effective = detail.Timing is null ? null : ProjectEffective(detail.Timing),
            participant_consequence_code = detail.ParticipantConsequenceCode,
        });
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
                || body.ExpectedRevision < 1
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
                || body.ExpectedRevision < 1)
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
                || body.ExpectedRevision < 1)
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

    private static object ProjectBaseline(BaselineTiming baseline) => new
    {
        starts_at_utc = EnrollmentEndpointExtensions.FormatUtc(baseline.StartsAtUtc),
        ends_at_utc = EnrollmentEndpointExtensions.FormatUtc(baseline.EndsAtUtc),
        deadline_utc = EnrollmentEndpointExtensions.FormatUtc(baseline.DeadlineUtc),
        time_zone_id = baseline.TimeZoneId,
        attempt_limit = baseline.AttemptLimit,
        per_attempt_duration_seconds = baseline.PerAttemptDurationSeconds,
    };

    private static object ProjectEffective(EffectiveTiming timing) => new
    {
        submission_starts_at_utc = EnrollmentEndpointExtensions.FormatUtc(timing.EffectiveSubmissionStartUtc),
        submission_exclusive_end_utc = EnrollmentEndpointExtensions.FormatUtc(timing.EffectiveSubmissionExclusiveEndUtc),
        attempt_start_utc = EnrollmentEndpointExtensions.FormatUtc(timing.EffectiveAttemptStartUtc),
        attempt_start_exclusive_end_utc = EnrollmentEndpointExtensions.FormatUtc(timing.EffectiveAttemptStartExclusiveEndUtc),
        per_attempt_duration_seconds = timing.EffectivePerAttemptDurationSeconds,
        evaluated_at_utc = EnrollmentEndpointExtensions.FormatUtc(timing.EvaluatedAtUtc),
        eligibility_state = timing.EligibilityState,
        is_authoritative = timing.IsAuthoritativeEligibility,
        time_zone_id = timing.TimeZoneId,
        participant_consequence_code = timing.ParticipantConsequenceCode,
    };

    private static object ProjectHistory(Accommodation item) => new
    {
        accommodation_id = item.AccommodationId,
        dimension = item.Dimension,
        status = item.Status,
        normalized_value = item.NormalizedValue,
        reason_category = item.ReasonCategory,
        fairness_exception = item.FairnessException,
        revision = item.Revision,
        created_at_utc = EnrollmentEndpointExtensions.FormatUtc(item.CreatedAtUtc),
        decided_at_utc = EnrollmentEndpointExtensions.FormatUtc(item.DecidedAtUtc),
        expires_at_utc = EnrollmentEndpointExtensions.FormatUtc(item.ExpiresAtUtc),
    };
}
