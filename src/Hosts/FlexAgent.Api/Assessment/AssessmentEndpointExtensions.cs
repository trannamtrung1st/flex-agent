using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class AssessmentEndpointExtensions
{
    public static IServiceCollection AddAssessmentConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        var productionLocked = environment.IsProduction() || environment.IsEnvironment("Staging");
        if (string.IsNullOrWhiteSpace(connectionString) && productionLocked)
        {
            return services;
        }

        services.AddSingleton<IActivationBaselineDigester, ActivationBaselineDigester>();
        services.AddSingleton<IAssessmentCommandDigest, AssessmentCommandDigest>();
        services.AddSingleton<IAssessmentDraftHandler, AssessmentDraftHandler>();
        services.AddSingleton<IAssessmentActivationCoordinator, AssessmentActivationCoordinator>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            if (services.All(descriptor => descriptor.ServiceType != typeof(Npgsql.NpgsqlDataSource)))
            {
                services.AddSingleton(_ => Npgsql.NpgsqlDataSource.Create(connectionString));
                services.AddSingleton<PostgresConnectionAccessor>();
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(IAuthorizationKernel)))
            {
                services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(ICommitAuthorizationKernel)))
            {
                services.AddSingleton<ICommitAuthorizationKernel>(sp =>
                    sp.GetService<IAuthorizationKernel>() as ICommitAuthorizationKernel
                    ?? ActivatorUtilities.CreateInstance<PostgresAuthorizationKernel>(sp));
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(IAuditEventWriter)))
            {
                services.AddSingleton<IAuditEventWriter, PostgresAuditEventWriter>();
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(IOutboxItemWriter)))
            {
                services.AddSingleton<IOutboxItemWriter, PostgresOutboxItemWriter>();
            }

            services.AddSingleton<PostgresAssessmentSourceCatalog>();
            services.AddSingleton<IAssessmentSourceCatalog>(sp => sp.GetRequiredService<PostgresAssessmentSourceCatalog>());
            services.AddSingleton<IAssessmentSourceTransactionPort>(sp => sp.GetRequiredService<PostgresAssessmentSourceCatalog>());
            services.AddSingleton<IAssessmentDevelopmentSourceSeeder, NoOpAssessmentDevelopmentSourceSeeder>();
            services.AddSingleton<IAssessmentDraftStore, PostgresAssessmentDraftStore>();
            services.AddSingleton<IAssessmentAuthorizationPort, KernelAssessmentAuthorizationPort>();
            services.AddSingleton<IAssessmentRelationshipResolver, PostgresAssessmentRelationshipResolver>();
            services.AddSingleton<IAssessmentActivationUnitOfWork, PostgresAssessmentUnitOfWork>();
            services.AddSingleton<IAssessmentBaselineStore, PostgresAssessmentBaselineStore>();
            services.AddSingleton<IAssessmentActivationAttemptStore, PostgresAssessmentAttemptStore>();
            return services;
        }

        services.AddSingleton<IAssessmentDraftStore, InMemoryAssessmentDraftStore>();
        services.AddSingleton<InMemoryAssessmentSourceCatalog>();
        services.AddSingleton<IAssessmentSourceCatalog>(sp => sp.GetRequiredService<InMemoryAssessmentSourceCatalog>());
        services.AddSingleton<IAssessmentSourceTransactionPort>(sp => sp.GetRequiredService<InMemoryAssessmentSourceCatalog>());
        services.AddSingleton<IAssessmentDevelopmentSourceSeeder>(sp => sp.GetRequiredService<InMemoryAssessmentSourceCatalog>());
        services.AddSingleton<IAssessmentAuthorizationPort>(_ => new InMemoryAssessmentAuthorizationPort(permit: false));
        services.AddSingleton<IAssessmentRelationshipResolver, EmptyAssessmentRelationshipResolver>();
        services.AddSingleton<IAssessmentActivationUnitOfWork, InMemoryAssessmentUnitOfWork>();
        services.AddSingleton<IAssessmentBaselineStore, InMemoryAssessmentBaselineStore>();
        services.AddSingleton<IAssessmentActivationAttemptStore, InMemoryAssessmentAttemptStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var productionLocked = environment.IsProduction() || environment.IsEnvironment("Staging");
        if (productionLocked && string.IsNullOrWhiteSpace(HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration)))
        {
            return endpoints;
        }

        if (endpoints.ServiceProvider.GetService<IAssessmentActivationCoordinator>() is null)
        {
            return endpoints;
        }

        var group = endpoints.MapGroup("/v1/assessment");
        group.MapGet("/shell", GetShell);
        group.MapGet("/source-options", GetSourceOptions);
        group.MapGet("/activities", ListActivities);
        group.MapPost("/activities", CreateActivity);
        group.MapGet("/activities/{activityId:guid}", GetActivity);
        group.MapPost("/activities/{activityId:guid}", SaveActivity);
        group.MapPost("/activities/{activityId:guid}/readiness", CheckReadiness);
        group.MapPost("/activities/{activityId:guid}/cohorts/{cohortId:guid}/activate", Activate);
        group.MapGet("/activities/{activityId:guid}/cohorts/{cohortId:guid}/activation", Reconcile);
        return endpoints;
    }

    private static async Task GetShell(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAssessmentRelationshipResolver relationships)
    {
        var resolved = await TryActorAsync(context, coordinator, options, relationships);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = HumanAuthenticationReasonCodes.MissingSession });
            return;
        }

        var strength = AssessmentAuthenticationPolicy.Evaluate(
            resolved.Actor,
            AssessmentAuthorizationActions.ReadActivity);
        if (strength is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = strength });
            return;
        }

        await context.Response.WriteAsJsonAsync(new
        {
            schema_version = "v1",
            actor_id = resolved.Actor.Actor.ActorId,
            organization_id = resolved.Actor.Organization.OrganizationId,
            relationship = resolved.Actor.Relationship,
            navigation = new[]
            {
                new { destination_id = "home", is_available = resolved.Authorization.PermittedActions.Count > 0 },
                new { destination_id = "activities", is_available = resolved.Authorization.PermittedActions.Count > 0 },
            },
            permitted_actions = resolved.Authorization.PermittedActions,
        });
    }

    private static async Task GetSourceOptions(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAssessmentDraftHandler drafts,
        IHostEnvironment hostEnvironment)
    {
        var resolved = await TryActorAsync(context, coordinator, options, context.RequestServices.GetRequiredService<IAssessmentRelationshipResolver>());
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var environment = AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName);
        var result = await drafts.ListSourceOptionsAsync(resolved.Actor, environment, context.RequestAborted);
        if (!result.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = result.OutcomeCode });
            return;
        }

        await context.Response.WriteAsJsonAsync(new
        {
            environment,
            sources = result.Value!.Select(source => new
            {
                category = source.Category,
                source_id = source.SourceId,
                version_id = source.VersionId,
                content_digest = source.ContentDigest,
                source_kind = source.SourceKind,
                production_eligible = source.ProductionEligible,
            }),
        });
    }

    private static async Task ListActivities(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAssessmentDraftHandler drafts)
    {
        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var result = await drafts.ListActivitiesAsync(resolved.Actor, context.RequestAborted);
        if (!result.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = result.OutcomeCode });
            return;
        }

        await context.Response.WriteAsJsonAsync(new
        {
            activities = result.Value!.Select(draft => new
            {
                activity_id = draft.ActivityId,
                title = draft.Content.Title,
                revision_number = draft.RevisionNumber,
                has_activated_cohort = draft.HasActivatedCohort,
            }),
            permitted_actions = HasAction(resolved, AssessmentAuthorizationActions.CreateActivity)
                ? new[] { "create_assessment" }
                : Array.Empty<string>(),
        });
    }

    private static async Task CreateActivity(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAntiforgery antiforgery,
        IAssessmentDraftHandler drafts,
        IAssessmentDraftStore store,
        IAssessmentDevelopmentSourceSeeder seeder,
        IHostEnvironment hostEnvironment)
    {
        if (!await ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var actor = resolved.Actor;

        var request = await context.Request.ReadFromJsonAsync<CreateActivityRequest>(context.RequestAborted);
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = AssessmentFailureCodes.InvalidField });
            return;
        }

        var development = AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName) == DeploymentEnvironments.Development;
        var policy = ExactSourceRef.TryCreate(request.OrganizationPolicySourceId, request.OrganizationPolicyVersionId, request.OrganizationPolicyDigest);
        var agent = ExactSourceRef.TryCreate(request.AgentSourceId, request.AgentVersionId, request.AgentDigest);
        var harness = ExactSourceRef.TryCreate(request.HarnessSourceId, request.HarnessVersionId, request.HarnessDigest);
        var workflow = ExactSourceRef.TryCreate(request.WorkflowSourceId, request.WorkflowVersionId, request.WorkflowDigest);
        var adaptive = ExactSourceRef.TryCreate(request.AdaptiveFollowUpSourceId, request.AdaptiveFollowUpVersionId, request.AdaptiveFollowUpDigest);
        var rubric = ExactSourceRef.TryCreate(request.RubricSourceId, request.RubricVersionId, request.RubricDigest);
        var model = ExactSourceRef.TryCreate(request.ModelSourceId, request.ModelVersionId, request.ModelDigest);
        var capability = ExactSourceRef.TryCreate(request.CapabilitySourceId, request.CapabilityVersionId, request.CapabilityDigest);
        var review = ExactSourceRef.TryCreate(request.ReviewSourceId, request.ReviewVersionId, request.ReviewDigest);
        var taskSource = ExactSourceRef.TryCreate(request.TaskSourceId, request.TaskVersionId, request.TaskDigest);
        IReadOnlyList<ExactSourceRef> knowledge = [];
        if (policy is null && development)
        {
            seeder.EnsureOrganization(actor.Organization.OrganizationId);
            policy = AssessmentDevelopmentSources.OrganizationPolicy;
            agent = AssessmentDevelopmentSources.Agent;
            harness = AssessmentDevelopmentSources.Harness;
            workflow = AssessmentDevelopmentSources.Workflow;
            adaptive = AssessmentDevelopmentSources.AdaptiveFollowUp;
            rubric = AssessmentDevelopmentSources.Rubric;
            model = AssessmentDevelopmentSources.ModelDeployment;
            capability = AssessmentDevelopmentSources.Capability;
            review = AssessmentDevelopmentSources.ReviewRelease;
            taskSource = AssessmentDevelopmentSources.TaskRequirement;
            knowledge = [AssessmentDevelopmentSources.Knowledge];
        }

        if (policy is null || agent is null || harness is null || workflow is null || adaptive is null || rubric is null || model is null || capability is null || review is null || taskSource is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = AssessmentFailureCodes.InvalidField });
            return;
        }

        var created = await drafts.CreateAsync(
            new CreateAssessmentDraftCommand(
                actor,
                request.Title,
                new TaskBinding(
                    request.TaskId == Guid.Empty ? Guid.CreateVersion7() : request.TaskId,
                    string.IsNullOrWhiteSpace(request.TaskTitle) ? "Task 1" : request.TaskTitle,
                    string.IsNullOrWhiteSpace(request.SubmissionRequirementSummary)
                        ? "Submit one written response"
                        : request.SubmissionRequirementSummary,
                    taskSource),
                request.StartsAtUtc == default
                    ? new TimingRules(
                        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
                        "UTC",
                        2,
                        3600)
                    : new TimingRules(
                        request.StartsAtUtc,
                        request.EndsAtUtc,
                        request.DeadlineUtc,
                        string.IsNullOrWhiteSpace(request.TimeZoneId) ? "UTC" : request.TimeZoneId,
                        request.AttemptLimit < 1 ? 2 : request.AttemptLimit,
                        request.PerAttemptDurationSeconds),
                policy,
                agent,
                harness,
                workflow,
                adaptive,
                rubric,
                model,
                knowledge,
                capability,
                review,
                AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName)),
            context.RequestAborted);

        AssessmentCohort? cohort = null;
        if (created is { Succeeded: true, Value: not null })
        {
            cohort = await store.FindCohortForActivityAsync(
                actor.Organization.OrganizationId,
                created.Value.ActivityId,
                context.RequestAborted);
        }

        context.Response.StatusCode = created.Succeeded ? StatusCodes.Status201Created : StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            succeeded = created.Succeeded,
            outcome_code = created.OutcomeCode,
            activity_id = created.Value?.ActivityId,
            revision_id = created.Value?.RevisionId,
            revision_number = created.Value?.RevisionNumber,
            cohort_id = cohort?.CohortId,
        });
    }

    private static async Task GetActivity(
        HttpContext context,
        Guid activityId,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAssessmentDraftHandler drafts,
        IAssessmentDraftStore store)
    {
        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var result = await drafts.GetActivityAsync(resolved.Actor, activityId, context.RequestAborted);
        if (!result.Succeeded || result.Value is null)
        {
            context.Response.StatusCode = result.OutcomeCode == AssessmentFailureCodes.Denied
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status403Forbidden;
            if (result.OutcomeCode != AssessmentFailureCodes.Denied)
            {
                await context.Response.WriteAsJsonAsync(new { error = result.OutcomeCode });
            }

            return;
        }

        var draft = result.Value;
        var cohort = await store.FindCohortForActivityAsync(
            resolved.Actor.Organization.OrganizationId,
            activityId,
            context.RequestAborted);
        await context.Response.WriteAsJsonAsync(new
        {
            activity_id = draft.ActivityId,
            title = draft.Content.Title,
            revision_id = draft.RevisionId,
            revision_number = draft.RevisionNumber,
            form = draft.Form,
            configured_type = draft.ConfiguredType,
            has_activated_cohort = draft.HasActivatedCohort,
            memory_mode = draft.Content.Memory.Mode,
            cohort_id = cohort?.CohortId,
            cohort_state = cohort?.State,
            baseline_digest = cohort?.BaselineDigest,
            permitted_actions = AssessmentDraftProjection.PermittedActions(
                resolved.Authorization.PermittedActions,
                draft.HasActivatedCohort),
        });
    }

    private static async Task SaveActivity(
        HttpContext context,
        Guid activityId,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAntiforgery antiforgery,
        IAssessmentDraftHandler drafts,
        IAssessmentDraftStore store,
        IHostEnvironment hostEnvironment)
    {
        if (!await ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var actor = resolved.Actor;

        var request = await context.Request.ReadFromJsonAsync<SaveActivityRequest>(context.RequestAborted);
        var current = await store.GetDraftAsync(actor.Organization.OrganizationId, activityId, context.RequestAborted);
        if (request is null || current is null)
        {
            context.Response.StatusCode = request is null ? StatusCodes.Status400BadRequest : StatusCodes.Status404NotFound;
            return;
        }

        var saved = await drafts.SaveAsync(
            new SaveAssessmentDraftCommand(
                actor,
                activityId,
                request.ExpectedRevisionNumber,
                current.Content with { Title = request.Title },
                AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName)),
            context.RequestAborted);
        context.Response.StatusCode = saved.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new
        {
            succeeded = saved.Succeeded,
            outcome_code = saved.OutcomeCode,
            activity_id = saved.Value?.ActivityId,
            revision_id = saved.Value?.RevisionId,
            revision_number = saved.Value?.RevisionNumber,
        });
    }

    private static async Task CheckReadiness(
        HttpContext context,
        Guid activityId,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAntiforgery antiforgery,
        IAssessmentDraftHandler drafts,
        IAssessmentDevelopmentSourceSeeder seeder,
        IHostEnvironment hostEnvironment)
    {
        if (!await ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var actor = resolved.Actor;

        if (AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName) == DeploymentEnvironments.Development)
        {
            seeder.EnsureOrganization(actor.Organization.OrganizationId);
        }

        var result = await drafts.CheckReadinessAsync(
            new CheckReadinessQuery(actor, activityId, AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName)),
            context.RequestAborted);
        context.Response.StatusCode = result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            succeeded = result.Succeeded,
            outcome_code = result.OutcomeCode,
            overall_severity = result.Value?.OverallSeverity,
            issues = result.Value?.Issues.Select(issue => new
            {
                issue.Category,
                issue.Severity,
                issue.ReasonCode,
                issue.RecoveryHint,
            }),
        });
    }

    private static async Task Activate(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAntiforgery antiforgery,
        IAssessmentActivationCoordinator activation,
        IAssessmentCommandDigest digests,
        IAssessmentDevelopmentSourceSeeder seeder,
        IHostEnvironment hostEnvironment)
    {
        if (!await ValidateMutationAsync(context, antiforgery))
        {
            return;
        }

        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var actor = resolved.Actor;

        var request = await context.Request.ReadFromJsonAsync<ActivateRequest>(context.RequestAborted);
        if (request is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName) == DeploymentEnvironments.Development)
        {
            seeder.EnsureOrganization(actor.Organization.OrganizationId);
        }

        var command = new ActivateCohortCommand(
            actor,
            activityId,
            cohortId,
            request.ExpectedRevisionId,
            request.ExpectedRevisionNumber,
            request.IdempotencyKey,
            "pending",
            AssessmentHostEnvironment.FromAspNetCore(hostEnvironment.EnvironmentName));
        command = command with { TrustedCommandDigest = digests.Compute(command) };
        var outcome = await activation.ActivateAsync(command, context.RequestAborted);
        context.Response.StatusCode = outcome.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(outcome);
    }

    private static async Task Reconcile(
        HttpContext context,
        Guid activityId,
        Guid cohortId,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAssessmentActivationCoordinator activation)
    {
        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var actor = resolved.Actor;
        var idempotencyKey = context.Request.Query["idempotency_key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = AssessmentFailureCodes.InvalidField });
            return;
        }

        var outcome = await activation.ReconcileAsync(
            new ReconcileActivationQuery(actor, activityId, cohortId, idempotencyKey),
            context.RequestAborted);
        context.Response.StatusCode = outcome.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(outcome);
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "csrf.invalid" });
            return false;
        }
    }

    private static Task<ResolvedAssessmentActor?> TryActorAsync(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options) =>
        TryActorAsync(
            context,
            coordinator,
            options,
            context.RequestServices.GetRequiredService<IAssessmentRelationshipResolver>());

    private static async Task<ResolvedAssessmentActor?> TryActorAsync(
        HttpContext context,
        IHumanAuthenticationCoordinator coordinator,
        HumanAuthenticationHostOptions options,
        IAssessmentRelationshipResolver relationships)
    {
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

        var authorization = await relationships.ResolveAsync(
            session.ActorId,
            session.OrganizationId,
            context.RequestAborted);
        return new ResolvedAssessmentActor(
            new AssessmentActorContext(
                new TrustedActor(session.ActorId, HumanInteractiveActorTypes.Interactive),
                new OrganizationScope(session.OrganizationId),
                authorization.Relationship,
                session.Strength,
                Guid.CreateVersion7(),
                "https"),
            authorization);
    }

    private static bool HasAction(ResolvedAssessmentActor actor, string action) =>
        actor.Authorization.PermittedActions.Contains(action, StringComparer.Ordinal);

    private sealed record CreateActivityRequest(
        string Title,
        string TaskTitle,
        string SubmissionRequirementSummary,
        Guid TaskId,
        Guid OrganizationPolicySourceId,
        Guid OrganizationPolicyVersionId,
        string OrganizationPolicyDigest,
        Guid AgentSourceId,
        Guid AgentVersionId,
        string AgentDigest,
        Guid HarnessSourceId,
        Guid HarnessVersionId,
        string HarnessDigest,
        Guid WorkflowSourceId,
        Guid WorkflowVersionId,
        string WorkflowDigest,
        Guid AdaptiveFollowUpSourceId,
        Guid AdaptiveFollowUpVersionId,
        string AdaptiveFollowUpDigest,
        Guid RubricSourceId,
        Guid RubricVersionId,
        string RubricDigest,
        Guid ModelSourceId,
        Guid ModelVersionId,
        string ModelDigest,
        Guid CapabilitySourceId,
        Guid CapabilityVersionId,
        string CapabilityDigest,
        Guid ReviewSourceId,
        Guid ReviewVersionId,
        string ReviewDigest,
        Guid TaskSourceId,
        Guid TaskVersionId,
        string TaskDigest,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        DateTimeOffset DeadlineUtc,
        string TimeZoneId,
        int AttemptLimit,
        int? PerAttemptDurationSeconds);

    private sealed record SaveActivityRequest(string Title, long ExpectedRevisionNumber);

    private sealed record ActivateRequest(
        Guid ExpectedRevisionId,
        long ExpectedRevisionNumber,
        string IdempotencyKey);

    private sealed record ResolvedAssessmentActor(
        AssessmentActorContext Actor,
        AssessmentActorAuthorization Authorization);
}

file sealed class NoOpAssessmentDevelopmentSourceSeeder : IAssessmentDevelopmentSourceSeeder
{
    public void EnsureOrganization(Guid organizationId)
    {
        _ = organizationId;
    }
}

