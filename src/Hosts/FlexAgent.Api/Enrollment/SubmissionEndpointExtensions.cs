using System.Text;
using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using FlexAgent.Submissions.Infrastructure.ObjectStorage;
using Microsoft.AspNetCore.Antiforgery;

namespace FlexAgent.Api;

public static class SubmissionEndpointExtensions
{
    public static IServiceCollection AddSubmissionIntake(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var artifactSection = configuration.GetSection("ArtifactStorage");
        if (!string.IsNullOrWhiteSpace(artifactSection["ServiceUrl"]))
        {
            services.AddSingleton(new S3ArtifactStoreOptions
            {
                ServiceUrl = artifactSection["ServiceUrl"]!,
                BucketName = artifactSection["BucketName"] ?? "flex-agent-artifacts",
                AccessKeyId = artifactSection["AccessKeyId"] ?? "access_key",
                SecretAccessKey = artifactSection["SecretAccessKey"] ?? "secret_key",
                ForcePathStyle = true,
            });
            services.AddSingleton<IArtifactStore, S3ArtifactStore>();
            services.AddHostedService<ArtifactBucketInitializer>();
        }
        else
        {
            services.AddSingleton<IArtifactStore, InMemoryArtifactStore>();
        }

        services.AddSingleton<IIntakeCoordinator, IntakeCoordinator>();
        services.AddSingleton<ISubmissionQueryService, SubmissionQueryService>();
        services.AddSingleton<ISubmissionCleanupProcessor, SubmissionCleanupProcessor>();
        services.AddSingleton<IExactAcceptedVersionReader, ScopedExactAcceptedVersionReader>();

        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IFrozenSubmissionRequirementPort, FixedFrozenSubmissionRequirementPort>();
            services.AddSingleton<IMaterialPolicyPort, FixedMaterialPolicyPort>();
            services.AddSingleton<IArtifactSafetyScanner, DisabledArtifactSafetyScanner>();
            services.AddSingleton<IIntakeStore, InMemoryIntakeStore>();
            services.AddSingleton<ISubmissionVersionStore, InMemorySubmissionVersionStore>();
            services.AddSingleton<ISubmissionWorkStore, InMemorySubmissionWorkStore>();
            services.AddSingleton<ISubmissionLifecycleHoldStore, InMemoryLifecycleHoldStore>();
            services.AddSingleton<IArtifactDispositionStore, InMemoryArtifactDispositionStore>();
            services.AddSingleton<IProtectedArtifactCapabilityStore, InMemoryProtectedArtifactCapabilityStore>();
            services.AddSingleton<IActivityClosurePort, UnavailableActivityClosurePort>();
            services.AddSingleton<IAcceptedPayloadLifecyclePolicyPort, ApprovedDefaultAcceptedPayloadLifecyclePolicyPort>();
            return services;
        }

