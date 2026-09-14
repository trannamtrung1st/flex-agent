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
            services.AddSingleton<IEvaluationDurableWorkProcessor, EvaluationDurableWorkProcessor>();
            services.AddSingleton<IEvaluationDurableWorkBacklogSampler>(sp =>
                new EvaluationDurableWorkBacklogSampler(
                    sp.GetRequiredService<IEvaluationDurableWorkStore>(),
                    sp.GetRequiredService<EvaluationDurableWorkSettings>(),
                    sp.GetRequiredService<ISessionRuntimeTelemetry>()));
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

public interface IEvaluationDurableWorkBacklogSampler
{
    Task SampleIfDueAsync(CancellationToken cancellationToken);
}

internal sealed class EvaluationDurableWorkBacklogSampler(
    IEvaluationDurableWorkStore workStore,
    EvaluationDurableWorkSettings settings,
    ISessionRuntimeTelemetry telemetry,
    TimeProvider? timeProvider = null,
    TimeSpan minInterval = default) : IEvaluationDurableWorkBacklogSampler
{
    public static TimeSpan DefaultMinInterval { get; } = TimeSpan.FromSeconds(30);

    private readonly IEvaluationDurableWorkStore _workStore = workStore;
    private readonly EvaluationDurableWorkSettings _settings = settings;
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _minInterval = minInterval > TimeSpan.Zero ? minInterval : DefaultMinInterval;
    private readonly object _gate = new();
    private DateTimeOffset _nextDue = DateTimeOffset.MinValue;

    public async Task SampleIfDueAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        lock (_gate)
        {
            if (now < _nextDue)
            {
                return;
            }

            _nextDue = now + _minInterval;
        }

        var snapshot = await _workStore.ReadClaimableSnapshotAsync(
            _settings.WorkerActorId,
            _settings.PerOrganizationConcurrency,
            cancellationToken);
        if (!snapshot.IsKnown)
        {
            return;
        }

        _telemetry.RecordGauge(
            SessionRuntimeTelemetryInstruments.WorkBacklog,
            snapshot.ClaimableCount,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.WorkType, EvaluationDurableWorkTypes.ExecuteRequest),
                (SessionRuntimeTelemetryLabelKeys.BacklogBucket, SessionRuntimeTelemetryBuckets.Count(snapshot.ClaimableCount)),
                (SessionRuntimeTelemetryLabelKeys.PartitionBucket, SessionRuntimeTelemetryBuckets.Count(snapshot.ClaimablePartitionCount))));
    }
}

internal sealed class IdleEvaluationDurableWorkBacklogSampler : IEvaluationDurableWorkBacklogSampler
{
    public static IdleEvaluationDurableWorkBacklogSampler Instance { get; } = new();

    public Task SampleIfDueAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
