using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenAi;
using FlexAgent.Sessions.OpenRouter;
using Npgsql;

namespace FlexAgent.Worker;

public sealed class WorkerRuntimeCapabilities
{
    public bool DurableWorkClaimingEnabled { get; init; }

    public bool TimerPollingEnabled { get; init; }

    public string WorkloadIdentityProfile { get; init; } = WorkloadIdentityProfiles.SyntheticConfiguredActor;

    public string ModelExecutionAdapter { get; init; } = "fail_closed";

    public bool ModelExecutionQualified { get; init; }

    public string ModelExecutionQualificationScope { get; init; } = string.Empty;
}

internal static class WorkerDurableWorkSampling
{
    public static void AddDurableWorkSampling(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<ISessionRuntimeTelemetrySink, MeterSessionRuntimeTelemetrySink>();
        services.AddSingleton<ISessionRuntimeTelemetry>(sp =>
            new SessionRuntimeTelemetry(sp.GetRequiredService<ISessionRuntimeTelemetrySink>()));
        var connectionString = configuration.GetConnectionString("Sessions");
        var invocationProcessingRequested = configuration.GetValue(
            "Sessions:InvocationProcessing:Enabled",
            false);
        var timerPollingRequested = configuration.GetValue("Sessions:TimerPolling:Enabled", false);
        var protectedLaneRequested = invocationProcessingRequested || timerPollingRequested;
        var identityProfile = ResolveWorkloadIdentityProfile(
            configuration,
            environment,
            protectedLaneRequested);
        var productionAuthenticated = string.Equals(
            identityProfile,
            WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
            StringComparison.Ordinal);
        if (invocationProcessingRequested && !IsSyntheticHostProfile(environment) && !productionAuthenticated)
        {
            throw new InvalidOperationException(
                "Sessions:InvocationProcessing:Enabled requires a configured OAuth workload identity profile and cannot be enabled without it.");
        }

        if (timerPollingRequested && !IsSyntheticHostProfile(environment) && !productionAuthenticated)
        {
            throw new InvalidOperationException(
                "Sessions:TimerPolling:Enabled requires a configured OAuth workload identity profile and cannot be enabled without it.");
        }

        var invocationProcessingEnabled = invocationProcessingRequested
            && (IsSyntheticHostProfile(environment) || productionAuthenticated);
        var timerPollingEnabled = timerPollingRequested
            && (IsSyntheticHostProfile(environment) || productionAuthenticated);
        var modelExecution = ComposeModelExecution(configuration, environment);
        RegisterWorkloadIdentitySource(
            services,
            configuration,
            identityProfile,
            protectedLaneRequested && (invocationProcessingEnabled || timerPollingEnabled),
            !string.IsNullOrWhiteSpace(connectionString));
        services.AddSingleton<IRecoverableAuthorityGate>(_ =>
        {
            var gate = new RecoverableAuthorityGate();
            if (!protectedLaneRequested || !invocationProcessingEnabled && !timerPollingEnabled)
            {
                gate.SetState(RecoverableAuthorityStates.Ready);
            }
            else if (string.Equals(
                identityProfile,
                WorkloadIdentityProfiles.SyntheticConfiguredActor,
                StringComparison.Ordinal))
            {
                gate.SetState(RecoverableAuthorityStates.Ready);
            }
            else
            {
                gate.SetState(RecoverableAuthorityStates.Authenticating);
            }

            return gate;
        });
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IDurableInvocationWorkStore>(UnknownDurableInvocationWorkStore.Instance);
            services.AddSingleton<IDurableInvocationWorkProcessor, IdleDurableInvocationWorkProcessor>();
            services.AddSingleton<IDurableTimerFireProcessor, IdleDurableTimerFireProcessor>();
            services.AddSingleton(new WorkerRuntimeCapabilities
            {
                DurableWorkClaimingEnabled = false,
                TimerPollingEnabled = false,
                WorkloadIdentityProfile = identityProfile,
            });
        }
        else
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
            services.AddSingleton<PostgresConnectionAccessor>();
            services.AddSingleton<PostgresSessionRuntimeRepository>();
            services.AddSingleton<ITrustedSessionBindingSource, PostgresTrustedSessionBindingSource>();
            services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
            services.AddSingleton<ICommitAuthorizationKernel>(sp =>
                (ICommitAuthorizationKernel)sp.GetRequiredService<IAuthorizationKernel>());
            if (invocationProcessingEnabled)
            {
                var workerActorId = RequireWorkerServiceActorId(configuration);
                services.AddSingleton(CreateInvocationWorkSettings(workerActorId, modelExecution));
                services.AddSingleton<IDurableInvocationWorkStore>(sp =>
                    new PostgresDurableInvocationWorkStore(
                        sp.GetRequiredService<PostgresConnectionAccessor>(),
                        sp.GetRequiredService<DurableInvocationWorkSettings>().ServiceActor,
                        sp.GetRequiredService<ICommitAuthorizationKernel>(),
                        sp.GetRequiredService<IAuthenticatedWorkloadContextSource>()));
                services.AddSingleton<IPublishAgentResponseFragmentHandler>(sp =>
                    new PublishAgentResponseFragmentHandler(sp.GetRequiredService<ISessionRuntimeTelemetry>()));
                services.AddSingleton<ICompleteInvocationHandler>(sp =>
                    new CompleteInvocationHandler(sp.GetRequiredService<ISessionRuntimeTelemetry>()));
                services.AddSingleton<IModelExecutionPort>(_ => modelExecution.Port);
                services.AddSingleton(sp =>
                    new PostgresModelProviderAttemptProvenanceWriter(
                        sp.GetRequiredService<PostgresConnectionAccessor>(),
                        sp.GetRequiredService<DurableInvocationWorkSettings>().ServiceActor,
                        sp.GetRequiredService<ICommitAuthorizationKernel>(),
                        sp.GetRequiredService<IAuthenticatedWorkloadContextSource>()));
                services.AddSingleton<IProviderRequestAdmissionPort>(sp =>
                    sp.GetRequiredService<PostgresModelProviderAttemptProvenanceWriter>());
                services.AddSingleton<IModelProviderAttemptProvenanceWriter>(sp =>
                    sp.GetRequiredService<PostgresModelProviderAttemptProvenanceWriter>());
                services.AddSingleton<PostgresPublishAgentResponseCoordinator>();
                services.AddSingleton<IAgentResponsePublicationPersistPort>(sp =>
                    sp.GetRequiredService<PostgresPublishAgentResponseCoordinator>());
                services.AddSingleton<IInvocationWorkSessionGateway, PostgresInvocationWorkSessionGateway>();
                services.AddSingleton<IDurableInvocationWorkProcessor, DurableInvocationWorkProcessor>();
            }
            else
            {
                services.AddSingleton<IDurableInvocationWorkStore>(UnknownDurableInvocationWorkStore.Instance);
                services.AddSingleton<IDurableInvocationWorkProcessor, IdleDurableInvocationWorkProcessor>();
            }

