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
        Assert.False(release.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Processor_reports_release_failure_when_store_rejects_cleanup()
    {
        var requestId = Guid.CreateVersion7();
        var workerActorId = Guid.CreateVersion7();
        var claimed = CreateWorkItem(requestId, workerActorId);
        var store = new RecordingEvaluationWorkStore
        {
            ClaimResult = claimed,
            ReleaseResult = false,
        };
        var processor = CreateProcessor(store, workerActorId);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(EvaluationDurableWorkOutcomes.ClaimReleaseFailed, result.Outcome);
        Assert.Equal(requestId, result.RequestId);
    }

    [Fact]
    public async Task Processor_uses_bounded_cleanup_token_when_cancelled_after_claim()
    {
        var requestId = Guid.CreateVersion7();
        var workerActorId = Guid.CreateVersion7();
        var claimed = CreateWorkItem(requestId, workerActorId);
        using var cancellation = new CancellationTokenSource();
        var store = new RecordingEvaluationWorkStore
        {
            ClaimResult = claimed,
            OnClaim = () => cancellation.Cancel(),
        };
        var processor = CreateProcessor(store, workerActorId);

        var result = await processor.TryProcessNextAsync(cancellation.Token);

        Assert.Equal(EvaluationDurableWorkOutcomes.RetryLater, result.Outcome);
        var release = Assert.Single(store.Releases);
        Assert.False(release.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Processor_uses_bounded_cleanup_token_when_cancelled_during_release()
    {
        var requestId = Guid.CreateVersion7();
        var workerActorId = Guid.CreateVersion7();
        var claimed = CreateWorkItem(requestId, workerActorId);
        using var callerCancellation = new CancellationTokenSource();
        var store = new RecordingEvaluationWorkStore
        {
            ClaimResult = claimed,
            OnRelease = () => callerCancellation.Cancel(),
        };
        var processor = CreateProcessor(store, workerActorId);

        var result = await processor.TryProcessNextAsync(callerCancellation.Token);

        Assert.Equal(EvaluationDurableWorkOutcomes.RetryLater, result.Outcome);
        var release = Assert.Single(store.Releases);
        Assert.True(callerCancellation.IsCancellationRequested);
        Assert.False(release.CancellationToken.IsCancellationRequested);
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

        public bool ReleaseResult { get; init; } = true;

        public Action? OnClaim { get; init; }

        public Action? OnRelease { get; init; }

        public int ClaimAttempts { get; private set; }

        public List<(EvaluationDurableWorkItem Work, Guid ClaimOwner, string FailureCategory, CancellationToken CancellationToken)> Releases { get; } = [];

        public Task<EvaluationDurableWorkItem?> TryClaimAsync(
            Guid claimOwner,
            TimeSpan lease,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken)
        {
            ClaimAttempts++;
            OnClaim?.Invoke();
            return Task.FromResult(ClaimResult);
        }

        public Task<EvaluationDurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(
            Guid claimOwner,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken) =>
            Task.FromResult(new EvaluationDurableWorkBacklogSnapshot(0, 0));

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
            OnRelease?.Invoke();
            Releases.Add((work, claimOwner, failureCategory, cancellationToken));
            return Task.FromResult(ReleaseResult);
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
