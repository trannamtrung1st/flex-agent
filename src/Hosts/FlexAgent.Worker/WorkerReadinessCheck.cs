using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexAgent.Worker;

public sealed class WorkerReadinessCheck(WorkClaimGate workClaimGate) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            workClaimGate.TryClaimWork()
                ? HealthCheckResult.Healthy("Worker is accepting work claims.")
                : HealthCheckResult.Unhealthy("Worker is shutting down and not accepting work claims."));
    }
}
