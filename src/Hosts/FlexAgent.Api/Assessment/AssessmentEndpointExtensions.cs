using System.Globalization;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static partial class AssessmentEndpointExtensions
{
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

        var grants = resolved.Authorization.PermittedActions;
        var activitiesAvailable = grants.Any(FlexAgent.Submissions.Application.EnrollmentAuthenticationPolicy.IsAdministratorDestinationGrant)
            && AssessmentAuthenticationPolicy.Evaluate(resolved.Actor, AssessmentAuthorizationActions.ReadActivity) is null;
        var myWorkAvailable = grants.Contains(
            FlexAgent.Submissions.Domain.EnrollmentAuthorizationActions.Discover,
            StringComparer.Ordinal)
            && FlexAgent.Submissions.Application.EnrollmentAuthenticationPolicy.Evaluate(
                new FlexAgent.Submissions.Application.EnrollmentActorContext(
                    resolved.Actor.Actor,
                    resolved.Actor.Organization,
                    resolved.Actor.Relationship,
                    resolved.Actor.Strength,
                    resolved.Actor.CorrelationId,
                    resolved.Actor.SourceChannel,
                    grants,
                    Guid.Empty),
                FlexAgent.Submissions.Domain.EnrollmentAuthorizationActions.Discover) is null;

        await context.Response.WriteAsJsonAsync(new
        {
            schema_version = "v1",
            actor_id = resolved.Actor.Actor.ActorId,
            organization_id = resolved.Actor.Organization.OrganizationId,
            relationship = resolved.Actor.Relationship,
            display_name = resolved.SeatedDisplayName,
            navigation = new[]
            {
                new { destination_id = "home", is_available = true },
                new { destination_id = "activities", is_available = activitiesAvailable },
                new { destination_id = "my-work", is_available = myWorkAvailable },
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
        context.Response.Headers.CacheControl = "no-store";
        var resolved = await TryActorAsync(context, coordinator, options);
        if (resolved is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!TryReadActivityListQuery(context.Request, out var numbered, out var omittedPaging))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = AssessmentFailureCodes.InvalidField });
            return;
        }

        if (omittedPaging)
        {
            var listed = await drafts.ListActivitiesAsync(resolved.Actor, context.RequestAborted);
            if (!listed.Succeeded)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = listed.OutcomeCode });
                return;
            }

            await context.Response.WriteAsJsonAsync(new
            {
                activities = listed.Value!.Select(ProjectActivitySummary),
                permitted_actions = PermittedListActions(resolved),
            });
            return;
        }

        var result = await drafts.ListActivitiesPageAsync(resolved.Actor, numbered!, context.RequestAborted);
        if (!result.Succeeded)
        {
            context.Response.StatusCode = AssessmentHttpStatus.IsAccessFailure(result.OutcomeCode)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = result.OutcomeCode });
            return;
        }

        var page = result.Value!;
        await context.Response.WriteAsJsonAsync(new
        {
            activities = page.Items.Select(ProjectActivitySummary),
            permitted_actions = PermittedListActions(resolved),
            pagination = new
            {
                mode = "numbered",
                page = page.Page,
                page_size = page.PageSize,
                total_items = page.TotalItems,
                total_pages = page.TotalPages,
            },
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
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
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

        TimingRules timingRules;
        if (string.Equals(
                request.TimingPresetId,
                AssessmentDevelopmentTimingPresets.SyntheticTimedV1,
                StringComparison.Ordinal))
        {
            timingRules = AssessmentDevelopmentTimingPresets.SyntheticTimedV1Rules();
        }
        else if (request.StartsAtUtc != default)
        {
            timingRules = new TimingRules(
                request.StartsAtUtc,
                request.EndsAtUtc,
                request.DeadlineUtc,
                string.IsNullOrWhiteSpace(request.TimeZoneId) ? "UTC" : request.TimeZoneId,
                request.AttemptLimit < 1 ? 2 : request.AttemptLimit,
                request.PerAttemptDurationSeconds,
                request.WarningApproachingRemainingSeconds,
                request.WarningImminentRemainingSeconds);
        }
        else
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
                timingRules,
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

        context.Response.StatusCode = AssessmentHttpStatus.ForDraftMutation(
            created.Succeeded,
            created.OutcomeCode,
            StatusCodes.Status201Created);
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
        IAssessmentDraftStore store,
        IAssessmentSourceCatalog catalog,
        IAssessmentBaselineStore baselines,
        IActivationBaselineDigester digester)
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
        string? verificationStatus = null;
        if (draft.HasActivatedCohort)
        {
            var sources = await catalog.LoadExactAsync(
                draft.OrganizationId,
                BaselineVerification.References(draft),
                context.RequestAborted);
            verificationStatus = BaselineVerification.Status(
                draft,
                sources,
                await LoadActivatedDigestCheckAsync(
                    baselines,
                    digester,
                    draft,
                    activityId,
                    cohort,
                    context.RequestAborted));
        }

        var capabilities = draft.Content.RequestedCapabilities;
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
            task_title = draft.Content.Task.Title,
            timing = new
            {
                time_zone_id = draft.Content.Timing.TimeZoneId,
                attempt_limit = draft.Content.Timing.AttemptLimit,
                starts_at_utc = draft.Content.Timing.StartsAtUtc,
                ends_at_utc = draft.Content.Timing.EndsAtUtc,
                deadline_utc = draft.Content.Timing.DeadlineUtc,
                per_attempt_duration_seconds = draft.Content.Timing.PerAttemptDurationSeconds,
                warning_approaching_remaining_seconds = draft.Content.Timing.WarningApproachingRemainingSeconds,
                warning_imminent_remaining_seconds = draft.Content.Timing.WarningImminentRemainingSeconds,
            },
            disabled_capabilities = DisabledCapabilityLabels(capabilities),
            cohort_id = cohort?.CohortId,
            cohort_state = cohort?.State,
            baseline_digest = cohort?.BaselineDigest,
            verification_status = verificationStatus,
            sources = new
            {
                organization_policy = ProjectSource(draft.Content.OrganizationPolicy),
                agent = ProjectSource(draft.Content.Agent),
                harness = ProjectSource(draft.Content.Harness),
                workflow = ProjectSource(draft.Content.Workflow),
                adaptive_follow_up = ProjectSource(draft.Content.AdaptiveFollowUp),
                rubric_evaluation = ProjectSource(draft.Content.Rubric),
                model_deployment = ProjectSource(draft.Content.ModelDeployment),
                capability = ProjectSource(draft.Content.CapabilityProfile),
                review_release = ProjectSource(draft.Content.ReviewRelease),
                task_submission = ProjectSource(draft.Content.Task.RequirementSource),
                knowledge = draft.Content.Knowledge.Select(ProjectSource),
            },
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
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
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
        context.Response.StatusCode = AssessmentHttpStatus.ForDraftMutation(saved.Succeeded, saved.OutcomeCode);
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
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
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
        context.Response.StatusCode = AssessmentHttpStatus.ForDraftMutation(result.Succeeded, result.OutcomeCode);
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
        if (!await EnrollmentEndpointExtensions.ValidateMutationAsync(context, antiforgery))
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
        if (request is null
            || !AssessmentActivateRequestValidator.IsValid(
                request.ExpectedRevisionId,
                request.ExpectedRevisionNumber))
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
        context.Response.StatusCode = AssessmentIdempotencyKey.StatusForActivation(outcome.Succeeded, outcome.OutcomeCode);
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
        if (!AssessmentReconcileQueryValidator.IsValid(idempotencyKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = AssessmentFailureCodes.InvalidField });
            return;
        }

        var outcome = await activation.ReconcileAsync(
            new ReconcileActivationQuery(actor, activityId, cohortId, idempotencyKey),
            context.RequestAborted);
        context.Response.StatusCode = outcome.Succeeded
            ? StatusCodes.Status200OK
            : outcome.OutcomeCode == AssessmentFailureCodes.InvalidField
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(outcome);
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
            authorization,
            session.SeatedDisplayName);
    }

    private static async Task<BaselineDigestCheck> LoadActivatedDigestCheckAsync(
        IAssessmentBaselineStore baselines,
        IActivationBaselineDigester digester,
        ActivityDraft draft,
        Guid activityId,
        AssessmentCohort? cohort,
        CancellationToken cancellationToken)
    {
        if (cohort is null
            || cohort.State != CohortStates.Activated
            || cohort.BaselineId is null
            || string.IsNullOrWhiteSpace(cohort.BaselineDigest))
        {
            return BaselineDigestCheck.Missing();
        }

        var persisted = await baselines.FindBoundAsync(
            draft.OrganizationId,
            activityId,
            cohort.CohortId,
            cancellationToken);
        if (persisted is null
            || !string.Equals(persisted.ContentDigest, cohort.BaselineDigest, StringComparison.Ordinal))
        {
            return BaselineDigestCheck.Missing();
        }

        var recomputed = digester.Digest(persisted.Document);
        return BaselineDigestCheck.Present(
            persisted.ContentDigest,
            recomputed is { Succeeded: true } ? recomputed.Value : null,
            cohort.BoundRevisionId,
            draft.RevisionId);
    }

    private static object ProjectSource(ExactSourceRef source) => new
    {
        source_id = source.SourceId,
        version_id = source.VersionId,
        content_digest = source.ContentDigest,
    };

    private static IReadOnlyList<string> DisabledCapabilityLabels(CapabilityBounds capabilities)
    {
        var labels = new List<string>();
        if (!capabilities.VoiceEnabled)
        {
            labels.Add("voice");
        }

        if (!capabilities.ToolsEnabled)
        {
            labels.Add("tools");
        }

        if (!capabilities.DynamicMemoryWritesEnabled)
        {
            labels.Add("dynamic memory writes");
        }

        if (!capabilities.SharedSessionEnabled)
        {
            labels.Add("shared session");
        }

        if (!capabilities.DirectDeploymentEnabled)
        {
            labels.Add("direct deployment");
        }

        return labels;
    }

    private static object ProjectActivitySummary(ActivityDraft draft) => new
    {
        activity_id = draft.ActivityId,
        title = draft.Content.Title,
        revision_number = draft.RevisionNumber,
        has_activated_cohort = draft.HasActivatedCohort,
        updated_at = draft.UpdatedAtUtc.ToString("O"),
    };

    private static string[] PermittedListActions(ResolvedAssessmentActor resolved) =>
        HasAction(resolved, AssessmentAuthorizationActions.CreateActivity)
            ? ["create_assessment"]
            : [];

    private static bool TryReadActivityListQuery(
        HttpRequest request,
        out NumberedActivityListRequest? numbered,
        out bool omittedPaging)
    {
        numbered = null;
        omittedPaging = !request.Query.ContainsKey("paging");
        if (omittedPaging)
        {
            return true;
        }

        if (!string.Equals(request.Query["paging"].ToString(), "numbered", StringComparison.Ordinal)
            || !TryReadOptionalInt(request.Query["page"].ToString(), out var page)
            || !TryReadOptionalInt(request.Query["page_size"].ToString(), out var pageSize)
            || !TryReadSortTerms(request.Query["sort"].ToString(), out var sort))
        {
            return false;
        }

        numbered = new NumberedActivityListRequest(page, pageSize, request.Query["q"].ToString(), sort);
        return true;
    }

    private static bool TryReadOptionalInt(string raw, out int? value)
    {
        value = null;
        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryReadSortTerms(string raw, out IReadOnlyList<ActivityListSortTerm>? sort)
    {
        sort = null;
        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        var terms = new List<ActivityListSortTerm>();
        foreach (var part in raw.Split(',', StringSplitOptions.None))
        {
            var separator = part.IndexOf(':');
            if (separator <= 0 || separator != part.LastIndexOf(':') || separator == part.Length - 1)
            {
                return false;
            }

            terms.Add(new ActivityListSortTerm(part[..separator], part[(separator + 1)..]));
        }

        sort = terms;
        return true;
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
        int? PerAttemptDurationSeconds,
        int? WarningApproachingRemainingSeconds = null,
        int? WarningImminentRemainingSeconds = null,
        string? TimingPresetId = null);

    private sealed record SaveActivityRequest(string Title, long ExpectedRevisionNumber);

    private sealed record ActivateRequest(
        Guid ExpectedRevisionId,
        long ExpectedRevisionNumber,
        string IdempotencyKey);

    private sealed record ResolvedAssessmentActor(
        AssessmentActorContext Actor,
        AssessmentActorAuthorization Authorization,
        string? SeatedDisplayName);
}

