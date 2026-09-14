namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationDurableWorkSettings(
    Guid WorkerActorId,
    string SourceChannel,
    TimeSpan ClaimLease = default,
    int PerOrganizationConcurrency = 1,
    TimeSpan ClaimCleanupTimeout = default)
{
    public TimeSpan EffectiveClaimLease =>
        ClaimLease > TimeSpan.Zero ? ClaimLease : TimeSpan.FromSeconds(30);

    public TimeSpan EffectiveClaimCleanupTimeout =>
        ClaimCleanupTimeout > TimeSpan.Zero ? ClaimCleanupTimeout : TimeSpan.FromSeconds(2);
}

public static class EvaluationDurableWorkTypes
{
    public const string ExecuteRequest = "evaluation.execute";
}

public sealed record EvaluationDurableWorkProcessResult(string Outcome, Guid? RequestId = null)
{
    public static EvaluationDurableWorkProcessResult Idle { get; } =
        new(EvaluationDurableWorkOutcomes.Idle);
}

public static class EvaluationDurableWorkOutcomes
{
    public const string Idle = "idle";

    public const string RetryLater = "retry_later";

    public const string ClaimReleaseFailed = "claim_release_failed";
}

public interface IEvaluationDurableWorkProcessor
{
    Task<EvaluationDurableWorkProcessResult> TryProcessNextAsync(CancellationToken cancellationToken);
}

public sealed class IdleEvaluationDurableWorkProcessor : IEvaluationDurableWorkProcessor
{
    public Task<EvaluationDurableWorkProcessResult> TryProcessNextAsync(CancellationToken cancellationToken) =>
        Task.FromResult(EvaluationDurableWorkProcessResult.Idle);
}

public sealed class EvaluationDurableWorkProcessor(
    IEvaluationDurableWorkStore workStore,
    EvaluationDurableWorkSettings settings) : IEvaluationDurableWorkProcessor
{
    public const string ExecutionDeferredFailureCategory = "worker.execution_deferred";

    public async Task<EvaluationDurableWorkProcessResult> TryProcessNextAsync(
        CancellationToken cancellationToken)
    {
        var claimed = await workStore.TryClaimAsync(
            settings.WorkerActorId,
            settings.EffectiveClaimLease,
            settings.PerOrganizationConcurrency,
            cancellationToken);
        if (claimed is null)
        {
            return EvaluationDurableWorkProcessResult.Idle;
        }

        // Slice 1 only proves claim authority; release cleanup is independent of caller shutdown.
        return await ReleaseForDeferredExecutionAsync(claimed);
    }

    private async Task<EvaluationDurableWorkProcessResult> ReleaseForDeferredExecutionAsync(
        EvaluationDurableWorkItem claimed)
    {
        using var cleanup = new CancellationTokenSource();
        cleanup.CancelAfter(settings.EffectiveClaimCleanupTimeout);
        try
        {
            var released = await workStore.ReleaseForRetryAsync(
                claimed,
                settings.WorkerActorId,
                ExecutionDeferredFailureCategory,
                cleanup.Token);
            if (!released)
            {
                return new EvaluationDurableWorkProcessResult(
                    EvaluationDurableWorkOutcomes.ClaimReleaseFailed,
                    claimed.RequestId);
            }

            return new EvaluationDurableWorkProcessResult(
                EvaluationDurableWorkOutcomes.RetryLater,
                claimed.RequestId);
        }
        catch (OperationCanceledException) when (cleanup.IsCancellationRequested)
        {
            return new EvaluationDurableWorkProcessResult(
                EvaluationDurableWorkOutcomes.ClaimReleaseFailed,
                claimed.RequestId);
        }
    }
}
