using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public interface IDurableWorkBacklogSampler
{
    Task SampleIfDueAsync(CancellationToken cancellationToken);
}

public sealed class DurableWorkBacklogSampler(
    IDurableInvocationWorkStore workStore,
    ISessionRuntimeTelemetry telemetry,
    TimeProvider? timeProvider = null,
    TimeSpan minInterval = default) : IDurableWorkBacklogSampler
{
    public static TimeSpan DefaultMinInterval { get; } = TimeSpan.FromSeconds(30);

    private readonly IDurableInvocationWorkStore _workStore = workStore;
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _minInterval = minInterval > TimeSpan.Zero ? minInterval : DefaultMinInterval;
    private readonly object _gate = new();
    private DateTimeOffset _nextDue = DateTimeOffset.MinValue;

    public async Task SampleIfDueAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        lock (_gate)
        {
            if (now < _nextDue)
            {
                return;
            }

            _nextDue = now + _minInterval;
        }

        var snapshot = await _workStore.ReadClaimableSnapshotAsync(cancellationToken);
        if (!snapshot.IsKnown)
        {
            return;
        }

        _telemetry.RecordGauge(
            SessionRuntimeTelemetryInstruments.WorkBacklog,
            snapshot.ClaimableCount,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.WorkType, DurableSessionWorkTypes.ExecuteInvocation),
                (SessionRuntimeTelemetryLabelKeys.BacklogBucket, SessionRuntimeTelemetryBuckets.Count(snapshot.ClaimableCount)),
                (SessionRuntimeTelemetryLabelKeys.PartitionBucket, SessionRuntimeTelemetryBuckets.Count(snapshot.ClaimablePartitionCount))));
    }
}
