using FlexAgent.Evaluation.Application;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationDurableWorkBacklogSamplerTests
{
    [Fact]
    public async Task First_due_sample_records_bounded_backlog_without_identifiers()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero));
        var telemetry = new CapturingEvaluationRuntimeTelemetry();
        var store = new CountingBacklogStore(claimableCount: 25, partitionCount: 3);
        var sampler = new EvaluationDurableWorkBacklogSampler(
            store,
            new EvaluationDurableWorkSettings(Guid.CreateVersion7(), "test.evaluation_runtime"),
            telemetry,
            clock);

        await sampler.SampleIfDueAsync(CancellationToken.None);

        var backlog = Assert.Single(telemetry.WorkBacklogs);
        Assert.Equal(25, backlog.ClaimableCount);
        Assert.Equal(3, backlog.PartitionCount);
        Assert.DoesNotContain(telemetry.AllRecordedValues(), value => Guid.TryParse(value, out _));
        Assert.Equal(1, store.ReadCount);
    }

    [Fact]
    public async Task Samples_inside_the_minimum_interval_do_not_count_the_claimable_backlog()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero));
        var telemetry = new CapturingEvaluationRuntimeTelemetry();
        var store = new CountingBacklogStore(claimableCount: 1, partitionCount: 1);
        var sampler = new EvaluationDurableWorkBacklogSampler(
            store,
            new EvaluationDurableWorkSettings(Guid.CreateVersion7(), "test.evaluation_runtime"),
            telemetry,
            clock);

        await sampler.SampleIfDueAsync(CancellationToken.None);
        clock.Advance(EvaluationDurableWorkBacklogSampler.DefaultMinInterval.Subtract(TimeSpan.FromSeconds(1)));
        await sampler.SampleIfDueAsync(CancellationToken.None);
        await sampler.SampleIfDueAsync(CancellationToken.None);

        Assert.Equal(1, store.ReadCount);
        Assert.Single(telemetry.WorkBacklogs);
    }

    [Fact]
    public async Task Unknown_snapshots_are_not_recorded()
    {
        var telemetry = new CapturingEvaluationRuntimeTelemetry();
        var sampler = new EvaluationDurableWorkBacklogSampler(
            new UnknownBacklogStore(),
            new EvaluationDurableWorkSettings(Guid.CreateVersion7(), "test.evaluation_runtime"),
            telemetry);

        await sampler.SampleIfDueAsync(CancellationToken.None);

        Assert.Empty(telemetry.WorkBacklogs);
    }

    private sealed class CountingBacklogStore(int claimableCount, int partitionCount) : IEvaluationDurableWorkStore
    {
        public int ReadCount { get; private set; }

        public Task<EvaluationDurableWorkItem?> TryClaimAsync(
            Guid claimOwner,
            TimeSpan lease,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken) =>
            Task.FromResult<EvaluationDurableWorkItem?>(null);

        public Task<EvaluationDurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(
            Guid claimOwner,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(new EvaluationDurableWorkBacklogSnapshot(claimableCount, partitionCount));
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
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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

    private sealed class UnknownBacklogStore : IEvaluationDurableWorkStore
    {
        public Task<EvaluationDurableWorkItem?> TryClaimAsync(
            Guid claimOwner,
            TimeSpan lease,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken) =>
            Task.FromResult<EvaluationDurableWorkItem?>(null);

        public Task<EvaluationDurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(
            Guid claimOwner,
            int perOrganizationConcurrency,
            CancellationToken cancellationToken) =>
            Task.FromResult(EvaluationDurableWorkBacklogSnapshot.Unknown);

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
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan delta) => utcNow += delta;
    }
}
