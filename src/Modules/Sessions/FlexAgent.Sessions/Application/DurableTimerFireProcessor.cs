using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record DurableTimerFireSettings(
    TrustedRuntimeActor ServiceActor,
    string SourceChannel);

public sealed record DurableTimerFireProcessResult(
    string Outcome,
    string? TimerOutcomeCode = null)
{
    public static DurableTimerFireProcessResult Idle { get; } = new(DurableTimerFireOutcomes.Idle);
}

public static class DurableTimerFireOutcomes
{
    public const string Idle = "idle";
    public const string Fired = "fired";
    public const string Acknowledged = "acknowledged";
    public const string RetryLater = "retry_later";
}

public interface IDueTimerFirePort
{
    Task<TimerFireResult> TryFireNextDueAsync(
        FireDueTimerCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class DurableTimerFireProcessor(
    IDueTimerFirePort dueTimerFirePort,
    DurableTimerFireSettings settings)
{
    public async Task<DurableTimerFireProcessResult> TryProcessNextAsync(
        CancellationToken cancellationToken)
    {
        var result = await dueTimerFirePort.TryFireNextDueAsync(
            new FireDueTimerCommand(settings.ServiceActor, Guid.NewGuid(), settings.SourceChannel),
            cancellationToken);
        return result.OutcomeCode switch
        {
            TimerFireOutcomeCodes.Idle => DurableTimerFireProcessResult.Idle,
            TimerFireOutcomeCodes.BudgetExhausted => new DurableTimerFireProcessResult(
                DurableTimerFireOutcomes.Acknowledged,
                result.OutcomeCode),
            TimerFireOutcomeCodes.Succeeded or TimerFireOutcomeCodes.Reconciled => new DurableTimerFireProcessResult(
                DurableTimerFireOutcomes.Fired,
                result.OutcomeCode),
            _ => new DurableTimerFireProcessResult(
                DurableTimerFireOutcomes.RetryLater,
                result.OutcomeCode),
        };
    }
}
