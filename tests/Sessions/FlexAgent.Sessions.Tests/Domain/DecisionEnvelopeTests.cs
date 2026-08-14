using System.Text;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class DecisionEnvelopeTests
{
    [Fact]
    public void Historical_emit_message_maps_to_respond_and_one_message_output()
    {
        var emit = SessionRuntimeTestFixtures.EmitMessage("ainv.1");
        var envelope = HistoricalDecisionEnvelopeMapper.ToEnvelope(emit);

        Assert.Equal(DecisionDispositions.Respond, envelope.Disposition);
        var output = Assert.Single(envelope.Outputs);
        Assert.Equal(AgentOutputKinds.Message, output.Kind);
        Assert.Equal("participant_reply", output.CommunicationPurpose);
        Assert.Empty(envelope.RequestedActions);
    }

    [Fact]
    public void Historical_no_action_maps_to_explicit_disposition_and_zero_outputs()
    {
        var noAction = SessionRuntimeTestFixtures.NoAction("ainv.1");
        var envelope = HistoricalDecisionEnvelopeMapper.ToEnvelope(noAction);

        Assert.Equal(DecisionDispositions.NoAction, envelope.Disposition);
        Assert.Empty(envelope.Outputs);
        Assert.Equal(NoActionReasonCategories.IntentionalSilence, envelope.NoActionReasonCategory);
    }

    [Fact]
    public void Mixed_message_and_voice_profile_accepts_message_and_rejects_voice()
    {
        var envelope = SessionRuntimeTestFixtures.Envelope(
            "ainv.1",
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(),
                SessionRuntimeTestFixtures.VoiceOutput(),
            ]);
        var policy = RuntimePolicyTestFixtures.ResolveEnabledTimerPolicy();

        var profile = P0DecisionProfileValidator.Validate(
            envelope,
            policy,
            P0TextSessionRuntimeCapabilityPolicy.Create().IsDecisionTypeSupportedByP0);

        Assert.Equal(DecisionValidationOutcomes.Accepted, profile.CommunicationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, profile.Outputs[0].ValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Rejected, profile.Outputs[1].ValidationOutcome);
    }

    [Fact]
    public void Mixed_message_and_voice_accepts_the_message_and_rejects_voice_independently()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(),
                SessionRuntimeTestFixtures.VoiceOutput(),
            ],
            requestedActions:
            [
                new RequestedActionRecommendation(
                    AgentRequestedActionKinds.NextTimerRequest,
                    "act.timer.primary",
                    "PT5M",
                    "1"),
            ]);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, result.ValidationEffect.EffectOutcome);
        Assert.Equal(TimerValidationOutcomes.Accepted, result.ValidationEffect.TimerValidationOutcome);
        Assert.True(result.PublicationPathClaimed);
        var message = Assert.Single(
            result.ValidationEffect.OutputValidations,
            item => item.Kind == AgentOutputKinds.Message);
        var voice = Assert.Single(
            result.ValidationEffect.OutputValidations,
            item => item.Kind == AgentOutputKinds.Voice);
        Assert.Equal(DecisionValidationOutcomes.Accepted, message.ValidationOutcome);
        Assert.False(string.IsNullOrWhiteSpace(message.AgentOutputId));
        Assert.StartsWith("aout.", message.AgentOutputId);
        Assert.Equal(DecisionValidationOutcomes.Rejected, voice.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.CapabilityDisabled, voice.RejectionReasonCategory);
        Assert.Null(voice.AgentOutputId);
        Assert.Equal(RuntimeDecisionTypes.EmitMessage, result.Decision!.DecisionType);
    }

    [Fact]
    public void Respond_with_zero_valid_outputs_is_a_decision_rejection_not_no_action()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(invocationId, outputs: []);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Decision);
        Assert.Null(result.ExecutionOutcome);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.PayloadInvalid, result.ValidationEffect.RejectionReasonCategory);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, result.ValidationEffect.EffectOutcome);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
        Assert.False(result.PublicationPathClaimed);
        Assert.DoesNotContain(session.VisibleTranscript, item => item.AuthorType == TranscriptAuthorTypes.Agent);
    }

    [Fact]
    public void Model_authored_output_id_and_reviewer_audience_are_rejected_per_item()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(
                    localRef: "out.message.one",
                    modelAgentOutputId: "aout.model.owned"),
                SessionRuntimeTestFixtures.MessageOutput(
                    localRef: "out.message.two",
                    audience: AgentOutputAudiences.Reviewer),
            ]);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.All(
            result.ValidationEffect.OutputValidations,
            item => Assert.Equal(DecisionValidationOutcomes.Rejected, item.ValidationOutcome));
        Assert.Equal(
            RejectionReasonCategories.PayloadInvalid,
            result.ValidationEffect.OutputValidations[0].RejectionReasonCategory);
        Assert.Equal(
            RejectionReasonCategories.PolicyProhibited,
            result.ValidationEffect.OutputValidations[1].RejectionReasonCategory);
        Assert.All(result.ValidationEffect.OutputValidations, item => Assert.Null(item.AgentOutputId));
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Extra_message_is_rejected_without_voiding_the_first_accepted_message()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(localRef: "out.message.one"),
                SessionRuntimeTestFixtures.MessageOutput(localRef: "out.message.two"),
            ]);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, result.ValidationEffect.EffectOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect.OutputValidations[0].ValidationOutcome);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect.OutputValidations[1].ValidationOutcome);
        Assert.Equal(
            RejectionReasonCategories.PolicyProhibited,
            result.ValidationEffect.OutputValidations[1].RejectionReasonCategory);
        Assert.True(result.PublicationPathClaimed);
    }

    [Fact]
    public void Message_referencing_a_nonexistent_local_ref_is_rejected_and_does_not_publish()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(
                    references: [new OutputLocalReference("continues", "out.missing.primary")]),
            ]);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Decision);
        Assert.Null(result.ExecutionOutcome);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, result.ValidationEffect.EffectOutcome);
        Assert.False(result.PublicationPathClaimed);
        var message = Assert.Single(result.ValidationEffect.OutputValidations);
        Assert.Equal(DecisionValidationOutcomes.Rejected, message.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.PayloadInvalid, message.RejectionReasonCategory);
        Assert.Null(message.AgentOutputId);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
        Assert.DoesNotContain(session.VisibleTranscript, item => item.AuthorType == TranscriptAuthorTypes.Agent);
    }

    [Fact]
    public void Message_referencing_a_p0_rejected_voice_sibling_is_rejected_and_does_not_publish()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(
                    references: [new OutputLocalReference("continues", "out.voice.primary")]),
                SessionRuntimeTestFixtures.VoiceOutput(),
            ]);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Decision);
        Assert.Null(result.ExecutionOutcome);
        Assert.Equal(DecisionValidationOutcomes.Rejected, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, result.ValidationEffect.EffectOutcome);
        Assert.False(result.PublicationPathClaimed);
        var message = Assert.Single(
            result.ValidationEffect.OutputValidations,
            item => item.Kind == AgentOutputKinds.Message);
        var voice = Assert.Single(
            result.ValidationEffect.OutputValidations,
            item => item.Kind == AgentOutputKinds.Voice);
        Assert.Equal(DecisionValidationOutcomes.Rejected, message.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.PolicyProhibited, message.RejectionReasonCategory);
        Assert.Null(message.AgentOutputId);
        Assert.Equal(DecisionValidationOutcomes.Rejected, voice.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.CapabilityDisabled, voice.RejectionReasonCategory);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
        Assert.DoesNotContain(session.VisibleTranscript, item => item.AuthorType == TranscriptAuthorTypes.Agent);
    }

    [Fact]
    public void Parser_rejects_hidden_reasoning_as_malformed_control()
    {
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"respond","outputs":[],"requested_actions":[],"reasoning":"hidden"}
            """;
        var parsed = AgentDecisionEnvelopeParser.Parse(Encoding.UTF8.GetBytes(json));
        Assert.False(parsed.Succeeded);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, parsed.FailureReasonCategory);
    }

    [Fact]
    public void Parser_treats_partial_json_as_incomplete_and_unknown_kind_as_malformed()
    {
        var incomplete = AgentDecisionEnvelopeParser.Parse(
            """{"schema_version":"v2","agent_decision_id":"adec.00000001"}"""u8);
        Assert.False(incomplete.Succeeded);
        Assert.Equal(ExecutionFailureReasons.IncompleteControl, incomplete.FailureReasonCategory);

        var malformed = AgentDecisionEnvelopeParser.Parse(
            """{"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"respond","outputs":[{"kind":"evaluation","local_ref":"out.eval.primary"}],"requested_actions":[]}"""u8);
        Assert.False(malformed.Succeeded);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, malformed.FailureReasonCategory);
    }

    [Fact]
    public void Parser_accepts_typed_voice_as_schema_valid_envelope()
    {
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"respond","outputs":[{"kind":"message","local_ref":"out.message.primary","communication_purpose":"reply"},{"kind":"voice","local_ref":"out.voice.primary"}],"requested_actions":[]}
            """;

        var parsed = AgentDecisionEnvelopeParser.Parse(Encoding.UTF8.GetBytes(json));

        Assert.True(parsed.Succeeded, parsed.OutcomeCode);
        Assert.Equal(2, parsed.Envelope!.Outputs.Count);
        Assert.Equal(AgentOutputKinds.Voice, parsed.Envelope.Outputs[1].Kind);
        Assert.Null(parsed.Envelope.Outputs[1].CommunicationPurpose);
    }

    [Fact]
    public void No_action_with_a_valid_message_rejects_the_message_and_does_not_publish()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope = SessionRuntimeTestFixtures.Envelope(
            invocationId,
            DecisionDispositions.NoAction,
            [SessionRuntimeTestFixtures.MessageOutput()],
            noActionReasonCategory: NoActionReasonCategories.IntentionalSilence);

        var result = session.CompleteInvocation(
            invocationId, envelope, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Decision);
        Assert.Null(result.ExecutionOutcome);
        Assert.Equal(DecisionValidationOutcomes.Accepted, result.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, result.ValidationEffect.EffectOutcome);
        Assert.False(result.PublicationPathClaimed);
        var message = Assert.Single(result.ValidationEffect.OutputValidations);
        Assert.Equal(DecisionValidationOutcomes.Rejected, message.ValidationOutcome);
        Assert.Equal(RejectionReasonCategories.PolicyProhibited, message.RejectionReasonCategory);
        Assert.Null(message.AgentOutputId);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, session.Turns[0].ResponseSlot.State);
        Assert.DoesNotContain(session.VisibleTranscript, item => item.AuthorType == TranscriptAuthorTypes.Agent);
    }

    [Fact]
    public void Parser_retains_envelope_payload_ref_and_output_references()
    {
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"respond","outputs":[{"kind":"message","local_ref":"out.message.primary","communication_purpose":"reply","references":[{"relation":"continues","local_ref":"out.prior.primary"}]},{"kind":"voice","local_ref":"out.voice.primary","payload_ref":{"protected_ref":"pref.voice.1","content_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}}],"requested_actions":[],"payload_ref":{"protected_ref":"pref.envelope.1","content_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}}
            """;

        var parsed = AgentDecisionEnvelopeParser.Parse(Encoding.UTF8.GetBytes(json));

        Assert.True(parsed.Succeeded, parsed.OutcomeCode);
        Assert.Equal("pref.envelope.1", parsed.Envelope!.PayloadRef!.ProtectedRef);
        var message = Assert.Single(parsed.Envelope.Outputs, item => item.Kind == AgentOutputKinds.Message);
        var reference = Assert.Single(message.References!);
        Assert.Equal("continues", reference.Relation);
        Assert.Equal("out.prior.primary", reference.LocalRef);
        var voice = Assert.Single(parsed.Envelope.Outputs, item => item.Kind == AgentOutputKinds.Voice);
        Assert.Equal("pref.voice.1", voice.PayloadRef!.ProtectedRef);
    }

    [Fact]
    public void Recommendation_digest_changes_when_any_retained_envelope_field_changes()
    {
        var payload = new ProtectedContentRef(
            "pref.envelope.1",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var voicePayload = new ProtectedContentRef(
            "pref.voice.1",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var baseline = SessionRuntimeTestFixtures.Envelope(
            "ainv.00000001",
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(
                    references: [new OutputLocalReference("continues", "out.prior.primary")]),
                SessionRuntimeTestFixtures.VoiceOutput() with { PayloadRef = voicePayload },
            ],
            requestedActions:
            [
                new RequestedActionRecommendation(
                    AgentRequestedActionKinds.NextTimerRequest,
                    "act.timer.primary",
                    "PT5M",
                    "1"),
            ],
            decisionId: "adec.00000001",
            payloadRef: payload);
        var baselineDigest = DecisionRecommendationDigestComputer.Compute(baseline);

        Assert.NotEqual(
            baselineDigest,
            DecisionRecommendationDigestComputer.Compute(
                baseline with
                {
                    PayloadRef = payload with { ProtectedRef = "pref.envelope.2" },
                }));
        Assert.NotEqual(
            baselineDigest,
            DecisionRecommendationDigestComputer.Compute(
                baseline with
                {
                    Outputs =
                    [
                        baseline.Outputs[0] with
                        {
                            References = [new OutputLocalReference("supersedes", "out.prior.primary")],
                        },
                        baseline.Outputs[1],
                    ],
                }));
        Assert.NotEqual(
            baselineDigest,
            DecisionRecommendationDigestComputer.Compute(
                baseline with
                {
                    Outputs =
                    [
                        baseline.Outputs[0],
                        baseline.Outputs[1] with
                        {
                            PayloadRef = voicePayload with { ProtectedRef = "pref.voice.2" },
                        },
                    ],
                }));
        Assert.NotEqual(
            baselineDigest,
            DecisionRecommendationDigestComputer.Compute(
                baseline with
                {
                    Outputs =
                    [
                        baseline.Outputs[0] with { CommunicationPurpose = "closing_summary" },
                        baseline.Outputs[1],
                    ],
                }));
        Assert.NotEqual(
            baselineDigest,
            DecisionRecommendationDigestComputer.Compute(
                baseline with
                {
                    RequestedActions =
                    [
                        baseline.RequestedActions[0] with { RelativeDelay = "PT6M" },
                    ],
                }));
    }

    [Fact]
    public void Parser_rejects_voice_speech_semantics_as_malformed_control()
    {
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"respond","outputs":[{"kind":"voice","local_ref":"out.voice.primary","communication_purpose":"spoken_summary"}],"requested_actions":[]}
            """;
        var parsed = AgentDecisionEnvelopeParser.Parse(Encoding.UTF8.GetBytes(json));
        Assert.False(parsed.Succeeded);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, parsed.FailureReasonCategory);
    }
}
