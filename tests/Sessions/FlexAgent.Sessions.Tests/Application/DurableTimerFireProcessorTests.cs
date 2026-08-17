using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class DurableTimerFireProcessorTests
{
    [Fact]
    public async Task Idle_port_does_not_retry()
    {
        var port = new ScriptedDueTimerFirePort(new TimerFireResult(false, TimerFireOutcomeCodes.Idle));
        var processor = CreateProcessor(port);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Idle, result.Outcome);
        Assert.Equal(1, port.CallCount);
    }

    [Fact]
    public async Task Budget_exhausted_is_a_durable_acknowledgement_and_is_not_retried()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(false, TimerFireOutcomeCodes.BudgetExhausted));
        var processor = CreateProcessor(port);

        var first = await processor.TryProcessNextAsync(CancellationToken.None);
        var second = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Acknowledged, first.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.BudgetExhausted, first.TimerOutcomeCode);
        Assert.NotEqual(DurableTimerFireOutcomes.RetryLater, first.Outcome);
        Assert.Equal(DurableTimerFireOutcomes.Idle, second.Outcome);
        Assert.Equal(2, port.CallCount);
    }

    [Fact]
    public async Task Lifecycle_ineligible_is_a_durable_acknowledgement_and_is_not_retried()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(false, TimerFireOutcomeCodes.LifecycleIneligible));
        var processor = CreateProcessor(port);

        var first = await processor.TryProcessNextAsync(CancellationToken.None);
        var second = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Acknowledged, first.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.LifecycleIneligible, first.TimerOutcomeCode);
        Assert.NotEqual(DurableTimerFireOutcomes.RetryLater, first.Outcome);
        Assert.Equal(DurableTimerFireOutcomes.Idle, second.Outcome);
        Assert.Equal(2, port.CallCount);
    }

    [Fact]
    public async Task Successful_fire_is_processed()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(true, TimerFireOutcomeCodes.Succeeded));
        var processor = CreateProcessor(port);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.Fired, result.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, result.TimerOutcomeCode);
    }

    [Fact]
    public async Task Stale_revision_retries_later()
    {
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(false, TimerFireOutcomeCodes.StaleRevision));
        var processor = CreateProcessor(port);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableTimerFireOutcomes.RetryLater, result.Outcome);
        Assert.Equal(TimerFireOutcomeCodes.StaleRevision, result.TimerOutcomeCode);
    }

    [Fact]
    public async Task Fire_outcomes_are_recorded_with_bounded_timer_labels()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var port = new ScriptedDueTimerFirePort(
            new TimerFireResult(false, TimerFireOutcomeCodes.BudgetExhausted));
        var processor = CreateProcessor(port, telemetry);

        await processor.TryProcessNextAsync(CancellationToken.None);

        var point = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.TimerFire);
        Assert.Equal(TimerFireOutcomeCodes.BudgetExhausted, point.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
    }

    [Fact]
    public async Task Idle_fire_uses_the_timer_fire_outcome_code()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var processor = CreateProcessor(
            new ScriptedDueTimerFirePort(new TimerFireResult(false, TimerFireOutcomeCodes.Idle)),
            telemetry);

        await processor.TryProcessNextAsync(CancellationToken.None);

        var point = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.TimerFire);
        Assert.Equal(TimerFireOutcomeCodes.Idle, point.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
    }

    [Fact]
    public async Task Observed_due_clock_records_a_bounded_drift_bucket()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var dueAt = SessionRuntimeTestFixtures.T0;
        var revision = TimerScheduleRevision.Rehydrate(
            "tsr.obs.1",
            1,
            TimerLaneStates.Fired,
            "PT5M",
            0,
            dueAt,
            dueAt,
            TimerRequestedByCategories.DefaultCadence,
            null,
            null,
            dueAt);
        var processor = CreateProcessor(
            new ScriptedDueTimerFirePort(
                new TimerFireResult(
                    true,
                    TimerFireOutcomeCodes.Succeeded,
                    revision,
                    ObservedAt: dueAt.AddSeconds(2))),
            telemetry);

        await processor.TryProcessNextAsync(CancellationToken.None);

        var drift = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.TimerDrift);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, drift.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
        Assert.Equal("s1_to_10", drift.Labels[SessionRuntimeTelemetryLabelKeys.DelayBucket]);
    }

    private static DurableTimerFireProcessor CreateProcessor(
        IDueTimerFirePort port,
        ISessionRuntimeTelemetry? telemetry = null) =>
        new(
            port,
            new DurableTimerFireSettings(SessionRuntimeTestFixtures.CreateActor(), "application.test"),
            telemetry);

    private sealed class ScriptedDueTimerFirePort(params TimerFireResult[] results) : IDueTimerFirePort
    {
        private int _index;

        public int CallCount { get; private set; }

        public Task<TimerFireResult> TryFireNextDueAsync(
            FireDueTimerCommand command,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (_index >= results.Length)
            {
                return Task.FromResult(new TimerFireResult(false, TimerFireOutcomeCodes.Idle));
            }

            return Task.FromResult(results[_index++]);
        }
    }
}
