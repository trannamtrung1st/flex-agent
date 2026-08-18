using FlexAgent.Sessions.Domain;

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
    public const string Unknown = "unknown";
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

    public IEnumerable<SessionRuntimeTelemetryPoint> Gauges =>
        _points.Where(point => point.Kind == SessionRuntimeTelemetryKinds.Gauge);

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

            if (!SessionRuntimeTelemetryVocabularies.IsAllowed(pair.Key, pair.Value))
            {
                WriteRejected(SessionRuntimeTelemetryValues.InvalidLabelValue);
                return;
            }

            sanitized[pair.Key] = pair.Value;
        }

        Emit(new SessionRuntimeTelemetryPoint(instrument, kind, value, sanitized));
    }

    private void WriteRejected(string reason) =>
        Emit(
            new SessionRuntimeTelemetryPoint(
                SessionRuntimeTelemetryInstruments.Rejected,
                SessionRuntimeTelemetryKinds.Counter,
                1,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [SessionRuntimeTelemetryLabelKeys.Reason] = reason,
                }));

    private void Emit(SessionRuntimeTelemetryPoint point)
    {
        try
        {
            _sink.Write(point);
        }
        catch
        {
        }
    }

    internal static bool IsAllowedLabel(string key, string value) =>
        SessionRuntimeTelemetryVocabularies.IsAllowed(key, value);
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
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            labels[key] = SessionRuntimeTelemetryVocabularies.CanonicalOpenSet(key, value);
        }

        return labels;
    }
}

internal static class SessionRuntimeTelemetryVocabularies
{
    private static readonly HashSet<string> TriggerFamilies = new(StringComparer.Ordinal)
    {
        RuntimeTriggerIdentifiers.ParticipantInputFamily,
        RuntimeTriggerIdentifiers.WorkflowEventFamily,
        RuntimeTriggerIdentifiers.TimerEventFamily,
        SessionRuntimeTelemetryValues.Unknown,
    };

    private static readonly HashSet<string> Outcomes =
    [
        TriggerAdmissionOutcomeCodes.Succeeded,
        TriggerAdmissionOutcomeCodes.Reconciled,
        TriggerAdmissionOutcomeCodes.UnknownTrigger,
        TriggerAdmissionOutcomeCodes.ProhibitedTrigger,
        TriggerAdmissionOutcomeCodes.LifecycleIneligible,
        TriggerAdmissionOutcomeCodes.BudgetExhausted,
        TriggerAdmissionOutcomeCodes.CooldownActive,
        TriggerAdmissionOutcomeCodes.IdempotencyConflict,
        TriggerAdmissionOutcomeCodes.OwnershipMismatch,
        TriggerAdmissionOutcomeCodes.StaleVersion,
        TriggerAdmissionOutcomeCodes.NonUtcClock,
        TriggerAdmissionOutcomeCodes.StaleClock,
        TriggerAdmissionOutcomeCodes.MissingTurn,
        TriggerAdmissionOutcomeCodes.Denied,
        InvocationCompletionOutcomeCodes.Decided,
        InvocationCompletionOutcomeCodes.ExecutionFailed,
        InvocationCompletionOutcomeCodes.AttemptsExhausted,
        InvocationCompletionOutcomeCodes.LateResult,
        InvocationCompletionOutcomeCodes.AlreadyTerminal,
        InvocationCompletionOutcomeCodes.EffectFailed,
        InvocationCompletionOutcomeCodes.AttemptRecorded,
        InvocationCompletionOutcomeCodes.IdentityMismatch,
        InvocationCompletionOutcomeCodes.NonUtcClock,
        InvocationCompletionOutcomeCodes.StaleClock,
        InvocationCompletionOutcomeCodes.StaleVersion,
        InvocationCompletionOutcomeCodes.Denied,
        InvocationCompletionOutcomeCodes.OwnershipMismatch,
        TimerFireOutcomeCodes.Succeeded,
        TimerFireOutcomeCodes.Reconciled,
        TimerFireOutcomeCodes.Idle,
        TimerFireOutcomeCodes.NotDue,
        TimerFireOutcomeCodes.LifecycleIneligible,
        TimerFireOutcomeCodes.BudgetExhausted,
        TimerFireOutcomeCodes.NonUtcClock,
        TimerFireOutcomeCodes.StaleClock,
        TimerFireOutcomeCodes.StaleRevision,
        TimerFireOutcomeCodes.AuthorityDenied,
        SessionLifecycleOutcomeCodes.Succeeded,
        SessionLifecycleOutcomeCodes.Reconciled,
        SessionLifecycleOutcomeCodes.Denied,
        SessionLifecycleOutcomeCodes.OwnershipMismatch,
        SessionLifecycleOutcomeCodes.StaleVersion,
        SessionLifecycleOutcomeCodes.LifecycleIneligible,
        FragmentCommitOutcomeCodes.Succeeded,
        FragmentCommitOutcomeCodes.Reconciled,
        FragmentCommitOutcomeCodes.Gap,
        FragmentCommitOutcomeCodes.DigestMismatch,
        FragmentCommitOutcomeCodes.CompetingAttempt,
        FragmentCommitOutcomeCodes.Cutoff,
        FragmentCommitOutcomeCodes.PublicationNotClaimed,
        FragmentCommitOutcomeCodes.EmptyDelta,
        FragmentCommitOutcomeCodes.AlreadyTerminal,
        FragmentCommitOutcomeCodes.NonUtcClock,
        FragmentCommitOutcomeCodes.StaleClock,
        FragmentCommitOutcomeCodes.StaleVersion,
        FragmentCommitOutcomeCodes.Denied,
        FragmentCommitOutcomeCodes.OwnershipMismatch,
        FragmentCommitOutcomeCodes.FragmentTooLarge,
        FragmentCommitOutcomeCodes.FragmentCountExceeded,
        FragmentCommitOutcomeCodes.AssembledSizeExceeded,
        FragmentCommitOutcomeCodes.InFlightExceeded,
        FragmentCommitOutcomeCodes.RateExceeded,
        FragmentCommitOutcomeCodes.ValidationFailed,
        FragmentCommitOutcomeCodes.UnpublishedFailed,
        SessionEventReplayOutcomeCodes.Succeeded,
        SessionEventReplayOutcomeCodes.Reconcile,
        SessionEventReplayOutcomeCodes.Denied,
        SessionEventReplayOutcomeCodes.OwnershipMismatch,
        DurableInvocationWorkOutcomes.Idle,
        DurableInvocationWorkOutcomes.Decided,
        DurableInvocationWorkOutcomes.ExecutionFailed,
        DurableInvocationWorkOutcomes.Reconciled,
        DurableInvocationWorkOutcomes.RetryLater,
        DurableInvocationWorkOutcomes.Published,
        DurableInvocationWorkOutcomes.PublicationIncomplete,
        DurableInvocationWorkOutcomes.PublicationFailed,
        DecisionEffectOutcomes.Applied,
        DecisionEffectOutcomes.NoDomainEffect,
        DecisionEffectOutcomes.EffectFailed,
        DecisionEffectOutcomes.NotAttempted,
        TimerValidationOutcomes.Accepted,
        TimerValidationOutcomes.Rejected,
        TimerValidationOutcomes.Omitted,
        TimerValidationOutcomes.NotPresent,
        SessionRuntimeTelemetryValues.Claimed,
        SessionRuntimeTelemetryValues.Succeeded,
        SessionRuntimeTelemetryValues.Failed,
    ];

