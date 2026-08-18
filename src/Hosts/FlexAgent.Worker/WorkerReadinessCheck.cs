using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexAgent.Worker;

public sealed class WorkerReadinessCheck(
    WorkClaimGate workClaimGate,
    WorkerRuntimeCapabilities capabilities) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!workClaimGate.TryClaimWork())
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Worker is shutting down."));
        }

        var claiming = capabilities.DurableWorkClaimingEnabled
            ? "durable work claiming is enabled"
            : "Durable work claiming is not enabled";
        var timerPolling = capabilities.TimerPollingEnabled
            ? "Timer polling is enabled"
            : "Timer polling is not enabled";
        var description = capabilities.DurableWorkClaimingEnabled
            ? $"Worker loop is running and {claiming}. {timerPolling}."
            : $"Worker loop is running. {claiming}. {timerPolling}.";
        return Task.FromResult(HealthCheckResult.Healthy(description));
    }
}
