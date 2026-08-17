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

        return Task.FromResult(
            capabilities.DurableWorkClaimingEnabled
                ? HealthCheckResult.Healthy("Worker loop is running and durable work claiming is enabled.")
                : HealthCheckResult.Healthy("Worker loop is running. Durable work claiming is not enabled."));
    }
}
