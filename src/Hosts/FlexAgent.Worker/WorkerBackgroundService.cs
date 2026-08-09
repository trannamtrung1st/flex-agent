namespace FlexAgent.Worker;

public sealed class WorkerBackgroundService(
    ILogger<WorkerBackgroundService> logger,
    WorkClaimGate workClaimGate) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker background loop started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (workClaimGate.TryClaimWork())
                {
                    logger.LogDebug("Worker idle heartbeat at {Timestamp}", DateTimeOffset.UtcNow);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker background loop cancellation requested.");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        workClaimGate.StopAcceptingWork();
        logger.LogInformation("Worker stopped accepting new work claims.");
        return base.StopAsync(cancellationToken);
    }
}
