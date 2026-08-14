using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class SessionRuntimeRehydrateTests
{
    [Fact]
    public void Rehydrate_restores_version_sequence_and_admitted_invocation()
    {
        var binding = SessionRuntimeTestFixtures.CreateBinding();
        var trigger = SessionRuntimeTestFixtures.OpeningTrigger();
        var invocation = AgentInvocation.Rehydrate(
            "inv-1",
            binding.Ownership,
            trigger,
            "idem-1",
            binding.Policy.PolicyDigest,
            sessionSequence: 4,
            AgentInvocationStatuses.Admitted);
        var committedAt = SessionRuntimeTestFixtures.T0.AddMinutes(3);

        var session = SessionRuntime.Rehydrate(
            binding,
            SessionLifecycleState.Active,
            sessionVersion: 7,
            sessionSequence: 4,
            cutoffSequence: null,
            committedAt,
            [invocation],
            lastAdmittedAtByFamily: new Dictionary<string, DateTimeOffset>
            {
                [trigger.TriggerFamily] = committedAt,
            });

        Assert.Equal(7, session.SessionVersion);
        Assert.Equal(4, session.SessionSequence);
        Assert.Equal(committedAt, session.LastCommittedAt);
        var loaded = Assert.Single(session.Invocations);
        Assert.Equal("inv-1", loaded.AgentInvocationId);
        Assert.Equal(4, loaded.SessionSequence);
        Assert.Equal(AgentInvocationStatuses.Admitted, loaded.Status);
    }

    [Fact]
    public void Participant_turns_capture_created_session_sequence_in_admission_order()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = new InvocationBounds(3, 10, 0, CooldownSeconds: 0, 30),
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));
        session.AcceptParticipantMessage(
            "msg.z", "turn.z", "slot.z", "trig.z", "idem.z", SessionRuntimeTestFixtures.T0);
        session.AcceptParticipantMessage(
            "msg.a", "turn.a", "slot.a", "trig.a", "idem.a", SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.Equal(["turn.z", "turn.a"], session.Turns.Select(turn => turn.TurnId));
        Assert.Equal(1, session.Turns[0].CreatedSessionSequence);
        Assert.Equal(2, session.Turns[1].CreatedSessionSequence);
    }

    [Fact]
    public void Opening_emit_then_participant_reply_assigns_distinct_created_session_sequences()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = new InvocationBounds(3, 10, 0, CooldownSeconds: 0, 30),
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(), "idem.open", SessionRuntimeTestFixtures.T0);
        var openingId = opening.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            openingId,
            SessionRuntimeTestFixtures.EmitMessage(
                openingId,
                communicationPurpose: "agent_opening",
                turnId: null,
                responseSlotId: null),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        var admitted = session.AcceptParticipantMessage(
            "msg.a", "turn.a", "slot.a", "trig.a", "idem.a", SessionRuntimeTestFixtures.T0.AddSeconds(3));
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        Assert.Equal(2, session.Turns.Count);
        Assert.Equal(TurnKinds.AgentOpening, session.Turns[0].Kind);
        Assert.Equal("turn.a", session.Turns[1].TurnId);
        Assert.Equal(2, session.Turns[0].CreatedSessionSequence);
        Assert.Equal(3, session.Turns[1].CreatedSessionSequence);
        Assert.Equal(3, session.Invocations[1].SessionSequence);
    }

    [Fact]
    public void Rehydrate_restores_agent_message_fragments_and_decision_linkage()
    {
        var binding = SessionRuntimeTestFixtures.CreateBinding();
        var fragments = new[]
        {
            new AgentResponseFragment(1, 4, "Hel", ProtectedContentRef.DigestUtf8("Hel")),
            new AgentResponseFragment(2, 5, "lo", ProtectedContentRef.DigestUtf8("lo")),
        };
        var message = AgentResponseMessage.Rehydrate(
            "aout.rehydrate.0001",
            "agen.1",
            "ainv.1",
            "adec.1",
            "turn.1",
            "slot.1",
            AgentMessageCompletionStates.Complete,
            ProtectedContentRef.DigestUtf8("Hello"),
            fragments);

        var session = SessionRuntime.Rehydrate(
            binding,
            SessionLifecycleState.Active,
            sessionVersion: 3,
            sessionSequence: 5,
            cutoffSequence: null,
            SessionRuntimeTestFixtures.T0,
            agentMessages: [message]);

        var loaded = Assert.Single(session.AgentMessages);
        Assert.Equal("aout.rehydrate.0001", loaded.MessageId);
        Assert.Equal("adec.1", loaded.DrivingDecisionId);
        Assert.Equal("ainv.1", loaded.DrivingInvocationId);
        Assert.Equal(AgentMessageCompletionStates.Complete, loaded.CompletionState);
        Assert.Equal("Hello", loaded.AssembleExactText());
        Assert.Equal(2, loaded.Fragments.Count);
        Assert.Equal(4, loaded.Fragments[0].SessionSequence);
    }
}
