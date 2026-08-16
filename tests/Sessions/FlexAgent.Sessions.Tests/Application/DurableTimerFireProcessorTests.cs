using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class DurableTimerFireProcessorTests
{
    [Fact]
    public async Task Idle_port_does_not_retry()
    {
        var port = new ScriptedDueTimerFirePort(new TimerFireResult(false, TimerFireOutcomeCodes.Idle));
        var processor = CreateProcessor(port);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Idle, result.Outcome);
        Assert.Equal(1, port.CallCount);
    }

    [Fact]
    public async Task Budget_exhausted_is_a_durable_acknowledgement_and_is_not_retried()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(false, TimerFireOutcomeCodes.BudgetExhausted));
        var processor = CreateProcessor(port);

        var first = await processor.TryProcessNextAsync(CancellationToken.None);
        var second = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Acknowledged, first.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.BudgetExhausted, first.TimerOutcomeCode);
        Assert.NotEqual(DurableTimerFireOutcomes.RetryLater, first.Outcome);
        Assert.Equal(DurableTimerFireOutcomes.Idle, second.Outcome);
        Assert.Equal(2, port.CallCount);
    }

    [Fact]
    public async Task Successful_fire_is_processed()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(true, TimerFireOutcomeCodes.Succeeded));
        var processor = CreateProcessor(port);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Fired, result.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, result.TimerOutcomeCode);
    }

    [Fact]
    public async Task Stale_revision_retries_later()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision));
        var processor = CreateProcessor(port);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.RetryLater, result.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.StaleRevision, result.TimerOutcomeCode);
    }

    private static DurableTimerFireProcessor CreateProcessor(IDueTimerFirePort port) =>
        new(
            port,
            new DurableTimerFireSettings(SessionRuntimeTestFixtures.CreateActor(), "application.test"));

    private sealed class ScriptedDueTimerFirePort(params TimerFireResult[] results) : IDueTimerFirePort
    {
        private int _index;

        public int CallCount { get; private set; }

        public Task<TimerFireResult> TryFireNextDueAsync(
            FireDueTimerCommand command,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (_index >= results.Length)
            {
                return Task.FromResult(new TimerFireResult(false, TimerFireOutcomeCodes.Idle));
            }

            return Task.FromResult(results[_index++]);
        }
    }
}
