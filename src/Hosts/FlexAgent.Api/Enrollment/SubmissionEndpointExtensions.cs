using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class SubmissionEndpointExtensions
{
    public static IServiceCollection AddSubmissionIntake(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IIntakeCoordinator, IntakeCoordinator>();
        services.AddSingleton<ISubmissionQueryService, SubmissionQueryService>();

        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IFrozenSubmissionRequirementPort, FixedFrozenSubmissionRequirementPort>();
            services.AddSingleton<IMaterialPolicyPort, FixedMaterialPolicyPort>();
            services.AddSingleton<IArtifactSafetyScanner, DisabledArtifactSafetyScanner>();
            services.AddSingleton<IIntakeStore, InMemoryIntakeStore>();
            services.AddSingleton<ISubmissionVersionStore, InMemorySubmissionVersionStore>();
            return services;
        }

        services.AddSingleton<IFrozenSubmissionRequirementPort, UnavailableFrozenSubmissionRequirementPort>();
        services.AddSingleton<IMaterialPolicyPort, UnavailableMaterialPolicyPort>();
        services.AddSingleton<IArtifactSafetyScanner, UnavailableArtifactSafetyScanner>();
        services.AddSingleton<IIntakeStore, PostgresIntakeStore>();
        services.AddSingleton<ISubmissionVersionStore, PostgresSubmissionVersionStore>();
        return services;
    }

    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints.ServiceProvider.GetService<IIntakeCoordinator>() is null)
        {
            return endpoints;
        }

        var group = endpoints.MapGroup("/v2/assessment");
        group.MapGet("/my-work/{enrollmentId:guid}/submission", GetMyWorkSubmission);
        group.MapGet("/my-work/{enrollmentId:guid}/submission/versions/{versionId:guid}", GetAcceptedVersion);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake", BeginIntake);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}/cancel", CancelIntake);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}/finalize", FinalizeIntake);
        return endpoints;
    }

    private static async Task GetMyWorkSubmission(
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
        await Results.Json(result.Value).ExecuteAsync(context);
    }

    private static async Task GetAcceptedVersion(
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
        await Results.Json(result.Value).ExecuteAsync(context);
    }

    private static Task BeginIntake(
        HttpContext context,
        Guid enrollmentId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, enrollmentId, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<BeginIntakeCommandBody>(context);
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null)
            {
                return null;
            }

            var digest = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Begin,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"));
            return await coordinator.BeginAsync(new BeginIntakeCommand(
                actor,
                enrollmentId,
                body.IdempotencyKey,
                digest));
        });

    private static Task CancelIntake(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, enrollmentId, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<CancelIntakeCommandBody>(context);
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
                || !EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision))
            {
                return null;
            }

            var digest = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Cancel,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                body.ExpectedRevision.ToString());
            return await coordinator.CancelAsync(new CancelIntakeCommand(
                actor,
                enrollmentId,
                intakeId,
                body.ExpectedRevision,
                body.IdempotencyKey,
                digest));
        });

    private static Task FinalizeIntake(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, enrollmentId, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<FinalizeIntakeCommandBody>(context);
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
                || !EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision))
            {
                return null;
            }

            var digest = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.Finalize,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                body.ExpectedRevision.ToString());
            return await coordinator.FinalizeAsync(new FinalizeIntakeCommand(
                actor,
                enrollmentId,
                intakeId,
                body.ExpectedRevision,
                body.IdempotencyKey,
                digest));
        });

    private static async Task MutateAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        Guid enrollmentId,
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

        var status = outcome.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status409Conflict;
        context.Response.Headers.CacheControl = "no-store";
        await Results.Json(outcome, statusCode: status).ExecuteAsync(context);
    }

    private sealed record BeginIntakeCommandBody(string SchemaVersion, string IdempotencyKey);

    private sealed record CancelIntakeCommandBody(string SchemaVersion, string IdempotencyKey, long ExpectedRevision);

    private sealed record FinalizeIntakeCommandBody(string SchemaVersion, string IdempotencyKey, long ExpectedRevision);
}
