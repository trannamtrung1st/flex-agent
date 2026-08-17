using System.Diagnostics.Metrics;
using FlexAgent.Sessions.Application;
using FlexAgent.Worker;

namespace FlexAgent.Runtime.Tests;

public sealed class MeterSessionRuntimeTelemetrySinkTests
{
    [Fact]
    public void Write_records_counters_histograms_and_gauges_on_distinct_meter_instruments()
    {
        var counters = new List<double>();
        var durations = new List<double>();
        var gauges = new List<double>();
        using var meter = new Meter("FlexAgent.Runtime.Tests.MeterSink");
        using var sink = new MeterSessionRuntimeTelemetrySink(meter);
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter != meter)
            {
                return;
            }

            meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
        {
            if (instrument.Name == SessionRuntimeTelemetryInstruments.TriggerAdmission)
            {
                counters.Add(value);
            }
            else if (instrument.Name == SessionRuntimeTelemetryInstruments.TriggerAdmission + ".duration")
            {
                durations.Add(value);
            }
            else if (instrument.Name == SessionRuntimeTelemetryInstruments.WorkBacklog)
            {
                gauges.Add(value);
            }
        });
        listener.Start();

        sink.Write(
            new SessionRuntimeTelemetryPoint(
                SessionRuntimeTelemetryInstruments.TriggerAdmission,
                SessionRuntimeTelemetryKinds.Counter,
                1,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [SessionRuntimeTelemetryLabelKeys.Outcome] = SessionRuntimeTelemetryValues.Succeeded,
                }));
        sink.Write(
            new SessionRuntimeTelemetryPoint(
                SessionRuntimeTelemetryInstruments.TriggerAdmission,
                SessionRuntimeTelemetryKinds.Histogram,
                15,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [SessionRuntimeTelemetryLabelKeys.Outcome] = SessionRuntimeTelemetryValues.Succeeded,
                }));
        sink.Write(
            new SessionRuntimeTelemetryPoint(
                SessionRuntimeTelemetryInstruments.WorkBacklog,
                SessionRuntimeTelemetryKinds.Gauge,
                7,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [SessionRuntimeTelemetryLabelKeys.WorkType] = "invocation.execute",
                    [SessionRuntimeTelemetryLabelKeys.BacklogBucket] = "n6_to_20",
                    [SessionRuntimeTelemetryLabelKeys.PartitionBucket] = "n1",
                }));

        Assert.Equal([1d], counters);
        Assert.Equal([15d], durations);
        Assert.Equal([7d], gauges);
    }
}
