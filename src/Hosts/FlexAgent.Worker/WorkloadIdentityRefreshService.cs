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

                if (context is null)
                {
                    authorityGate.SetState(RecoverableAuthorityStates.DependencyUnavailable);
                }
                else if (!context.IsProofValidAt(DateTimeOffset.UtcNow))
                {
                    authorityGate.SetState(RecoverableAuthorityStates.IdentityDenied);
                }
                else if (context.ExpiresAt - DateTimeOffset.UtcNow <= TimeSpan.FromSeconds(60))
                {
                    authorityGate.SetState(RecoverableAuthorityStates.RefreshDegraded);
                }
                else
                {
                    authorityGate.SetState(RecoverableAuthorityStates.Ready);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Workload identity refresh failed.");
                authorityGate.SetState(RecoverableAuthorityStates.DependencyUnavailable);
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
}
