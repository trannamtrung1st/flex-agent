using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class P0TextSessionRuntimeCapabilityPolicyTests
{
    [Fact]
    public void P0_kernel_supports_optional_single_timer_lane_without_selecting_durations()
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create(
            timerLaneAvailability: TimerLaneAvailability.Disabled);

        Assert.True(policy.IsTimerLaneOptional);
        Assert.Equal(TimerLaneAvailability.Disabled, policy.TimerLaneAvailability);
        Assert.False(policy.IsTriggerPermitted(
            RuntimeTriggerIdentifiers.TimerEventFamily,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType));
    }

    [Fact]
    public void P0_kernel_enables_text_interaction_capability()
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.IsCapabilityEnabled(RuntimeCapabilityIdentifiers.TextInteraction));
    }

    [Theory]
    [InlineData(RuntimeTriggerIdentifiers.ParticipantInputFamily, RuntimeTriggerIdentifiers.ParticipantMessageType)]
    [InlineData(RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentOpeningType)]
    [InlineData(RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentClosingType)]
    public void P0_kernel_permits_approved_text_session_triggers(
        string triggerFamily,
        string triggerType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.IsTriggerPermitted(triggerFamily, triggerType));
    }

    [Fact]
    public void P0_kernel_permits_timer_trigger_only_when_lane_is_enabled()
    {
        var disabled = P0TextSessionRuntimeCapabilityPolicy.Create(
            timerLaneAvailability: TimerLaneAvailability.Disabled);
        var enabled = P0TextSessionRuntimeCapabilityPolicy.Create(
            timerLaneAvailability: TimerLaneAvailability.Enabled);

        Assert.False(disabled.IsTriggerPermitted(
            RuntimeTriggerIdentifiers.TimerEventFamily,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType));
        Assert.True(enabled.IsTriggerPermitted(
            RuntimeTriggerIdentifiers.TimerEventFamily,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType));
    }

    [Theory]
    [InlineData("interaction_signal", "interaction_signal.voice_end")]
    [InlineData("interaction_signal", "interaction_signal.silence_detected")]
    [InlineData("tool_result", "tool_result.participant_tool")]
    [InlineData("workflow_event", "workflow_event.custom_stage_transition")]
    [InlineData("timer_event", "timer_event.parallel_lane")]
    [InlineData("system_event", "system_event.evaluation_ready")]
    [InlineData("participant_input", "participant_input.voice_utterance")]
    public void P0_kernel_denies_deferred_or_prohibited_triggers(
        string triggerFamily,
        string triggerType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create(
            timerLaneAvailability: TimerLaneAvailability.Enabled);

        Assert.False(policy.IsTriggerPermitted(triggerFamily, triggerType));
    }

    [Theory]
    [InlineData(RuntimeDecisionTypes.EmitMessage)]
    [InlineData(RuntimeDecisionTypes.NoAction)]
    public void P0_kernel_permits_approved_decision_types(string decisionType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.IsDecisionTypePermitted(decisionType));
    }

    [Theory]
    [InlineData(RuntimeDecisionTypes.RequestTool)]
    [InlineData(RuntimeDecisionTypes.ProposeTransition)]
    [InlineData(RuntimeDecisionTypes.Escalate)]
    public void P0_kernel_denies_deferred_decision_types(string decisionType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsDecisionTypePermitted(decisionType));
    }

    [Theory]
    [InlineData(RuntimeCapabilityIdentifiers.VoiceInteraction)]
    [InlineData(RuntimeCapabilityIdentifiers.InteractionController)]
    [InlineData(RuntimeCapabilityIdentifiers.SilenceDrivenTrigger)]
    [InlineData(RuntimeCapabilityIdentifiers.ParticipantSessionTools)]
    [InlineData(RuntimeCapabilityIdentifiers.ToolResultTrigger)]
    [InlineData(RuntimeCapabilityIdentifiers.ToolExecution)]
    [InlineData(RuntimeCapabilityIdentifiers.ParallelTimerLane)]
    [InlineData(RuntimeCapabilityIdentifiers.ArbitraryTimerLane)]
    [InlineData(RuntimeCapabilityIdentifiers.ConfigurableWorkflowTriggers)]
    [InlineData(RuntimeCapabilityIdentifiers.DynamicMemoryWrite)]
    [InlineData(RuntimeCapabilityIdentifiers.DynamicMemoryLearning)]
    [InlineData(RuntimeCapabilityIdentifiers.SharedSession)]
    [InlineData(RuntimeCapabilityIdentifiers.ModelAuthorizedEvaluation)]
    [InlineData(RuntimeCapabilityIdentifiers.ModelAuthorizedResultRelease)]
    public void P0_kernel_explicitly_denies_deferred_capabilities(string capabilityIdentifier)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsCapabilityEnabled(capabilityIdentifier));
    }

    [Theory]
    [InlineData("future.capability.example")]
    [InlineData("voice")]
    [InlineData("")]
    public void P0_kernel_fails_closed_for_unknown_decision_types(string decisionType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsDecisionTypePermitted(decisionType));
    }

    [Theory]
    [InlineData("future.capability.example")]
    [InlineData("voice")]
    [InlineData("")]
    public void P0_kernel_fails_closed_for_unknown_capability_identifiers(string capabilityIdentifier)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsCapabilityEnabled(capabilityIdentifier));
    }

    [Theory]
    [InlineData("unknown_family", "unknown.type")]
    [InlineData("participant_input", "participant_input.")]
    public void P0_kernel_fails_closed_for_unknown_trigger_identifiers(
        string triggerFamily,
        string triggerType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create(
            timerLaneAvailability: TimerLaneAvailability.Enabled);

        Assert.False(policy.IsTriggerPermitted(triggerFamily, triggerType));
    }
}
