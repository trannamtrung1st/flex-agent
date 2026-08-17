using System.Diagnostics;
using System.Diagnostics.Metrics;
using FlexAgent.Sessions.Application;

namespace FlexAgent.Worker;

public sealed class MeterSessionRuntimeTelemetrySink : ISessionRuntimeTelemetrySink, IDisposable
{
    public const string MeterName = "FlexAgent.Sessions.Runtime";

    private readonly Meter _meter;
    private readonly bool _ownsMeter;
    private readonly object _instruments = new();
    private readonly Dictionary<string, Counter<double>> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Histogram<double>> _histograms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Gauge<double>> _gauges = new(StringComparer.Ordinal);

    public MeterSessionRuntimeTelemetrySink()
        : this(new Meter(MeterName), ownsMeter: true)
    {
    }

    public MeterSessionRuntimeTelemetrySink(Meter meter)
        : this(meter, ownsMeter: false)
    {
    }

    private MeterSessionRuntimeTelemetrySink(Meter meter, bool ownsMeter)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        _ownsMeter = ownsMeter;
    }

    public void Write(SessionRuntimeTelemetryPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var tags = ToTags(point.Labels);
        switch (point.Kind)
        {
            case SessionRuntimeTelemetryKinds.Counter:
                GetCounter(point.Instrument).Add(point.Value, tags);
                break;
            case SessionRuntimeTelemetryKinds.Histogram:
                GetHistogram(point.Instrument).Record(point.Value, tags);
                break;
            case SessionRuntimeTelemetryKinds.Gauge:
                GetGauge(point.Instrument).Record(point.Value, tags);
                break;
        }
    }

    public void Dispose()
    {
        if (_ownsMeter)
        {
            _meter.Dispose();
        }
    }

    private Counter<double> GetCounter(string instrument)
    {
        lock (_instruments)
        {
            if (!_counters.TryGetValue(instrument, out var counter))
            {
                counter = _meter.CreateCounter<double>(instrument);
                _counters[instrument] = counter;
            }

            return counter;
        }
    }

    private Histogram<double> GetHistogram(string instrument)
    {
        var name = instrument + ".duration";
        lock (_instruments)
        {
            if (!_histograms.TryGetValue(name, out var histogram))
            {
                histogram = _meter.CreateHistogram<double>(name, unit: "ms");
                _histograms[name] = histogram;
            }

            return histogram;
        }
    }

    private Gauge<double> GetGauge(string instrument)
    {
        lock (_instruments)
        {
            if (!_gauges.TryGetValue(instrument, out var gauge))
            {
                gauge = _meter.CreateGauge<double>(instrument);
                _gauges[instrument] = gauge;
            }

            return gauge;
        }
    }

    private static TagList ToTags(IReadOnlyDictionary<string, string> labels)
    {
        var tags = new TagList();
        foreach (var pair in labels)
        {
            tags.Add(pair.Key, pair.Value);
        }

        return tags;
    }
}
