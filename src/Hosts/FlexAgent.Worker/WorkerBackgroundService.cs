using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Worker;

public sealed class WorkerBackgroundService(
    ILogger<WorkerBackgroundService> logger,
    WorkClaimGate workClaimGate,
    IDurableInvocationWorkProcessor workProcessor) : BackgroundService
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
                    try
                    {
                        var processed = await workProcessor.TryProcessNextAsync(stoppingToken);
                        if (processed.Outcome == DurableInvocationWorkOutcomes.Idle)
                        {
                            logger.LogDebug("Worker idle heartbeat at {Timestamp}", DateTimeOffset.UtcNow);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Durable invocation work processing failed.");
                    }
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
