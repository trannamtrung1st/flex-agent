using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using FlexAgent.Submissions.Infrastructure.ObjectStorage;
using Microsoft.Extensions.Options;

namespace FlexAgent.Api;

public static partial class SubmissionEndpointExtensions
{
    public static IServiceCollection AddSubmissionIntake(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<S3ArtifactStoreOptions>()
            .Configure<IConfiguration>(ArtifactStorageOptionsBinding.Configure)
            .Validate<IHostEnvironment>(
                ArtifactStorageOptionsBinding.IsValidWhenConfigured,
                "ArtifactStorage is configured but missing required production credentials or bucket name.")
            .ValidateOnStart();

        var artifactOptions = ArtifactStorageOptionsBinding.CreateSnapshot(configuration);
        if (!string.IsNullOrWhiteSpace(artifactOptions.ServiceUrl))
        {
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<S3ArtifactStoreOptions>>().Value);
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
            services.AddSingleton<InMemorySubmissionIdentityStore>();
            services.AddSingleton<IIntakeStore, InMemoryIntakeStore>();
            services.AddSingleton<ISubmissionVersionStore, InMemorySubmissionVersionStore>();
            services.AddSingleton<ISubmissionWorkStore, InMemorySubmissionWorkStore>();
            services.AddSingleton<ISubmissionLifecycleHoldStore, InMemoryLifecycleHoldStore>();
            services.AddSingleton<IArtifactDispositionStore, InMemoryArtifactDispositionStore>();
            services.AddSingleton<IProtectedArtifactCapabilityStore, InMemoryProtectedArtifactCapabilityStore>();
            services.AddSingleton<IActivityClosurePort, UnavailableActivityClosurePort>();
            services.AddSingleton<IAcceptedPayloadLifecyclePolicyPort, ApprovedDefaultAcceptedPayloadLifecyclePolicyPort>();
            services.AddSingleton<IAcceptedCleanupScanStore, InMemoryAcceptedCleanupScanStore>();
            return services;
        }

        services.AddSingleton<IFrozenSubmissionRequirementPort, AssessmentFrozenSubmissionRequirementPort>();
        services.AddSingleton<IMaterialPolicyPort, EnvironmentMaterialPolicyPort>();
        services.AddSingleton<IActivityClosurePort, UnavailableActivityClosurePort>();
        services.AddSingleton<IAcceptedPayloadLifecyclePolicyPort, ApprovedDefaultAcceptedPayloadLifecyclePolicyPort>();
        services.AddSingleton<IAcceptedCleanupScanStore, PostgresAcceptedCleanupScanStore>();
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
    public Task<AcceptedSubmissionVersion?> GetExactAsync(
        SubmissionParentScope scope,
        Guid versionId,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commitTransaction);
        var transaction = commitTransaction as IEnrollmentTransaction
            ?? throw new InvalidOperationException("commit.transaction.required");
        return GetExactCoreAsync(scope, versionId, transaction, cancellationToken);
    }

    private async Task<AcceptedSubmissionVersion?> GetExactCoreAsync(
        SubmissionParentScope scope,
        Guid versionId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        var version = await versions.FindVersionAsync(scope.OrganizationId, versionId, transaction, cancellationToken);
        if (version is null
            || version.Scope.EnrollmentId != scope.EnrollmentId
            || version.Scope.ParticipantActorId != scope.ParticipantActorId)
        {
            return null;
        }

        return version;
    }
}
