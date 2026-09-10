using FlexAgent.Evaluation.Application;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationDurableWorkProcessorTests
{
    [Fact]
    public async Task Idle_processor_returns_idle_without_claiming_work()
    {
        var processor = new IdleEvaluationDurableWorkProcessor();

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(EvaluationDurableWorkOutcomes.Idle, result.Outcome);
    }
}