        services.AddSingleton<IFrozenSubmissionRequirementPort, AssessmentFrozenSubmissionRequirementPort>();
        services.AddSingleton<IMaterialPolicyPort, EnvironmentMaterialPolicyPort>();
        services.AddSingleton<IActivityClosurePort, UnavailableActivityClosurePort>();
        services.AddSingleton<IAcceptedPayloadLifecyclePolicyPort, ApprovedDefaultAcceptedPayloadLifecyclePolicyPort>();
        if (environment.IsProduction() || environment.IsEnvironment("Staging"))
        {
            services.AddSingleton<IArtifactSafetyScanner, UnavailableArtifactSafetyScanner>();
        }
        else
        {
            services.AddSingleton<IArtifactSafetyScanner, DisabledArtifactSafetyScanner>();
        }
        services.AddSingleton<IIntakeStore, PostgresIntakeStore>();
        services.AddSingleton<ISubmissionVersionStore, PostgresSubmissionVersionStore>();
        services.AddSingleton<ISubmissionWorkStore, PostgresSubmissionWorkStore>();
        services.AddSingleton<ISubmissionLifecycleHoldStore, PostgresLifecycleHoldStore>();
        services.AddSingleton<IArtifactDispositionStore, PostgresArtifactDispositionStore>();
        services.AddSingleton<IProtectedArtifactCapabilityStore, PostgresProtectedArtifactCapabilityStore>();
        services.AddHostedService<SubmissionCleanupHostedService>();
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
        group.MapGet("/my-work/{enrollmentId:guid}/submission/versions/{versionId:guid}/items/{itemId:guid}/preview", GetItemPreview);
        group.MapGet("/my-work/{enrollmentId:guid}/submission/versions/{versionId:guid}/items/{itemId:guid}/download", GetItemDownload);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake", BeginIntake);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}/items", CompleteItem);
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
        await Results.Json(MapMyWork(result.Value)).ExecuteAsync(context);
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
        await Results.Json(MapAcceptedVersion(result.Value)).ExecuteAsync(context);
    }

    private static async Task GetItemPreview(
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

    private static async Task GetItemDownload(
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

    private static Task BeginIntake(
        HttpContext context,
        Guid enrollmentId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<BeginIntakeCommandV2>(context);
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

    private static Task CompleteItem(
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
            if (body is null
                || !string.Equals(body.SchemaVersion, "v2", StringComparison.Ordinal)
                || EnrollmentIdempotencyKey.Validate(body.IdempotencyKey) is not null
                || !EnrollmentHttpLimits.IsValidAccommodationRevision(body.ExpectedRevision)
                || string.IsNullOrWhiteSpace(body.Content))
            {
                return null;
            }

            var content = Encoding.UTF8.GetBytes(body.Content);
            var digest = MaterialContentValidator.Sha256Hex(content);
            var trusted = SubmissionCommandDigest.Compute(
                IntakeOperationKinds.CompleteItem,
                actor.Organization.OrganizationId.ToString("D"),
                enrollmentId.ToString("D"),
                intakeId.ToString("D"),
                body.ExpectedRevision.ToString(),
                digest);
            return await coordinator.CompleteItemAsync(new CompleteIntakeItemCommand(
                actor,
                enrollmentId,
                intakeId,
                Guid.Empty,
                body.Category,
                body.Filename,
                body.DeclaredMimeType,
                content,
                digest,
                body.ExpectedRevision,
                body.IdempotencyKey,
                trusted));
        });

    private static Task CancelIntake(
        HttpContext context,
        Guid enrollmentId,
        Guid intakeId,
        IIntakeCoordinator coordinator,
        IAntiforgery antiforgery) =>
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<IntakeRevisionCommandV2>(context);
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
        MutateAsync(context, antiforgery, async actor =>
        {
            var body = await EnrollmentEndpointExtensions.TryReadCommandAsync<IntakeRevisionCommandV2>(context);
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
        await Results.Json(MapOutcome(outcome), statusCode: status).ExecuteAsync(context);
    }

    private static IntakeMutationOutcomeV2 MapOutcome(IntakeMutationOutcome outcome) =>
        new(
            "v2",
            outcome.Succeeded,
            outcome.OutcomeCode,
            outcome.IntakeId,
            outcome.SubmissionId,
            outcome.Status,
            outcome.Revision,
            outcome.VersionId,
            outcome.VersionNumber,
            MapActions(outcome.Status, outcome.VersionNumber is not null));

    private static IReadOnlyList<string> MapActions(string? status, bool hasVersion) =>
        SubmissionLifecycle.PermittedActions(
            true,
            status,
            hasVersion);

    private static MyWorkSubmissionV2 MapMyWork(MyWorkSubmissionProjection projection) =>
        new(
            "v2",
            projection.EnrollmentId,
            projection.EnrollmentStatus,
            projection.IntakeAvailable,
            projection.UnavailableReason,
            projection.Requirements is null ? null : MapRequirements(projection.Requirements),
            projection.ActiveIntake is null ? null : MapIntake(projection.ActiveIntake),
            projection.VersionHistory.Select(version => new AcceptedVersionSummaryV2(
                version.VersionId,
                version.VersionNumber,
                EnrollmentEndpointExtensions.FormatUtc(version.AcceptedAtUtc)!,
                version.ItemCount)).ToArray(),
            projection.PermittedActions);

    private static MaterialRequirementsV2 MapRequirements(NormalizedMaterialPolicy policy) =>
        new(
            policy.ContractVersion,
            policy.MaxAttachmentCount,
            policy.MaxAttachmentAggregateBytes,
            policy.Categories.FirstOrDefault(category => category.Category == MaterialCategories.DirectText)?.MaxBytes ?? 1_048_576,
            policy.ScannerMode == MaterialScannerMode.Required ? "required" : "disabled_by_approved_policy",
            policy.Categories.Select(category => new MaterialCategoryLimitV2(
                category.Category,
                category.Available,
                category.MaxBytes)).ToArray());

    private static SubmissionIntakeV2 MapIntake(SubmissionIntakeProjection intake) =>
        new(
            intake.IntakeId,
            intake.SubmissionId,
            intake.Status,
            intake.Revision,
            EnrollmentEndpointExtensions.FormatUtc(intake.CreatedAtUtc)!,
            EnrollmentEndpointExtensions.FormatUtc(intake.UpdatedAtUtc)!,
            EnrollmentEndpointExtensions.FormatUtc(intake.CompleteReceiptAtUtc),
            intake.Items.Select(item => new SubmissionIntakeItemV2(
                item.ItemId,
                item.Category,
                item.Filename,
                item.ByteCount,
                item.ReceiptState)).ToArray(),
            intake.PermittedActions);

    private static AcceptedVersionDetailV2 MapAcceptedVersion(AcceptedVersionDetail detail) =>
        new(
            "v2",
            detail.Summary.VersionId,
            detail.Summary.VersionNumber,
            EnrollmentEndpointExtensions.FormatUtc(detail.Summary.AcceptedAtUtc)!,
            detail.Items.Select(item => new AcceptedVersionItemV2(
                item.ItemId,
                item.Category,
                item.Filename,
                item.ByteCount,
                item.PreviewAuthorized,
                item.DownloadAuthorized)).ToArray(),
            detail.PermittedActions);
}

public sealed class ArtifactBucketInitializer(IArtifactStore store) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (store is S3ArtifactStore s3)
        {
            await s3.EnsureBucketAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class SubmissionCleanupHostedService(
    ISubmissionCleanupProcessor processor,
    ILogger<SubmissionCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await processor.TryProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Submission cleanup processing failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

internal sealed class ScopedExactAcceptedVersionReader(ISubmissionVersionStore versions) : IExactAcceptedVersionReader
{
    public async Task<AcceptedSubmissionVersion?> GetExactAsync(
        SubmissionParentScope scope,
        Guid versionId,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var version = await versions.FindVersionAsync(scope.OrganizationId, versionId, null, cancellationToken);
        if (version is null
            || version.Scope.EnrollmentId != scope.EnrollmentId
            || version.Scope.ParticipantActorId != scope.ParticipantActorId)
        {
            return null;
        }

        return version;
    }
}
