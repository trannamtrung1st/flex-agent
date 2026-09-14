using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;

namespace FlexAgent.Worker;

internal static class WorkerEvaluationLane
{
    public static bool ResolveRequested(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var evaluationProcessingRequested = configuration.GetValue("Evaluation:Processing:Enabled", false);
        if (evaluationProcessingRequested && !EvaluationInfrastructure.ProcessingEnabled)
        {
            throw new InvalidOperationException(
                "Evaluation:Processing:Enabled requires EvaluationInfrastructure.ProcessingEnabled to be true.");
        }

        return evaluationProcessingRequested;
    }

    public static void RegisterServices(
        IServiceCollection services,
        IConfiguration configuration,
        bool evaluationProcessingEnabled)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (evaluationProcessingEnabled)
        {
            var workerActorId = RequireWorkerServiceActorId(configuration);
            services.AddSingleton(new EvaluationDurableWorkSettings(
                workerActorId,
                "worker.evaluation_runtime"));
            services.AddSingleton<IEvaluationWorkloadIdentityGate>(sp =>
                new EvaluationWorkloadIdentityGateAdapter(
                    sp.GetRequiredService<IAuthenticatedWorkloadTransactionGuard>()));
            services.AddSingleton<IEvaluationDurableWorkStore>(sp =>
                new PostgresEvaluationDurableWorkStore(
                    sp.GetRequiredService<PostgresConnectionAccessor>(),
                    sp.GetRequiredService<IEvaluationWorkloadIdentityGate>()));
            services.AddSingleton<IEvaluationRuntimeTelemetry>(sp =>
                new EvaluationRuntimeTelemetryAdapter(sp.GetRequiredService<ISessionRuntimeTelemetry>()));
            services.AddSingleton<IEvaluationDurableWorkProcessor>(sp =>
                new EvaluationDurableWorkProcessor(
                    sp.GetRequiredService<IEvaluationDurableWorkStore>(),
                    sp.GetRequiredService<EvaluationDurableWorkSettings>(),
                    sp.GetRequiredService<IEvaluationRuntimeTelemetry>()));
            services.AddSingleton<IEvaluationDurableWorkBacklogSampler>(sp =>
                new EvaluationDurableWorkBacklogSampler(
                    sp.GetRequiredService<IEvaluationDurableWorkStore>(),
                    sp.GetRequiredService<EvaluationDurableWorkSettings>(),
                    sp.GetRequiredService<IEvaluationRuntimeTelemetry>()));
            return;
        }

        services.AddSingleton<IEvaluationDurableWorkProcessor, IdleEvaluationDurableWorkProcessor>();
        services.AddSingleton<IEvaluationDurableWorkBacklogSampler>(_ =>
            IdleEvaluationDurableWorkBacklogSampler.Instance);
    }

    private static Guid RequireWorkerServiceActorId(IConfiguration configuration)
    {
        var configured = configuration["Sessions:WorkerServiceActorId"];
        if (!Guid.TryParse(configured, out var parsed) || parsed == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Sessions:WorkerServiceActorId must be an explicit non-empty actor id when Evaluation processing is enabled.");
        }

        return parsed;
    }
}