            if (timerPollingEnabled)
            {
                var workerActorId = RequireWorkerServiceActorId(configuration);
                services.AddSingleton<IDueTimerFirePort, PostgresFireDueTimerCoordinator>();
                services.AddSingleton(CreateTimerFireSettings(workerActorId));
                services.AddSingleton<IDurableTimerFireProcessor, DurableTimerFireProcessor>();
            }
            else
            {
                services.AddSingleton<IDurableTimerFireProcessor, IdleDurableTimerFireProcessor>();
            }

            services.AddSingleton(new WorkerRuntimeCapabilities
            {
                DurableWorkClaimingEnabled = invocationProcessingEnabled,
                TimerPollingEnabled = timerPollingEnabled,
                WorkloadIdentityProfile = identityProfile,
                ModelExecutionAdapter = invocationProcessingEnabled
                    ? modelExecution.Adapter
                    : "fail_closed",
                ModelExecutionQualified = invocationProcessingEnabled && modelExecution.Qualified,
                ModelExecutionQualificationScope = invocationProcessingEnabled
                    ? modelExecution.QualificationScope
                    : string.Empty,
            });
        }

        services.AddSingleton<IDurableWorkBacklogSampler>(sp =>
            new DurableWorkBacklogSampler(
                sp.GetRequiredService<IDurableInvocationWorkStore>(),
                sp.GetRequiredService<ISessionRuntimeTelemetry>()));
    }

    private static bool IsSyntheticHostProfile(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("Testing");

    private static string ResolveWorkloadIdentityProfile(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool protectedLaneRequested)
    {
        var configured = configuration["WorkloadIdentity:Profile"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            return IsSyntheticHostProfile(environment)
                ? WorkloadIdentityProfiles.SyntheticConfiguredActor
                : string.Empty;
        }

        if (string.Equals(configured, WorkloadIdentityProfiles.SyntheticConfiguredActor, StringComparison.Ordinal))
        {
            if (!IsSyntheticHostProfile(environment) && protectedLaneRequested)
            {
                throw new InvalidOperationException(
                    "WorkloadIdentity:Profile synthetic.configured_actor cannot be selected outside Development/Testing.");
            }

            return WorkloadIdentityProfiles.SyntheticConfiguredActor;
        }

        if (string.Equals(configured, WorkloadIdentityProfiles.OAuthClientCredentialsJwt, StringComparison.Ordinal))
        {
            RequireOauthProfile(configuration, environment);
            return WorkloadIdentityProfiles.OAuthClientCredentialsJwt;
        }

        throw new InvalidOperationException("WorkloadIdentity:Profile is not a supported authentication profile.");
    }

    private static void RequireOauthProfile(IConfiguration configuration, IHostEnvironment environment)
    {
        var required = new[]
        {
            "WorkloadIdentity:Issuer",
            "WorkloadIdentity:Audience",
            "WorkloadIdentity:Subject",
            "WorkloadIdentity:ClientId",
            "WorkloadIdentity:TokenEndpoint",
            "WorkloadIdentity:JwksUri",
            "WorkloadIdentity:SecretDirectory",
            "WorkloadIdentity:ClientSecretName",
        };
        if (required.Any(key => string.IsNullOrWhiteSpace(configuration[key])))
        {
            throw new InvalidOperationException(
                "OAuth workload identity requires issuer, audience, subject, client id, token endpoint, JWKS URI, and a mounted client-secret file.");
        }

        RequireHttpsAbsoluteUri(
            configuration["WorkloadIdentity:TokenEndpoint"],
            "WorkloadIdentity:TokenEndpoint",
            environment);
        RequireHttpsAbsoluteUri(
            configuration["WorkloadIdentity:JwksUri"],
            "WorkloadIdentity:JwksUri",
            environment);

        var secretDirectory = configuration["WorkloadIdentity:SecretDirectory"]!;
        var secretName = configuration["WorkloadIdentity:ClientSecretName"]!;
        var secretPath = Path.Combine(secretDirectory, secretName);
        if (!File.Exists(secretPath))
        {
            throw new InvalidOperationException(
                "OAuth workload identity requires the client secret to be present on the mounted-file SecretSource.");
        }
    }

    private static void RequireHttpsAbsoluteUri(string? value, string settingName, IHostEnvironment environment)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException($"{settingName} must be an absolute URI.");
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return;
        }

        if (IsSyntheticHostProfile(environment) && uri.Scheme == Uri.UriSchemeHttp)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{settingName} must be an absolute https URI outside Development/Testing.");
    }

    private static void RegisterWorkloadIdentitySource(
        IServiceCollection services,
        IConfiguration configuration,
        string identityProfile,
        bool protectedLaneEnabled,
        bool postgresConfigured)
    {
        if (string.Equals(identityProfile, WorkloadIdentityProfiles.OAuthClientCredentialsJwt, StringComparison.Ordinal))
        {
            services.AddSingleton<ISecretSource>(_ =>
                new MountedFileSecretSource(configuration["WorkloadIdentity:SecretDirectory"]!));
            if (!postgresConfigured)
            {
                services.AddSingleton<IAuthenticatedWorkloadContextSource>(
                    StaticUnavailableWorkloadIdentitySource.Instance);
                return;
            }

            var expectedActorId = RequireWorkerServiceActorId(configuration);
            services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(2) });
            services.AddSingleton<IWorkloadTokenClient>(sp =>
                new HttpWorkloadTokenClient(sp.GetRequiredService<HttpClient>()));
            services.AddSingleton<IJwksKeySource>(sp =>
                new CachedJwksKeySource(
                    sp.GetRequiredService<HttpClient>(),
                    TimeProvider.System,
                    TimeSpan.FromMinutes(10)));
            services.AddSingleton<IAuthenticatedWorkloadContextSource>(sp =>
                new OAuthWorkloadIdentitySource(
                    sp.GetRequiredService<ISecretSource>(),
                    sp.GetRequiredService<IWorkloadTokenClient>(),
                    sp.GetRequiredService<IJwksKeySource>(),
                    sp.GetRequiredService<PostgresConnectionAccessor>(),
                    WorkloadJwtValidationProfile.Reference(
                        configuration["WorkloadIdentity:Issuer"]!,
                        configuration["WorkloadIdentity:Audience"]!,
                        configuration["WorkloadIdentity:Subject"]!,
                        configuration["WorkloadIdentity:ClientId"]),
                    configuration["WorkloadIdentity:TokenEndpoint"]!,
                    configuration["WorkloadIdentity:JwksUri"]!,
                    configuration["WorkloadIdentity:ClientSecretName"]!,
                    expectedActorId,
                    TimeProvider.System,
                    TimeSpan.FromSeconds(60),
                    sp.GetRequiredService<IRecoverableAuthorityGate>()));
            services.AddHostedService<WorkloadIdentityRefreshService>();
            return;
        }

        if (protectedLaneEnabled
            && string.Equals(identityProfile, WorkloadIdentityProfiles.SyntheticConfiguredActor, StringComparison.Ordinal))
        {
            var actorId = RequireWorkerServiceActorId(configuration);
            services.AddSingleton<IAuthenticatedWorkloadContextSource>(
                new ConfiguredActorWorkloadIdentitySource(actorId));
            return;
        }

        services.AddSingleton<IAuthenticatedWorkloadContextSource>(
            StaticUnavailableWorkloadIdentitySource.Instance);
    }

    private static DurableInvocationWorkSettings CreateInvocationWorkSettings(
        Guid workerActorId,
        WorkerModelExecutionComposition modelExecution)
    {
        return new DurableInvocationWorkSettings(
            new TrustedRuntimeActor(workerActorId, "worker.session_runtime"),
            "worker.session_runtime",
            65_536,
            InstalledProfiles: modelExecution.Profiles,
            CredentialCatalog: modelExecution.Catalog);
    }

    private static WorkerModelExecutionComposition ComposeModelExecution(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var adapter = configuration["Sessions:ModelExecution:Adapter"] ?? "fail_closed";
        var qualified = configuration.GetValue("Sessions:ModelExecution:Qualified", false);
        if (string.Equals(adapter, "openrouter", StringComparison.Ordinal))
        {
            return ComposeOpenRouter(configuration, environment, qualified);
        }

        if (!string.Equals(adapter, "direct_openai", StringComparison.Ordinal) || !qualified)
        {
            return WorkerModelExecutionComposition.FailClosed(adapter);
        }

        var profilesPath = configuration["Sessions:ModelExecution:InstalledProfilesPath"];
        var secretDirectory = configuration["Sessions:ModelExecution:SecretDirectory"];
        var catalogPath = configuration["Sessions:ModelExecution:CredentialCatalogPath"];
        if (string.IsNullOrWhiteSpace(profilesPath)
            || string.IsNullOrWhiteSpace(secretDirectory)
            || string.IsNullOrWhiteSpace(catalogPath)
            || !File.Exists(profilesPath)
            || !File.Exists(catalogPath)
            || !Directory.Exists(secretDirectory))
        {
            return WorkerModelExecutionComposition.FailClosed("direct_openai");
        }

        try
        {
            var profiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
            if (profiles.Length == 0
                || profiles.Any(profile =>
                    !string.Equals(profile.AdapterKind, ModelDeploymentAdapterKinds.DirectOpenAi, StringComparison.Ordinal)))
            {
                return WorkerModelExecutionComposition.FailClosed("direct_openai");
            }

            var catalog = InstalledCredentialCatalogFile.Load(catalogPath);
            var secrets = new MountedFileProviderSecretSource(secretDirectory);
            var registry = new InMemoryInstalledModelDeploymentProfileRegistry(profiles);
            return new WorkerModelExecutionComposition(
                new DirectOpenAiModelExecutionAdapter(registry, catalog, secrets),
                registry,
                catalog,
                "direct_openai",
                true);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or ArgumentException or FormatException or UriFormatException)
        {
            return WorkerModelExecutionComposition.FailClosed("direct_openai");
        }
    }

    private static WorkerModelExecutionComposition ComposeOpenRouter(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool qualified)
    {
        var scope = configuration["Sessions:ModelExecution:QualificationScope"];
        var syntheticDataPolicyAccepted = configuration.GetValue(
            "Sessions:ModelExecution:SyntheticDataPolicyAccepted",
            false);
        if (!qualified
            || !syntheticDataPolicyAccepted
            || !string.Equals(scope, OpenRouterAdapterContracts.QualificationScope, StringComparison.Ordinal)
            || (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")))
        {
            return WorkerModelExecutionComposition.FailClosed("openrouter", OpenRouterAdapterContracts.QualificationScope);
        }

        var profilesPath = configuration["Sessions:ModelExecution:InstalledProfilesPath"];
        var secretDirectory = configuration["Sessions:ModelExecution:SecretDirectory"];
        var catalogPath = configuration["Sessions:ModelExecution:CredentialCatalogPath"];
        var configurationsPath = configuration["Sessions:ModelExecution:OpenRouterConfigurationsPath"];
        if (string.IsNullOrWhiteSpace(profilesPath)
            || string.IsNullOrWhiteSpace(secretDirectory)
            || string.IsNullOrWhiteSpace(catalogPath)
            || string.IsNullOrWhiteSpace(configurationsPath)
            || !File.Exists(profilesPath)
            || !File.Exists(catalogPath)
            || !File.Exists(configurationsPath)
            || !Directory.Exists(secretDirectory)
            || !UnixOwnerOnlyMountedFileProviderSecretSource.PlatformSupportsUnixModes()
            || !UnixOwnerOnlyMountedFileProviderSecretSource.HasOwnerOnlyDirectoryMode(secretDirectory))
        {
            return WorkerModelExecutionComposition.FailClosed("openrouter", OpenRouterAdapterContracts.QualificationScope);
        }

        try
        {
            var profiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
            if (profiles.Length == 0
                || profiles.Any(profile =>
                    !string.Equals(profile.AdapterKind, ModelDeploymentAdapterKinds.OpenRouter, StringComparison.Ordinal)))
            {
                return WorkerModelExecutionComposition.FailClosed("openrouter", OpenRouterAdapterContracts.QualificationScope);
            }

            var loaded = OpenRouterInstalledConfigurationFile.Load(configurationsPath, profiles);
            if (loaded.Length != profiles.Length)
            {
                return WorkerModelExecutionComposition.FailClosed("openrouter", OpenRouterAdapterContracts.QualificationScope);
            }

            var catalog = InstalledCredentialCatalogFile.Load(catalogPath);
            var secrets = new UnixOwnerOnlyMountedFileProviderSecretSource(secretDirectory);
            var registry = new InMemoryInstalledModelDeploymentProfileRegistry(profiles);
            var configurations = new InMemoryOpenRouterInstalledConfigurationRegistry(loaded);
            return new WorkerModelExecutionComposition(
                new OpenRouterModelExecutionAdapter(registry, catalog, secrets, configurations, syntheticDataPolicyAccepted: true),
                registry,
                catalog,
                "openrouter",
                true,
                OpenRouterAdapterContracts.QualificationScope);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or ArgumentException or FormatException or UriFormatException or InvalidOperationException)
        {
            return WorkerModelExecutionComposition.FailClosed("openrouter", OpenRouterAdapterContracts.QualificationScope);
        }
    }

    private static DurableTimerFireSettings CreateTimerFireSettings(Guid workerActorId) =>
        new(
            new TrustedRuntimeActor(workerActorId, "worker.session_runtime"),
            "worker.session_runtime");

    private static Guid RequireWorkerServiceActorId(IConfiguration configuration)
    {
        var configured = configuration["Sessions:WorkerServiceActorId"];
        if (!Guid.TryParse(configured, out var parsed) || parsed == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Sessions:WorkerServiceActorId must be an explicit non-empty actor id when timer polling or Invocation processing is enabled. The compiled default is not used for a live protected lane, and the actor must already exist in IdentityAccess.");
        }

        return parsed;
    }
}

internal sealed record WorkerModelExecutionComposition(
    IModelExecutionPort Port,
    IInstalledModelDeploymentProfileRegistry Profiles,
    IModelDeploymentCredentialCatalog Catalog,
    string Adapter,
    bool Qualified,
    string QualificationScope = "")
{
    public static WorkerModelExecutionComposition FailClosed(string adapter, string qualificationScope = "") =>
        new(
            FailClosedModelExecutionPort.Instance,
            new InMemoryInstalledModelDeploymentProfileRegistry(),
            new InMemoryModelDeploymentCredentialCatalog(),
            string.IsNullOrWhiteSpace(adapter) ? "fail_closed" : adapter,
            false,
            qualificationScope);
}
