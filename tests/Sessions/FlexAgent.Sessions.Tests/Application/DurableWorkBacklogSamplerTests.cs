using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class DurableWorkBacklogSamplerTests
{
    [Fact]
    public async Task First_due_sample_records_bounded_backlog_gauges_without_identifiers()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 17, 8, 0, 0, TimeSpan.Zero));
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var store = new CountingBacklogStore(claimableCount: 25, partitionCount: 3);
        var sampler = new DurableWorkBacklogSampler(
            store,
            new SessionRuntimeTelemetry(sink),
            clock);

        await sampler.SampleIfDueAsync(CancellationToken.None);

        var gauge = Assert.Single(sink.Gauges);
        Assert.Equal(SessionRuntimeTelemetryInstruments.WorkBacklog, gauge.Instrument);
        Assert.Equal(25, gauge.Value);
        Assert.Equal("n21_to_100", gauge.Labels[SessionRuntimeTelemetryLabelKeys.BacklogBucket]);
        Assert.Equal("n2_to_5", gauge.Labels[SessionRuntimeTelemetryLabelKeys.PartitionBucket]);
        Assert.Equal(
            DurableSessionWorkTypes.ExecuteInvocation,
            gauge.Labels[SessionRuntimeTelemetryLabelKeys.WorkType]);
        Assert.DoesNotContain(sink.AllLabelValues(), value => Guid.TryParse(value, out _));
        Assert.Equal(1, store.ReadCount);
    }

    [Fact]
    public async Task Samples_inside_the_minimum_interval_do_not_count_the_claimable_backlog()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 17, 8, 0, 0, TimeSpan.Zero));
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var store = new CountingBacklogStore(claimableCount: 1, partitionCount: 1);
        var sampler = new DurableWorkBacklogSampler(
            store,
            new SessionRuntimeTelemetry(sink),
            clock);

        await sampler.SampleIfDueAsync(CancellationToken.None);
        clock.Advance(DurableWorkBacklogSampler.DefaultMinInterval.Subtract(TimeSpan.FromSeconds(1)));
        await sampler.SampleIfDueAsync(CancellationToken.None);
        await sampler.SampleIfDueAsync(CancellationToken.None);

        Assert.Equal(1, store.ReadCount);
        Assert.Single(sink.Gauges);
    }

    [Fact]
    public async Task A_later_interval_records_another_sample()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 17, 8, 0, 0, TimeSpan.Zero));
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var store = new CountingBacklogStore(claimableCount: 0, partitionCount: 0);
        var sampler = new DurableWorkBacklogSampler(
            store,
            new SessionRuntimeTelemetry(sink),
            clock);

        await sampler.SampleIfDueAsync(CancellationToken.None);
        clock.Advance(DurableWorkBacklogSampler.DefaultMinInterval);
        await sampler.SampleIfDueAsync(CancellationToken.None);

        Assert.Equal(2, store.ReadCount);
        Assert.Equal(2, sink.Gauges.Count());
        Assert.All(
            sink.Gauges,
            gauge => Assert.Equal("n0", gauge.Labels[SessionRuntimeTelemetryLabelKeys.BacklogBucket]));
    }

    [Fact]
    public async Task Unknown_snapshots_are_not_recorded()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var sampler = new DurableWorkBacklogSampler(
            new UnknownBacklogStore(),
            new SessionRuntimeTelemetry(sink));

        await sampler.SampleIfDueAsync(CancellationToken.None);

        Assert.Empty(sink.Gauges);
        Assert.Empty(sink.Points);
    }

    private sealed class CountingBacklogStore(int claimableCount, int partitionCount) : IDurableInvocationWorkStore
    {
        public int ReadCount { get; private set; }

        public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
            TimeSpan lease,
            CancellationToken cancellationToken) =>
            Task.FromResult<DurableInvocationWorkItem?>(null);

        public Task ReleaseToPendingAsync(
            DurableInvocationWorkItem work,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task MarkCompletedAsync(
            DurableInvocationWorkItem work,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<DurableWorkBacklogSnapshot> ReadClaimableSnapshotAsync(CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(new DurableWorkBacklogSnapshot(claimableCount, partitionCount));
        }
    }

    private sealed class UnknownBacklogStore : IDurableInvocationWorkStore
    {
        public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
            TimeSpan lease,
            CancellationToken cancellationToken) =>
            Task.FromResult<DurableInvocationWorkItem?>(null);

        public Task ReleaseToPendingAsync(
            DurableInvocationWorkItem work,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task MarkCompletedAsync(
            DurableInvocationWorkItem work,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan delta) => utcNow += delta;
    }
}
