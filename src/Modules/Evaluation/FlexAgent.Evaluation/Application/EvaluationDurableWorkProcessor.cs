namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationDurableWorkSettings(
    Guid WorkerActorId,
    string SourceChannel,
    TimeSpan ClaimLease = default,
    int PerOrganizationConcurrency = 1)
{
    public TimeSpan EffectiveClaimLease =>
        ClaimLease > TimeSpan.Zero ? ClaimLease : TimeSpan.FromSeconds(30);
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

        if (cancellationToken.IsCancellationRequested)
        {
            await workStore.ReleaseForRetryAsync(
                claimed,
                settings.WorkerActorId,
                ExecutionDeferredFailureCategory,
                cancellationToken);
            return new EvaluationDurableWorkProcessResult(
                EvaluationDurableWorkOutcomes.RetryLater,
                claimed.RequestId);
        }

        // Full evaluator execution is deferred to later Phase 10 slices; prove claim authority first.
        await workStore.ReleaseForRetryAsync(
            claimed,
            settings.WorkerActorId,
            ExecutionDeferredFailureCategory,
            cancellationToken);
        return new EvaluationDurableWorkProcessResult(
            EvaluationDurableWorkOutcomes.RetryLater,
            claimed.RequestId);
    }
}
