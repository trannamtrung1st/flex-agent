using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Worker;

public sealed class WorkerRuntimeCapabilities
{
    public bool DurableWorkClaimingEnabled { get; init; }

    public bool TimerPollingEnabled { get; init; }
}

internal static class WorkerDurableWorkSampling
{
    internal static readonly Guid DefaultWorkerServiceActorId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public static void AddDurableWorkSampling(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ISessionRuntimeTelemetrySink, MeterSessionRuntimeTelemetrySink>();
        services.AddSingleton<ISessionRuntimeTelemetry>(sp =>
            new SessionRuntimeTelemetry(sp.GetRequiredService<ISessionRuntimeTelemetrySink>()));
        var connectionString = configuration.GetConnectionString("Sessions");
        var invocationProcessingEnabled = configuration.GetValue(
            "Sessions:InvocationProcessing:Enabled",
            false);
        var timerPollingEnabled = configuration.GetValue("Sessions:TimerPolling:Enabled", false);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IDurableInvocationWorkStore>(UnknownDurableInvocationWorkStore.Instance);
            services.AddSingleton<IDurableInvocationWorkProcessor, IdleDurableInvocationWorkProcessor>();
            services.AddSingleton<IDurableTimerFireProcessor, IdleDurableTimerFireProcessor>();
            services.AddSingleton(new WorkerRuntimeCapabilities
            {
                DurableWorkClaimingEnabled = false,
                TimerPollingEnabled = false,
            });
        }
        else
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
            services.AddSingleton<PostgresConnectionAccessor>();
            services.AddSingleton<PostgresSessionRuntimeRepository>();
            services.AddSingleton<IDurableInvocationWorkStore, PostgresDurableInvocationWorkStore>();
            services.AddSingleton<ITrustedSessionBindingSource, PostgresTrustedSessionBindingSource>();
            services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
            services.AddSingleton<ICommitAuthorizationKernel>(sp =>
                (ICommitAuthorizationKernel)sp.GetRequiredService<IAuthorizationKernel>());
            if (invocationProcessingEnabled)
            {
                services.AddSingleton<IPublishAgentResponseFragmentHandler>(sp =>
                    new PublishAgentResponseFragmentHandler(sp.GetRequiredService<ISessionRuntimeTelemetry>()));
                services.AddSingleton<ICompleteInvocationHandler>(sp =>
                    new CompleteInvocationHandler(sp.GetRequiredService<ISessionRuntimeTelemetry>()));
                services.AddSingleton<IModelExecutionPort>(_ => FailClosedModelExecutionPort.Instance);
                services.AddSingleton(CreateInvocationWorkSettings(configuration));
                services.AddSingleton<PostgresPublishAgentResponseCoordinator>();
                services.AddSingleton<IAgentResponsePublicationPersistPort>(sp =>
                    sp.GetRequiredService<PostgresPublishAgentResponseCoordinator>());
                services.AddSingleton<IInvocationWorkSessionGateway, PostgresInvocationWorkSessionGateway>();
                services.AddSingleton<IDurableInvocationWorkProcessor, DurableInvocationWorkProcessor>();
            }
            else
            {
                services.AddSingleton<IDurableInvocationWorkProcessor, IdleDurableInvocationWorkProcessor>();
            }

            if (timerPollingEnabled)
            {
                services.AddSingleton<IDueTimerFirePort, PostgresFireDueTimerCoordinator>();
                services.AddSingleton(CreateTimerFireSettings(configuration));
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
            });
        }

        services.AddSingleton<IDurableWorkBacklogSampler>(sp =>
            new DurableWorkBacklogSampler(
                sp.GetRequiredService<IDurableInvocationWorkStore>(),
                sp.GetRequiredService<ISessionRuntimeTelemetry>()));
    }

    private static DurableInvocationWorkSettings CreateInvocationWorkSettings(IConfiguration configuration)
    {
        var providerId = string.IsNullOrWhiteSpace(configuration["Sessions:ModelDeployment:ProviderId"])
            ? "unconfigured.provider"
            : configuration["Sessions:ModelDeployment:ProviderId"]!;
        var organizationBindingReference = configuration["Sessions:ModelDeployment:OrganizationBindingReference"];
        var organizationBindingVersion = configuration["Sessions:ModelDeployment:OrganizationBindingVersion"];
        return new DurableInvocationWorkSettings(
            new TrustedRuntimeActor(ResolveServiceActorId(configuration), "worker.session_runtime"),
            providerId,
            "worker.session_runtime",
            65_536,
            ownership => new ModelDeploymentCredentialBindingRequest(
                ownership.OrganizationId,
                providerId,
                organizationBindingReference,
                organizationBindingVersion,
                null,
                null,
                false,
                false,
                false));
    }

    private static DurableTimerFireSettings CreateTimerFireSettings(IConfiguration configuration) =>
        new(
            new TrustedRuntimeActor(ResolveServiceActorId(configuration), "worker.session_runtime"),
            "worker.session_runtime");

    private static Guid ResolveServiceActorId(IConfiguration configuration)
    {
        var configured = configuration["Sessions:WorkerServiceActorId"];
        return Guid.TryParse(configured, out var parsed) && parsed != Guid.Empty
            ? parsed
            : DefaultWorkerServiceActorId;
    }
}
