using FlexAgent.IdentityAccess.Application;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexAgent.Worker;

public sealed class WorkerReadinessCheck(
    WorkClaimGate workClaimGate,
    IRecoverableAuthorityGate authorityGate,
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

        var identityState = authorityGate.State;
        if (ProtectedLaneEnabled(capabilities)
            && (identityState is RecoverableAuthorityStates.RefreshDegraded
                || (!authorityGate.CanAcceptProtectedWork()
                    && identityState is RecoverableAuthorityStates.IdentityDenied
                        or RecoverableAuthorityStates.DependencyUnavailable
                        or RecoverableAuthorityStates.Authenticating)))
        {
            return Task.FromResult(
                HealthCheckResult.Degraded($"Worker identity is {identityState}."));
        }

        if (ProtectedLaneEnabled(capabilities)
            && capabilities.DurableWorkClaimingEnabled
            && (string.Equals(capabilities.ModelExecutionAdapter, "direct_openai", StringComparison.Ordinal)
                || string.Equals(capabilities.ModelExecutionAdapter, "openrouter", StringComparison.Ordinal))
            && !capabilities.ModelExecutionQualified)
        {
            var adapterName = string.Equals(capabilities.ModelExecutionAdapter, "openrouter", StringComparison.Ordinal)
                ? "OpenRouter"
                : "Direct OpenAI";
            return Task.FromResult(
                HealthCheckResult.Degraded($"{adapterName} adapter is requested but not qualified."));
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

    private static bool ProtectedLaneEnabled(WorkerRuntimeCapabilities capabilities) =>
        capabilities.DurableWorkClaimingEnabled || capabilities.TimerPollingEnabled;
}
