namespace FlexAgent.Evaluation.Application;

public static class EvaluationRuntimeTelemetryOutcomes
{
    public const string Claimed = "claimed";
}

public interface IEvaluationRuntimeTelemetry
{
    void RecordWorkClaim(string outcome);

    void RecordWorkProcess(string outcome);

    void RecordWorkBacklog(int claimableCount, int partitionCount);
}

public sealed class NoopEvaluationRuntimeTelemetry : IEvaluationRuntimeTelemetry
{
    public static NoopEvaluationRuntimeTelemetry Instance { get; } = new();

    public void RecordWorkClaim(string outcome)
    {
    }

    public void RecordWorkProcess(string outcome)
    {
    }

    public void RecordWorkBacklog(int claimableCount, int partitionCount)
    {
    }
}

public sealed class CapturingEvaluationRuntimeTelemetry : IEvaluationRuntimeTelemetry
{
    public List<string> WorkClaims { get; } = [];

    public List<string> WorkProcesses { get; } = [];

    public List<(int ClaimableCount, int PartitionCount)> WorkBacklogs { get; } = [];

    public IEnumerable<string> AllRecordedValues() =>
        WorkClaims.Concat(WorkProcesses);

    public void RecordWorkClaim(string outcome) => WorkClaims.Add(outcome);

    public void RecordWorkProcess(string outcome) => WorkProcesses.Add(outcome);

    public void RecordWorkBacklog(int claimableCount, int partitionCount) =>
        WorkBacklogs.Add((claimableCount, partitionCount));
}

public interface IEvaluationDurableWorkBacklogSampler
{
    Task SampleIfDueAsync(CancellationToken cancellationToken);
}

public sealed class EvaluationDurableWorkBacklogSampler(
    IEvaluationDurableWorkStore workStore,
    EvaluationDurableWorkSettings settings,
    IEvaluationRuntimeTelemetry telemetry,
    TimeProvider? timeProvider = null,
    TimeSpan minInterval = default) : IEvaluationDurableWorkBacklogSampler
{
    public static TimeSpan DefaultMinInterval { get; } = TimeSpan.FromSeconds(30);

    private readonly IEvaluationDurableWorkStore _workStore = workStore;
    private readonly EvaluationDurableWorkSettings _settings = settings;
    private readonly IEvaluationRuntimeTelemetry _telemetry =
        telemetry ?? NoopEvaluationRuntimeTelemetry.Instance;
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

        var snapshot = await _workStore.ReadClaimableSnapshotAsync(
            _settings.WorkerActorId,
            _settings.PerOrganizationConcurrency,
            cancellationToken);
        if (!snapshot.IsKnown)
        {
            return;
        }

        _telemetry.RecordWorkBacklog(snapshot.ClaimableCount, snapshot.ClaimablePartitionCount);
    }
}

public sealed class IdleEvaluationDurableWorkBacklogSampler : IEvaluationDurableWorkBacklogSampler
{
    public static IdleEvaluationDurableWorkBacklogSampler Instance { get; } = new();

    public Task SampleIfDueAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
