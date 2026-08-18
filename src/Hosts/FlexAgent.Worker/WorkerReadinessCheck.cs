using FlexAgent.IdentityAccess.Application;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexAgent.Worker;

public sealed class WorkerReadinessCheck(
    WorkClaimGate workClaimGate,
    IRecoverableAuthorityGate authorityGate,
    WorkerRuntimeCapabilities capabilities,
    IAuthenticatedWorkloadContextSource identitySource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!workClaimGate.TryClaimWork())
        {
            return HealthCheckResult.Unhealthy("Worker is shutting down.");
        }

        var identityState = authorityGate.State;
        var current = ProtectedLaneEnabled(capabilities)
            ? await identitySource.TryGetCurrentAsync(cancellationToken).ConfigureAwait(false)
            : null;
        var identityUnavailable = ProtectedLaneEnabled(capabilities)
            && (current is null || !current.IsProofValidAt(DateTimeOffset.UtcNow));
        if (ProtectedLaneEnabled(capabilities)
            && (identityUnavailable
                || identityState is RecoverableAuthorityStates.RefreshDegraded
                || (!authorityGate.CanAcceptProtectedWork()
                    && identityState is RecoverableAuthorityStates.IdentityDenied
                        or RecoverableAuthorityStates.DependencyUnavailable
                        or RecoverableAuthorityStates.Authenticating)))
        {
            var reported = identityUnavailable && identityState is RecoverableAuthorityStates.Ready
                ? RecoverableAuthorityStates.IdentityDenied
                : identityState;
            return HealthCheckResult.Degraded($"Worker identity is {reported}.");
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
        return HealthCheckResult.Healthy(description);
    }

    private static bool ProtectedLaneEnabled(WorkerRuntimeCapabilities capabilities) =>
        capabilities.DurableWorkClaimingEnabled || capabilities.TimerPollingEnabled;
}
