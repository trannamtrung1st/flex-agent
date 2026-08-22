using System.Globalization;
using System.Text.Json;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.Contracts.Enrollment;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class EnrollmentEndpointExtensions
{
    public static IServiceCollection AddEnrollment(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        var productionLocked = environment.IsProduction() || environment.IsEnvironment("Staging");
        if (string.IsNullOrWhiteSpace(connectionString) && productionLocked)
        {
            return services;
        }

        services.AddSingleton<IEnrollmentCoordinator, EnrollmentCoordinator>();
        services.AddSingleton<IEnrollmentQueryService, EnrollmentQueryService>();
        services.AddSingleton<IEnrollmentClock, SystemEnrollmentClock>();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IEnrollmentAuthorizationPort>(_ => new AllowEnrollmentAuthorizationPort { Permit = false });
            services.AddSingleton<IActivatedCohortPort, FixedActivatedCohortPort>();
            services.AddSingleton<IEnrollmentCandidatePort, InMemoryCandidatePort>();
            services.AddSingleton<IEnrollmentStore, InMemoryEnrollmentStore>();
            services.AddSingleton<IEnrollmentOperationStore, InMemoryEnrollmentOperationStore>();
            services.AddSingleton<IEnrollmentAuditPort, RecordingEnrollmentAuditPort>();
            services.AddSingleton<IEnrollmentUnitOfWork, InMemoryEnrollmentUnitOfWork>();
            return services;
        }

        services.AddSingleton<IActivatedCohortBindingReader, PostgresActivatedCohortBindingReader>();
        services.AddSingleton<IHumanDisplayProfileDirectory, PostgresHumanDisplayProfileDirectory>();
        services.AddSingleton<IActivatedCohortPort, AssessmentActivatedCohortPort>();
        services.AddSingleton<IEnrollmentCandidatePort, IdentityEnrollmentCandidatePort>();
        services.AddSingleton<IEnrollmentStore, PostgresEnrollmentStore>();
        services.AddSingleton<IEnrollmentOperationStore, PostgresEnrollmentOperationStore>();
        services.AddSingleton<IEnrollmentAuditPort, PostgresEnrollmentAuditPort>();
        services.AddSingleton<IEnrollmentAuthorizationPort, KernelEnrollmentAuthorizationPort>();
        services.AddSingleton<IEnrollmentUnitOfWork, PostgresEnrollmentUnitOfWork>();
        return services;
    }

    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints.ServiceProvider.GetService<IEnrollmentCoordinator>() is null)
        {
            return endpoints;
        }

        var group = endpoints.MapGroup("/v1/assessment");
        group.MapGet("/activities/{activityId:guid}/cohorts/{cohortId:guid}/participant-options", ListCandidates);
        group.MapGet("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments", ListEnrollments);
        group.MapPost("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments", Assign);
        group.MapGet("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}", GetEnrollment);
        group.MapPost("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/suspend", Suspend);
        group.MapPost("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/restore", Restore);
        group.MapPost("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/close", Close);
        group.MapPost("/activities/{activityId:guid}/cohorts/{cohortId:guid}/enrollments/{enrollmentId:guid}/revoke", Revoke);
        group.MapGet("/my-work", ListMyWork);
        group.MapGet("/my-work/{enrollmentId:guid}", GetMyWork);
        return endpoints;
    }

    private static async Task ListCandidates(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        IEnrollmentQueryService queries)
    {
        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var result = await queries.ListCandidatesAsync(
            actor,
            activityId,
            cohortId,
            context.Request.Query["q"].FirstOrDefault(),
            context.Request.Query["cursor"].FirstOrDefault(),
            ParseLimit(context),
            context.RequestAborted);
        await WriteQuery(context, result, page => new
        {
            schema_version = "v1",
            items = page.Items.Select(item => new { actor_id = item.ActorId, display_label = item.DisplayLabel }),
            next_cursor = page.NextCursor,
            has_more = page.HasMore,
        });
    }

    private static async Task ListEnrollments(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        IEnrollmentQueryService queries)
    {
        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var result = await queries.ListEnrollmentsAsync(
            actor,
            activityId,
            cohortId,
            context.Request.Query["cursor"].FirstOrDefault(),
            ParseLimit(context),
            context.RequestAborted);
        await WriteQuery(context, result, page => new
        {
            schema_version = "v1",
            items = page.Items.Select(ProjectSummary),
            next_cursor = page.NextCursor,
            has_more = page.HasMore,
        });
    }

    private static async Task GetEnrollment(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        IEnrollmentQueryService queries)
    {
        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var result = await queries.GetEnrollmentAsync(actor, activityId, cohortId, enrollmentId, context.RequestAborted);
        await WriteQuery(context, result, detail => new
        {
            schema_version = "v1",
            enrollment = ProjectSummary(detail.Summary),
            history = detail.History.Select(item => new
            {
                sequence = item.Sequence,
                prior_status = item.PriorStatus,
                new_status = item.NewStatus,
                reason_code = item.ReasonCode,
                occurred_at = item.OccurredAtUtc,
            }),
        });
    }

    private static Task Assign(HttpContext context, Guid activityId, Guid cohortId, IEnrollmentCoordinator coordinator, IAntiforgery antiforgery) =>
        MutateAssignAsync(context, activityId, cohortId, coordinator, antiforgery);

    private static Task Suspend(HttpContext context, Guid activityId, Guid cohortId, Guid enrollmentId, IEnrollmentCoordinator coordinator, IAntiforgery antiforgery) =>
        MutateLifecycleAsync(context, activityId, cohortId, enrollmentId, EnrollmentOperationKinds.Suspend, coordinator, antiforgery);

    private static Task Restore(HttpContext context, Guid activityId, Guid cohortId, Guid enrollmentId, IEnrollmentCoordinator coordinator, IAntiforgery antiforgery) =>
        MutateLifecycleAsync(context, activityId, cohortId, enrollmentId, EnrollmentOperationKinds.Restore, coordinator, antiforgery);

    private static Task Close(HttpContext context, Guid activityId, Guid cohortId, Guid enrollmentId, IEnrollmentCoordinator coordinator, IAntiforgery antiforgery) =>
        MutateLifecycleAsync(context, activityId, cohortId, enrollmentId, EnrollmentOperationKinds.Close, coordinator, antiforgery);

    private static Task Revoke(HttpContext context, Guid activityId, Guid cohortId, Guid enrollmentId, IEnrollmentCoordinator coordinator, IAntiforgery antiforgery) =>
        MutateLifecycleAsync(context, activityId, cohortId, enrollmentId, EnrollmentOperationKinds.Revoke, coordinator, antiforgery);

    private static async Task ListMyWork(HttpContext context, IEnrollmentQueryService queries)
    {
        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var result = await queries.ListMyWorkAsync(
            actor,
            context.Request.Query["cursor"].FirstOrDefault(),
            ParseLimit(context),
            context.RequestAborted);
        await WriteQuery(context, result, page => new
        {
            schema_version = "v1",
            items = page.Items.Select(ProjectAssignment),
            next_cursor = page.NextCursor,
            has_more = page.HasMore,
        });
    }

    private static async Task GetMyWork(HttpContext context, Guid enrollmentId, IEnrollmentQueryService queries)
    {
        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var result = await queries.GetMyWorkAsync(actor, enrollmentId, context.RequestAborted);
        await WriteQuery(context, result, item => new { schema_version = "v1", assignment = ProjectAssignment(item) });
    }

    private static async Task MutateAssignAsync(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        IEnrollmentCoordinator coordinator,
        IAntiforgery antiforgery)
    {
        if (!await ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var body = await TryReadCommandAsync<EnrollmentAssignCommandV1>(context);
        if (body is null
            || !string.Equals(body.SchemaVersion, "v1", StringComparison.Ordinal)
            || body.ParticipantActorId == Guid.Empty
            || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, EnrollmentFailureCodes.InvalidField);
            return;
        }

        var digest = EnrollmentCommandDigest.Compute(
            EnrollmentOperationKinds.Assign,
            actor.Organization.OrganizationId,
            activityId,
            cohortId,
            null,
            body.ParticipantActorId,
            null,
            null);
        var outcome = await coordinator.AssignAsync(
            new AssignEnrollmentCommand(actor, activityId, cohortId, body.ParticipantActorId, body.IdempotencyKey, digest),
            context.RequestAborted);
        await WriteMutation(context, outcome);
    }

    private static async Task MutateLifecycleAsync(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        string operationKind,
        IEnrollmentCoordinator coordinator,
        IAntiforgery antiforgery)
    {
        if (!await ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var actor = await TryActorAsync(context);
        if (actor is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, HumanAuthenticationReasonCodes.MissingSession);
            return;
        }

        var body = await TryReadCommandAsync<EnrollmentLifecycleCommandV1>(context);
        if (body is null
            || !string.Equals(body.SchemaVersion, "v1", StringComparison.Ordinal)
            || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
            || string.IsNullOrWhiteSpace(body.ReasonCode)
            || body.ExpectedRevision < 1)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, EnrollmentFailureCodes.InvalidField);
            return;
        }

        var digest = EnrollmentCommandDigest.Compute(
            operationKind,
            actor.Organization.OrganizationId,
            activityId,
            cohortId,
            enrollmentId,
            null,
            body.ReasonCode,
            body.ExpectedRevision);
        var outcome = await coordinator.MutateAsync(
            new EnrollmentLifecycleCommand(
                actor,
                activityId,
                cohortId,
                enrollmentId,
                operationKind,
                body.ReasonCode,
                body.ExpectedRevision,
                body.IdempotencyKey,
                digest),
            context.RequestAborted);
        await WriteMutation(context, outcome);
    }

    private static async Task<EnrollmentActorContext?> TryActorAsync(HttpContext context)
    {
        var coordinator = context.RequestServices.GetRequiredService<IHumanAuthenticationCoordinator>();
        var options = context.RequestServices.GetRequiredService<HumanAuthenticationHostOptions>();
        var relationships = context.RequestServices.GetRequiredService<IAssessmentRelationshipResolver>();
        var credential = context.Request.Cookies[HumanAuthenticationHostOptions.CookieName];
        if (string.IsNullOrWhiteSpace(credential))
        {
            return null;
        }

        var session = await coordinator.AuthenticateAsync(credential, advanceActivity: true, context.RequestAborted);
        if (session is null)
        {
            return null;
        }

        var authorization = await relationships.ResolveAsync(session.ActorId, session.OrganizationId, context.RequestAborted);
        return new EnrollmentActorContext(
            new TrustedActor(session.ActorId, HumanInteractiveActorTypes.Interactive),
            new OrganizationScope(session.OrganizationId),
            authorization.Relationship,
            session.Strength,
            Guid.CreateVersion7(),
            "https",
            authorization.PermittedActions);
    }

    private static async Task<bool> ValidateMutationAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "csrf.invalid");
            return false;
        }
    }

    private static async Task WriteMutation(HttpContext context, EnrollmentMutationOutcome outcome)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = outcome.Succeeded
            ? StatusCodes.Status200OK
            : outcome.OutcomeCode switch
            {
                EnrollmentFailureCodes.InvalidField or EnrollmentFailureCodes.InvalidReason => StatusCodes.Status400BadRequest,
                EnrollmentFailureCodes.Denied => StatusCodes.Status404NotFound,
                EnrollmentFailureCodes.AuditUnavailable or EnrollmentFailureCodes.Unavailable => StatusCodes.Status503ServiceUnavailable,
                EnrollmentFailureCodes.StaleRevision or EnrollmentFailureCodes.Conflict or EnrollmentFailureCodes.IdempotencyConflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status409Conflict,
            };
        await context.Response.WriteAsJsonAsync(new EnrollmentMutationOutcomeV1(
            "v1",
            outcome.Succeeded,
            outcome.OutcomeCode,
            outcome.EnrollmentId,
            outcome.Status,
            outcome.Revision,
            outcome.Visibility,
            outcome.PermittedActions));
    }

    private static async Task WriteQuery<T>(HttpContext context, EnrollmentDecision<T> result, Func<T, object> projector)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!result.Succeeded || result.Value is null)
        {
            await WriteError(
                context,
                result.OutcomeCode == EnrollmentFailureCodes.InvalidField
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status404NotFound,
                result.OutcomeCode);
            return;
        }

        await context.Response.WriteAsJsonAsync(projector(result.Value));
    }

    private static Task WriteError(HttpContext context, int status, string code)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { error = code });
    }

    private static object ProjectSummary(EnrollmentSummary summary) => new
    {
        enrollment_id = summary.EnrollmentId,
        participant_actor_id = summary.ParticipantActorId,
        display_label = summary.DisplayLabel,
        status = summary.Status,
        revision = summary.Revision,
        assigned_at = FormatUtc(summary.AssignedAtUtc),
        updated_at = FormatUtc(summary.UpdatedAtUtc),
        visibility = summary.Visibility,
        permitted_actions = summary.PermittedActions,
    };

    private static object ProjectAssignment(AssignmentSummary assignment) => new
    {
        enrollment_id = assignment.EnrollmentId,
        status = assignment.Status,
        visibility = assignment.Visibility,
        activity_title = assignment.ActivityTitle,
        task_title = assignment.TaskTitle,
        time_zone_id = assignment.TimeZoneId,
        starts_at_utc = FormatUtc(assignment.StartsAtUtc),
        ends_at_utc = FormatUtc(assignment.EndsAtUtc),
        deadline_utc = FormatUtc(assignment.DeadlineUtc),
        summary_available = assignment.SummaryAvailable,
        permitted_actions = assignment.PermittedActions,
    };

    private static async Task<T?> TryReadCommandAsync<T>(HttpContext context)
        where T : class
    {
        if (context.Request.ContentLength is > EnrollmentHttpLimits.MaximumBodyBytes)
        {
            return null;
        }

        try
        {
            return await context.Request.ReadFromJsonAsync<T>(EnrollmentHttpLimits.SerializerOptions, context.RequestAborted);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FormatUtc(DateTimeOffset? value) =>
        value is null
            ? null
            : value.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static int ParseLimit(HttpContext context)
    {
        if (!int.TryParse(context.Request.Query["limit"].FirstOrDefault(), out var limit))
        {
            return EnrollmentPageBounds.DefaultLimit;
        }

        return limit is < 1 or > EnrollmentPageBounds.MaximumLimit
            ? EnrollmentPageBounds.DefaultLimit
            : limit;
    }
}

internal static class EnrollmentHttpLimits
{
    public const int MaximumBodyBytes = 4096;

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };
}
