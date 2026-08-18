using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.Worker;

public sealed class WorkloadIdentityRefreshService(
    IAuthenticatedWorkloadContextSource identitySource,
    IRecoverableAuthorityGate authorityGate,
    ILogger<WorkloadIdentityRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (string.Equals(authorityGate.State, RecoverableAuthorityStates.Stopping, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                var context = await identitySource.TryGetCurrentAsync(stoppingToken).ConfigureAwait(false);
                if (string.Equals(authorityGate.State, RecoverableAuthorityStates.Stopping, StringComparison.Ordinal))
                {
                    return;
                }

                ApplyObservation(authorityGate, context);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Workload identity refresh failed.");
                if (!string.Equals(
                    authorityGate.State,
                    RecoverableAuthorityStates.IdentityDenied,
                    StringComparison.Ordinal))
                {
                    authorityGate.SetState(RecoverableAuthorityStates.DependencyUnavailable);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    public static void ApplyObservation(
        IRecoverableAuthorityGate authorityGate,
        AuthenticatedWorkloadContext? context)
    {
        ArgumentNullException.ThrowIfNull(authorityGate);
        if (string.Equals(authorityGate.State, RecoverableAuthorityStates.Stopping, StringComparison.Ordinal))
        {
            return;
        }

        if (context is null)
        {
            if (!string.Equals(
                authorityGate.State,
                RecoverableAuthorityStates.IdentityDenied,
                StringComparison.Ordinal))
            {
                authorityGate.SetState(RecoverableAuthorityStates.DependencyUnavailable);
            }

            return;
        }

        if (!context.IsProofValidAt(DateTimeOffset.UtcNow))
        {
            authorityGate.SetState(RecoverableAuthorityStates.IdentityDenied);
            return;
        }

        if (context.ExpiresAt - DateTimeOffset.UtcNow <= TimeSpan.FromSeconds(60))
        {
            authorityGate.SetState(RecoverableAuthorityStates.RefreshDegraded);
            return;
        }

        authorityGate.SetState(RecoverableAuthorityStates.Ready);
    }
}
