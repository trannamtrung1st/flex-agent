using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class SessionRuntimeTelemetryTests
{
    [Fact]
    public void Admission_records_bounded_trigger_family_and_outcome_without_scope_identifiers()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = new AdmitTrustedTriggerCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion,
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open.telemetry",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "application.test");

        var result = new AdmitTrustedTriggerHandler(telemetry).Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0);

        Assert.True(result.Succeeded);
        var point = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.TriggerAdmission);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, point.Labels["outcome"]);
        Assert.Equal(RuntimeTriggerIdentifiers.WorkflowEventFamily, point.Labels["trigger_family"]);
        Assert.DoesNotContain(point.Labels.Values, value => value.Contains(session.Ownership.OrganizationId.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(point.Labels.Values, value => value.Contains(command.IdempotencyKey, StringComparison.Ordinal));
        Assert.Contains(sink.Durations, item => item.Instrument == SessionRuntimeTelemetryInstruments.TriggerAdmission);
    }

    [Fact]
    public void Completion_records_no_action_and_timer_validation_without_decision_payload()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.telemetry",
            "turn.1",
            "slot.1",
            "trig.participant.telemetry",
            "idem.p.telemetry",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var command = new CompleteInvocationCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion,
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            null,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new CompleteInvocationHandler(telemetry).Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded);
        var completion = Assert.Single(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.InvocationCompletion);
        Assert.Equal(InvocationCompletionOutcomeCodes.Decided, completion.Labels["outcome"]);
        Assert.Equal(RuntimeDecisionTypes.NoAction, completion.Labels["decision_type"]);
        var timer = Assert.Single(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.TimerRecommendation);
        Assert.Equal(TimerValidationOutcomes.NotPresent, timer.Labels["outcome"]);
        Assert.DoesNotContain(
            sink.AllLabelValues(),
            value => value.Contains("intentional_silence", StringComparison.Ordinal) && value.Contains("payload", StringComparison.Ordinal));
        Assert.DoesNotContain(sink.AllLabelValues(), value => value.Contains(invocationId, StringComparison.Ordinal));
    }

    [Fact]
    public void Sanitizer_rejects_unrestricted_identifiers_content_and_credentials()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);

        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.TriggerAdmission,
            new Dictionary<string, string>
            {
                ["organization_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                ["outcome"] = TriggerAdmissionOutcomeCodes.Succeeded,
            });
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.FragmentCommit,
            new Dictionary<string, string>
            {
                ["outcome"] = FragmentCommitOutcomeCodes.Succeeded,
                ["text"] = "hello participant transcript",
            });
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkProcess,
            new Dictionary<string, string>
            {
                ["outcome"] = DurableInvocationWorkOutcomes.Decided,
                ["credential"] = "sk-live-secret-value",
            });

        Assert.DoesNotContain(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.TriggerAdmission);
        Assert.DoesNotContain(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.FragmentCommit);
        Assert.DoesNotContain(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.WorkProcess);
        Assert.Equal(3, sink.Counters.Count(item => item.Instrument == SessionRuntimeTelemetryInstruments.Rejected));
        Assert.All(
            sink.Counters.Where(item => item.Instrument == SessionRuntimeTelemetryInstruments.Rejected),
            item => Assert.DoesNotContain("sk-live-secret-value", item.Labels.Values));
        Assert.DoesNotContain(sink.AllLabelValues(), value => value.Contains("hello participant", StringComparison.Ordinal));
        Assert.DoesNotContain(
            sink.AllLabelValues(),
            value => Guid.TryParse(value, out _));
    }

    [Fact]
    public void Duplicate_stale_and_late_completion_outcomes_are_distinct_categories()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.stale",
            "turn.1",
            "slot.1",
            "trig.participant.stale",
            "idem.p.stale",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var stale = new CompleteInvocationHandler(telemetry).Handle(
            new CompleteInvocationCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                session.SessionVersion + 4,
                invocationId,
                SessionRuntimeTestFixtures.NoAction(invocationId),
                null,
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "application.test"),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(InvocationCompletionOutcomeCodes.StaleVersion, stale.OutcomeCode);
        Assert.Contains(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.InvocationCompletion
                && item.Labels["outcome"] == InvocationCompletionOutcomeCodes.StaleVersion);
    }

    [Fact]
    public void Delay_and_count_buckets_are_bounded_label_values()
    {
        Assert.True(SessionRuntimeTelemetry.IsAllowedValue(SessionRuntimeTelemetryBuckets.Count(0)));
        Assert.True(SessionRuntimeTelemetry.IsAllowedValue(SessionRuntimeTelemetryBuckets.Count(3)));
        Assert.True(SessionRuntimeTelemetry.IsAllowedValue(SessionRuntimeTelemetryBuckets.Delay(TimeSpan.FromMilliseconds(200))));
        Assert.True(SessionRuntimeTelemetry.IsAllowedValue(SessionRuntimeTelemetryBuckets.Delay(TimeSpan.FromMinutes(8))));
        Assert.Equal("n0", SessionRuntimeTelemetryBuckets.Count(0));
        Assert.Equal("over_5m", SessionRuntimeTelemetryBuckets.Delay(TimeSpan.FromMinutes(8)));
    }

    [Fact]
    public void Replay_records_outcome_without_projected_text()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var result = new ReplayAuthorizedSessionEventsHandler(telemetry).Handle(
            new ReplayAuthorizedSessionEventsCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                null),
            session);

        Assert.True(result.Succeeded);
        var point = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.EventReplay);
        Assert.Equal(SessionEventReplayOutcomeCodes.Succeeded, point.Labels["outcome"]);
        Assert.DoesNotContain(point.Labels.Keys, key => key.Contains("text", StringComparison.Ordinal));
    }
}
