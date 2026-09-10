namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationDurableWorkProcessResult(string Outcome)
{
    public static EvaluationDurableWorkProcessResult Idle { get; } =
        new(EvaluationDurableWorkOutcomes.Idle);
}

public static class EvaluationDurableWorkOutcomes
{
    public const string Idle = "idle";
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
