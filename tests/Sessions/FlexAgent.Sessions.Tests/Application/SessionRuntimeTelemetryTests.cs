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
    public void Sanitizer_rejects_unbounded_values_under_allowed_keys()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);

        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.TriggerAdmission,
            new Dictionary<string, string>
            {
                [SessionRuntimeTelemetryLabelKeys.Outcome] = "hello",
                [SessionRuntimeTelemetryLabelKeys.TriggerFamily] = RuntimeTriggerIdentifiers.WorkflowEventFamily,
            });
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.TriggerAdmission,
            new Dictionary<string, string>
            {
                [SessionRuntimeTelemetryLabelKeys.Outcome] = TriggerAdmissionOutcomeCodes.Succeeded,
                [SessionRuntimeTelemetryLabelKeys.TriggerFamily] = "participant_name",
            });
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.Fault,
            new Dictionary<string, string>
            {
                [SessionRuntimeTelemetryLabelKeys.Outcome] = "password",
                [SessionRuntimeTelemetryLabelKeys.FaultKind] = SessionRuntimeTelemetryValues.Audit,
            });
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkProcess,
            new Dictionary<string, string>
            {
                [SessionRuntimeTelemetryLabelKeys.Outcome] = "ghp_pat_not_openai",
                [SessionRuntimeTelemetryLabelKeys.WorkType] = DurableSessionWorkTypes.ExecuteInvocation,
            });
        telemetry.RecordCounter(
            SessionRuntimeTelemetryInstruments.WorkProcess,
            new Dictionary<string, string>
            {
                [SessionRuntimeTelemetryLabelKeys.Outcome] = "aws_secret_access_key",
                [SessionRuntimeTelemetryLabelKeys.WorkType] = DurableSessionWorkTypes.ExecuteInvocation,
            });

        for (var index = 0; index < 1000; index++)
        {
            telemetry.RecordCounter(
                SessionRuntimeTelemetryInstruments.TriggerAdmission,
                new Dictionary<string, string>
                {
                    [SessionRuntimeTelemetryLabelKeys.Outcome] = $"hello_{index}",
                    [SessionRuntimeTelemetryLabelKeys.TriggerFamily] = RuntimeTriggerIdentifiers.WorkflowEventFamily,
                });
        }

        Assert.DoesNotContain(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.TriggerAdmission
                && item.Labels.GetValueOrDefault(SessionRuntimeTelemetryLabelKeys.Outcome) == "hello");
        Assert.DoesNotContain(
            sink.Counters,
            item => item.Labels.GetValueOrDefault(SessionRuntimeTelemetryLabelKeys.TriggerFamily)
                == "participant_name");
        Assert.Equal(1005, sink.Counters.Count(item => item.Instrument == SessionRuntimeTelemetryInstruments.Rejected));
        Assert.DoesNotContain(sink.AllLabelValues(), value => value.StartsWith("hello_", StringComparison.Ordinal));
        Assert.DoesNotContain(sink.AllLabelValues(), value => value.Contains("ghp_pat", StringComparison.Ordinal));
        Assert.DoesNotContain(sink.AllLabelValues(), value => value.Contains("aws_secret", StringComparison.Ordinal));
        Assert.DoesNotContain(sink.AllLabelValues(), value => value == "password");
    }

    [Fact]
    public void Admission_coerces_unknown_trigger_family_to_a_bounded_token()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = new AdmitTrustedTriggerCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion,
            new TrustedTrigger(
                "participant_name",
                RuntimeTriggerIdentifiers.AgentOpeningType,
                "trig.unknown.family",
                InvocationPurposes.AgentOpening,
                null,
                null),
            "idem.open.unknown.family",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "application.test");

        var result = new AdmitTrustedTriggerHandler(telemetry).Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        var point = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.TriggerAdmission);
        Assert.Equal(TriggerAdmissionOutcomeCodes.UnknownTrigger, point.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
        Assert.Equal(SessionRuntimeTelemetryValues.Unknown, point.Labels[SessionRuntimeTelemetryLabelKeys.TriggerFamily]);
        Assert.DoesNotContain(sink.AllLabelValues(), value => value == "participant_name");
    }

    [Fact]
    public void Unknown_lifecycle_transition_records_denied_with_a_bounded_token()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = new ChangeSessionLifecycleCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion,
            "destroy_everything",
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "application.test");

        var result = new ChangeSessionLifecycleHandler(telemetry).Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Equal(SessionLifecycleOutcomeCodes.Denied, result.OutcomeCode);
        var point = Assert.Single(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.LifecycleChange);
        Assert.Equal(SessionLifecycleOutcomeCodes.Denied, point.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
        Assert.Equal(SessionRuntimeTelemetryValues.Unknown, point.Labels[SessionRuntimeTelemetryLabelKeys.Transition]);
        Assert.DoesNotContain(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.Rejected);
        Assert.DoesNotContain(sink.AllLabelValues(), value => value == "destroy_everything");
    }

    [Fact]
    public void Prohibited_open_set_decision_type_records_a_bounded_token()
    {
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.prohibited.telemetry",
            "turn.1",
            "slot.1",
            "trig.participant.prohibited.telemetry",
            "idem.p.prohibited.telemetry",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var command = new CompleteInvocationCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion,
            invocationId,
            new ProhibitedDecisionRecommendation(
                Guid.NewGuid().ToString("N"),
                invocationId,
                SessionRuntimeTestFixtures.T0.AddSeconds(2),
                "customer_secret_action",
                null),
            null,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "application.test");

        var result = new CompleteInvocationHandler(telemetry).Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        var completion = Assert.Single(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.InvocationCompletion);
        Assert.Equal(InvocationCompletionOutcomeCodes.Decided, completion.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
        Assert.Equal(SessionRuntimeTelemetryValues.Unknown, completion.Labels[SessionRuntimeTelemetryLabelKeys.DecisionType]);
        Assert.DoesNotContain(sink.Counters, item => item.Instrument == SessionRuntimeTelemetryInstruments.Rejected);
        Assert.DoesNotContain(sink.AllLabelValues(), value => value == "customer_secret_action");
    }

    [Fact]
    public void Sink_exceptions_do_not_escape_the_telemetry_boundary()
    {
        var telemetry = new SessionRuntimeTelemetry(new ThrowingSessionRuntimeTelemetrySink());

        var exception = Record.Exception(() =>
            telemetry.RecordCounter(
                SessionRuntimeTelemetryInstruments.TriggerAdmission,
                new Dictionary<string, string>
                {
                    [SessionRuntimeTelemetryLabelKeys.Outcome] = TriggerAdmissionOutcomeCodes.Succeeded,
                    [SessionRuntimeTelemetryLabelKeys.TriggerFamily] = RuntimeTriggerIdentifiers.WorkflowEventFamily,
                }));

        Assert.Null(exception);
        Assert.Null(
            Record.Exception(() =>
                telemetry.RecordCounter(
                    SessionRuntimeTelemetryInstruments.TriggerAdmission,
                    new Dictionary<string, string> { ["text"] = "hello" })));
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
        Assert.True(
            SessionRuntimeTelemetry.IsAllowedLabel(
                SessionRuntimeTelemetryLabelKeys.BacklogBucket,
                SessionRuntimeTelemetryBuckets.Count(0)));
        Assert.True(
            SessionRuntimeTelemetry.IsAllowedLabel(
                SessionRuntimeTelemetryLabelKeys.BacklogBucket,
                SessionRuntimeTelemetryBuckets.Count(3)));
        Assert.True(
            SessionRuntimeTelemetry.IsAllowedLabel(
                SessionRuntimeTelemetryLabelKeys.DelayBucket,
                SessionRuntimeTelemetryBuckets.Delay(TimeSpan.FromMilliseconds(200))));
        Assert.True(
            SessionRuntimeTelemetry.IsAllowedLabel(
                SessionRuntimeTelemetryLabelKeys.DelayBucket,
                SessionRuntimeTelemetryBuckets.Delay(TimeSpan.FromMinutes(8))));
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

    private sealed class ThrowingSessionRuntimeTelemetrySink : ISessionRuntimeTelemetrySink
    {
        public void Write(SessionRuntimeTelemetryPoint point) =>
            throw new InvalidOperationException("telemetry sink failed");
    }
}