    private static readonly HashSet<string> DecisionTypes =
    [
        RuntimeDecisionTypes.EmitMessage,
        RuntimeDecisionTypes.NoAction,
        RuntimeDecisionTypes.RequestTool,
        RuntimeDecisionTypes.ProposeTransition,
        RuntimeDecisionTypes.Escalate,
        SessionRuntimeTelemetryValues.Unknown,
    ];

    private static readonly HashSet<string> YesNo =
    [
        SessionRuntimeTelemetryValues.Yes,
        SessionRuntimeTelemetryValues.No,
    ];

    private static readonly HashSet<string> WorkTypes =
    [
        DurableSessionWorkTypes.ExecuteInvocation,
    ];

    private static readonly HashSet<string> DelayBuckets =
    [
        "lt_1s",
        "s1_to_10",
        "s10_to_60",
        "m1_to_5",
        "over_5m",
    ];

    private static readonly HashSet<string> CountBuckets =
    [
        "n0",
        "n1",
        "n2_to_5",
        "n6_to_20",
        "n21_to_100",
        "n_over_100",
    ];

    private static readonly HashSet<string> FaultKinds =
    [
        SessionRuntimeTelemetryValues.Audit,
        SessionRuntimeTelemetryValues.Outbox,
        SessionRuntimeTelemetryValues.Manifest,
    ];

    private static readonly HashSet<string> RejectionReasons =
    [
        SessionRuntimeTelemetryValues.UnknownInstrument,
        SessionRuntimeTelemetryValues.UnknownLabelKey,
        SessionRuntimeTelemetryValues.InvalidLabelValue,
        SessionRuntimeTelemetryValues.ExcessiveLabels,
    ];

    private static readonly HashSet<string> Transitions =
    [
        SessionLifecycleTransitions.Pause,
        SessionLifecycleTransitions.Resume,
        SessionLifecycleTransitions.BeginCompleting,
        SessionLifecycleTransitions.Complete,
        SessionLifecycleTransitions.Terminate,
        SessionLifecycleTransitions.Abort,
        SessionRuntimeTelemetryValues.Unknown,
    ];

    private static readonly Dictionary<string, HashSet<string>> ByKey = new(StringComparer.Ordinal)
    {
        [SessionRuntimeTelemetryLabelKeys.Outcome] = Outcomes,
        [SessionRuntimeTelemetryLabelKeys.TriggerFamily] = TriggerFamilies,
        [SessionRuntimeTelemetryLabelKeys.DecisionType] = DecisionTypes,
        [SessionRuntimeTelemetryLabelKeys.FirstFragment] = YesNo,
        [SessionRuntimeTelemetryLabelKeys.WorkType] = WorkTypes,
        [SessionRuntimeTelemetryLabelKeys.DelayBucket] = DelayBuckets,
        [SessionRuntimeTelemetryLabelKeys.BacklogBucket] = CountBuckets,
        [SessionRuntimeTelemetryLabelKeys.PartitionBucket] = CountBuckets,
        [SessionRuntimeTelemetryLabelKeys.FaultKind] = FaultKinds,
        [SessionRuntimeTelemetryLabelKeys.Reason] = RejectionReasons,
        [SessionRuntimeTelemetryLabelKeys.Transition] = Transitions,
    };

    internal static bool IsAllowed(string key, string value) =>
        ByKey.TryGetValue(key, out var allowed) && allowed.Contains(value);

    private static readonly HashSet<string> OpenSetKeys = new(StringComparer.Ordinal)
    {
        SessionRuntimeTelemetryLabelKeys.TriggerFamily,
        SessionRuntimeTelemetryLabelKeys.DecisionType,
        SessionRuntimeTelemetryLabelKeys.Transition,
    };

    internal static string CanonicalOpenSet(string key, string value) =>
        OpenSetKeys.Contains(key) && !IsAllowed(key, value)
            ? SessionRuntimeTelemetryValues.Unknown
            : value;
}
