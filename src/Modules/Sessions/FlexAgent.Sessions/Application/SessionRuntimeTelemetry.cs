namespace FlexAgent.Sessions.Application;

public static class SessionRuntimeTelemetryInstruments
{
    public const string TriggerAdmission = "session.trigger.admission";
    public const string InvocationCompletion = "session.invocation.completion";
    public const string DecisionEffect = "session.decision.effect";
    public const string TimerRecommendation = "session.timer.recommendation";
    public const string TimerFire = "session.timer.fire";
    public const string TimerDrift = "session.timer.drift";
    public const string FragmentCommit = "session.fragment.commit";
    public const string EventReplay = "session.event.replay";
    public const string LifecycleChange = "session.lifecycle.change";
    public const string WorkClaim = "session.work.claim";
    public const string WorkProcess = "session.work.process";
    public const string WorkBacklog = "session.work.backlog";
    public const string Fault = "session.fault";
    public const string Rejected = "session.telemetry.rejected";
}

public static class SessionRuntimeTelemetryKinds
{
    public const string Counter = "counter";
    public const string Histogram = "histogram";
    public const string Gauge = "gauge";
}

public static class SessionRuntimeTelemetryLabelKeys
{
    public const string Outcome = "outcome";
    public const string TriggerFamily = "trigger_family";
    public const string DecisionType = "decision_type";
    public const string FirstFragment = "first_fragment";
    public const string WorkType = "work_type";
    public const string DelayBucket = "delay_bucket";
    public const string BacklogBucket = "backlog_bucket";
    public const string PartitionBucket = "partition_bucket";
    public const string FaultKind = "fault_kind";
    public const string Reason = "reason";
    public const string Transition = "transition";
}

public static class SessionRuntimeTelemetryValues
{
    public const string Yes = "yes";
    public const string No = "no";
    public const string Claimed = "claimed";
    public const string Idle = "idle";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Recovered = "recovered";
    public const string Audit = "audit";
    public const string Outbox = "outbox";
    public const string Manifest = "manifest";
    public const string UnknownInstrument = "unknown_instrument";
    public const string UnknownLabelKey = "unknown_label_key";
    public const string InvalidLabelValue = "invalid_label_value";
    public const string ExcessiveLabels = "excessive_labels";
}

public sealed record SessionRuntimeTelemetryPoint(
    string Instrument,
    string Kind,
    double Value,
    IReadOnlyDictionary<string, string> Labels);

public interface ISessionRuntimeTelemetrySink
{
    void Write(SessionRuntimeTelemetryPoint point);
}

public interface ISessionRuntimeTelemetry
{
    void RecordCounter(string instrument, IReadOnlyDictionary<string, string> labels, long delta = 1);

    void RecordDuration(string instrument, TimeSpan duration, IReadOnlyDictionary<string, string> labels);

    void RecordGauge(string instrument, double value, IReadOnlyDictionary<string, string> labels);
}

public sealed class NoopSessionRuntimeTelemetrySink : ISessionRuntimeTelemetrySink
{
    public static NoopSessionRuntimeTelemetrySink Instance { get; } = new();

    public void Write(SessionRuntimeTelemetryPoint point)
    {
    }
}

public sealed class NoopSessionRuntimeTelemetry : ISessionRuntimeTelemetry
{
    public static NoopSessionRuntimeTelemetry Instance { get; } = new();

    public void RecordCounter(string instrument, IReadOnlyDictionary<string, string> labels, long delta = 1)
    {
    }

    public void RecordDuration(string instrument, TimeSpan duration, IReadOnlyDictionary<string, string> labels)
    {
    }

    public void RecordGauge(string instrument, double value, IReadOnlyDictionary<string, string> labels)
    {
    }
}

public sealed class CapturingSessionRuntimeTelemetrySink : ISessionRuntimeTelemetrySink
{
    private readonly List<SessionRuntimeTelemetryPoint> _points = [];

    public IReadOnlyList<SessionRuntimeTelemetryPoint> Points => _points;

    public IEnumerable<SessionRuntimeTelemetryPoint> Counters =>
        _points.Where(point => point.Kind == SessionRuntimeTelemetryKinds.Counter);

    public IEnumerable<SessionRuntimeTelemetryPoint> Durations =>
        _points.Where(point => point.Kind == SessionRuntimeTelemetryKinds.Histogram);

    public IEnumerable<string> AllLabelValues() =>
        _points.SelectMany(point => point.Labels.Values);

    public void Write(SessionRuntimeTelemetryPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        _points.Add(point);
    }
}

public sealed class SessionRuntimeTelemetry(ISessionRuntimeTelemetrySink? sink = null) : ISessionRuntimeTelemetry
{
    private static readonly HashSet<string> AllowedInstruments =
    [
        SessionRuntimeTelemetryInstruments.TriggerAdmission,
        SessionRuntimeTelemetryInstruments.InvocationCompletion,
        SessionRuntimeTelemetryInstruments.DecisionEffect,
        SessionRuntimeTelemetryInstruments.TimerRecommendation,
        SessionRuntimeTelemetryInstruments.TimerFire,
        SessionRuntimeTelemetryInstruments.TimerDrift,
        SessionRuntimeTelemetryInstruments.FragmentCommit,
        SessionRuntimeTelemetryInstruments.EventReplay,
        SessionRuntimeTelemetryInstruments.LifecycleChange,
        SessionRuntimeTelemetryInstruments.WorkClaim,
        SessionRuntimeTelemetryInstruments.WorkProcess,
        SessionRuntimeTelemetryInstruments.WorkBacklog,
        SessionRuntimeTelemetryInstruments.Fault,
        SessionRuntimeTelemetryInstruments.Rejected,
    ];

