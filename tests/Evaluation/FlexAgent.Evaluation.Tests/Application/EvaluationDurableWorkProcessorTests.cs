using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationDurableWorkProcessorTests
{
    [Fact]
    public async Task Idle_processor_returns_idle_without_claiming_work()
    {
        var processor = new IdleEvaluationDurableWorkProcessor();

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(EvaluationDurableWorkOutcomes.Idle, result.Outcome);
        Assert.Null(result.RequestId);
    }

    [Fact]
    public async Task Processor_returns_idle_when_no_claimable_work_exists()
    {
        var store = new RecordingEvaluationWorkStore { ClaimResult = null };
        var processor = CreateProcessor(store);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(EvaluationDurableWorkOutcomes.Idle, result.Outcome);
        Assert.Equal(1, store.ClaimAttempts);
        Assert.Empty(store.Releases);
    }

    [Fact]
    public async Task Processor_claims_admitted_work_then_releases_for_deferred_execution()
    {
        var requestId = Guid.CreateVersion7();
        var workerActorId = Guid.CreateVersion7();
        var claimed = CreateWorkItem(requestId, workerActorId);
        var store = new RecordingEvaluationWorkStore { ClaimResult = claimed };
        var processor = CreateProcessor(store, workerActorId);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(EvaluationDurableWorkOutcomes.RetryLater, result.Outcome);
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(1, store.ClaimAttempts);
        var release = Assert.Single(store.Releases);
        Assert.Equal(requestId, release.Work.RequestId);
        Assert.Equal(
            EvaluationDurableWorkProcessor.ExecutionDeferredFailureCategory,
            release.FailureCategory);
    }

    private static EvaluationDurableWorkProcessor CreateProcessor(
        IEvaluationDurableWorkStore store,
        Guid? workerActorId = null) =>
        new(
            store,
            new EvaluationDurableWorkSettings(
                workerActorId ?? Guid.CreateVersion7(),
                "test.evaluation_runtime"));

    private static EvaluationDurableWorkItem CreateWorkItem(Guid requestId, Guid delegationId) =>
        new(
            Guid.CreateVersion7(),
            requestId,
            Guid.CreateVersion7(),
            new EvaluationOwnership(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            new string('a', 64),
            delegationId,
            1,
            3,
            30,
            5,
            DateTimeOffset.UtcNow.AddSeconds(30));

    private sealed class RecordingEvaluationWorkStore : IEvaluationDurableWorkStore
    {
        public EvaluationDurableWorkItem? ClaimResult { get; init; }

        public int ClaimAttempts { get; private set; }

        public List<(EvaluationDurableWorkItem Work, Guid ClaimOwner, string FailureCategory)> Releases { get; } = [];

        public Task<EvaluationDurableWorkItem?> TryClaimAsync(
            Guid claimOwner,
            TimeSpan lease,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken)
        {
            ClaimAttempts++;
            return Task.FromResult(ClaimResult);
        }

        public Task<DateTimeOffset?> TryRenewAsync(
            EvaluationDurableWorkItem work,
            Guid claimOwner,
            TimeSpan lease,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ReleaseForRetryAsync(
            EvaluationDurableWorkItem work,
            Guid claimOwner,
            string failureCategory,
            CancellationToken cancellationToken)
        {
            Releases.Add((work, claimOwner, failureCategory));
            return Task.FromResult(true);
        }

        public Task<bool> MarkExhaustedAsync(
            EvaluationDurableWorkItem work,
            Guid claimOwner,
            string failureCategory,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkCompletedAsync(
            EvaluationDurableWorkItem work,
            Guid claimOwner,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
