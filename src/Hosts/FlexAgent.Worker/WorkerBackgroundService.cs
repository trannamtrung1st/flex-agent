using FlexAgent.IdentityAccess.Application;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Worker;

public sealed class WorkerBackgroundService(
    ILogger<WorkerBackgroundService> logger,
    WorkClaimGate workClaimGate,
    IRecoverableAuthorityGate authorityGate,
    IDurableInvocationWorkProcessor workProcessor,
    IDurableTimerFireProcessor timerFireProcessor,
    IDurableWorkBacklogSampler backlogSampler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker background loop started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await backlogSampler.SampleIfDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Durable work backlog sampling failed.");
                }

                if (workClaimGate.TryClaimWork() && authorityGate.CanAcceptProtectedWork())
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

                    try
                    {
                        var timerProcessed = await timerFireProcessor.TryProcessNextAsync(stoppingToken);
                        if (timerProcessed.Outcome == DurableTimerFireOutcomes.Idle)
                        {
                            logger.LogDebug("Worker timer lane idle at {Timestamp}", DateTimeOffset.UtcNow);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Durable timer-fire processing failed.");
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
        authorityGate.SetState(RecoverableAuthorityStates.Stopping);
        logger.LogInformation("Worker stopped accepting new durable work.");
        return base.StopAsync(cancellationToken);
    }
}