    private static readonly HashSet<string> AllowedLabelKeys =
    [
        SessionRuntimeTelemetryLabelKeys.Outcome,
        SessionRuntimeTelemetryLabelKeys.TriggerFamily,
        SessionRuntimeTelemetryLabelKeys.DecisionType,
        SessionRuntimeTelemetryLabelKeys.FirstFragment,
        SessionRuntimeTelemetryLabelKeys.WorkType,
        SessionRuntimeTelemetryLabelKeys.DelayBucket,
        SessionRuntimeTelemetryLabelKeys.BacklogBucket,
        SessionRuntimeTelemetryLabelKeys.PartitionBucket,
        SessionRuntimeTelemetryLabelKeys.FaultKind,
        SessionRuntimeTelemetryLabelKeys.Reason,
        SessionRuntimeTelemetryLabelKeys.Transition,
    ];

    private readonly ISessionRuntimeTelemetrySink _sink = sink ?? NoopSessionRuntimeTelemetrySink.Instance;

    public void RecordCounter(string instrument, IReadOnlyDictionary<string, string> labels, long delta = 1) =>
        Write(instrument, SessionRuntimeTelemetryKinds.Counter, delta, labels);

    public void RecordDuration(string instrument, TimeSpan duration, IReadOnlyDictionary<string, string> labels) =>
        Write(instrument, SessionRuntimeTelemetryKinds.Histogram, duration.TotalMilliseconds, labels);

    public void RecordGauge(string instrument, double value, IReadOnlyDictionary<string, string> labels) =>
        Write(instrument, SessionRuntimeTelemetryKinds.Gauge, value, labels);

    private void Write(
        string instrument,
        string kind,
        double value,
        IReadOnlyDictionary<string, string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        if (!AllowedInstruments.Contains(instrument))
        {
            WriteRejected(SessionRuntimeTelemetryValues.UnknownInstrument);
            return;
        }

        if (labels.Count > 6)
        {
            WriteRejected(SessionRuntimeTelemetryValues.ExcessiveLabels);
            return;
        }

        var sanitized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in labels)
        {
            if (!AllowedLabelKeys.Contains(pair.Key))
            {
                WriteRejected(SessionRuntimeTelemetryValues.UnknownLabelKey);
                return;
            }

            if (!IsAllowedValue(pair.Value))
            {
                WriteRejected(SessionRuntimeTelemetryValues.InvalidLabelValue);
                return;
            }

            sanitized[pair.Key] = pair.Value;
        }

        _sink.Write(new SessionRuntimeTelemetryPoint(instrument, kind, value, sanitized));
    }

    private void WriteRejected(string reason) =>
        _sink.Write(
            new SessionRuntimeTelemetryPoint(
                SessionRuntimeTelemetryInstruments.Rejected,
                SessionRuntimeTelemetryKinds.Counter,
                1,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [SessionRuntimeTelemetryLabelKeys.Reason] = reason,
                }));

    internal static bool IsAllowedValue(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64)
        {
            return false;
        }

        if (Guid.TryParse(value, out _))
        {
            return false;
        }

        if (value.Contains("sk-", StringComparison.OrdinalIgnoreCase)
            || value.Contains("bearer", StringComparison.OrdinalIgnoreCase)
            || value.Contains(' ')
            || value.Contains('@'))
        {
            return false;
        }

        if (value[0] is < 'a' or > 'z')
        {
            return false;
        }

        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}

public static class SessionRuntimeTelemetryBuckets
{
    public static string Count(int value) =>
        value switch
        {
            <= 0 => "n0",
            1 => "n1",
            <= 5 => "n2_to_5",
            <= 20 => "n6_to_20",
            <= 100 => "n21_to_100",
            _ => "n_over_100",
        };

    public static string Delay(TimeSpan drift)
    {
        var seconds = Math.Abs(drift.TotalSeconds);
        if (seconds < 1)
        {
            return "lt_1s";
        }

        if (seconds < 10)
        {
            return "s1_to_10";
        }

        if (seconds < 60)
        {
            return "s10_to_60";
        }

        if (seconds < 300)
        {
            return "m1_to_5";
        }

        return "over_5m";
    }
}

public static class SessionRuntimeLatencyObjectives
{
    public static TimeSpan AdmissionOrReconnectP95 { get; } = TimeSpan.FromSeconds(2);

    public static TimeSpan Percentile(IReadOnlyList<TimeSpan> samples, double percentile)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }

        if (percentile is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile));
        }

        var ordered = samples.OrderBy(sample => sample).ToArray();
        var index = (int)Math.Ceiling(percentile / 100d * ordered.Length) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
    }
}

internal static class SessionRuntimeTelemetryRecording
{
    public static Dictionary<string, string> Labels(params (string Key, string? Value)[] pairs)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                labels[key] = value;
            }
        }

        return labels;
    }
}
