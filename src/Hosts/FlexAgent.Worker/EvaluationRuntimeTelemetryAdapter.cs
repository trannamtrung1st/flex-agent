using FlexAgent.Evaluation.Application;
using FlexAgent.Sessions.Application;

namespace FlexAgent.Worker;

internal sealed class EvaluationRuntimeTelemetryAdapter(ISessionRuntimeTelemetry telemetry) : IEvaluationRuntimeTelemetry
{
    public void RecordWorkClaim(string outcome) =>
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkClaim,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.Outcome, outcome),
                (SessionRuntimeTelemetryLabelKeys.WorkType, EvaluationDurableWorkTypes.ExecuteRequest)));

    public void RecordWorkProcess(string outcome) =>
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkProcess,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.Outcome, outcome),
                (SessionRuntimeTelemetryLabelKeys.WorkType, EvaluationDurableWorkTypes.ExecuteRequest)));

    public void RecordWorkBacklog(int claimableCount, int partitionCount) =>
        telemetry.RecordGauge(
            SessionRuntimeTelemetryInstruments.WorkBacklog,
            claimableCount,
            SessionRuntimeTelemetryRecording.Labels(
                (SessionRuntimeTelemetryLabelKeys.WorkType, EvaluationDurableWorkTypes.ExecuteRequest),
                (SessionRuntimeTelemetryLabelKeys.BacklogBucket, SessionRuntimeTelemetryBuckets.Count(claimableCount)),
                (SessionRuntimeTelemetryLabelKeys.PartitionBucket, SessionRuntimeTelemetryBuckets.Count(partitionCount))));
}
