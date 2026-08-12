using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class P0TextSessionRuntimeCapabilityPolicyTests
{
    [Fact]
    public void P0_kernel_exposes_required_explicitly_disabled_capabilities_for_mvp_profile()
    {
        var required = P0TextSessionRuntimeCapabilityPolicy.RequiredExplicitlyDisabledCapabilities;

        Assert.Contains(RuntimeCapabilityIdentifiers.VoiceInteraction, required);
        Assert.Contains(RuntimeCapabilityIdentifiers.SharedSession, required);
        Assert.DoesNotContain(RuntimeCapabilityIdentifiers.TextInteraction, required);
        Assert.True(P0TextSessionRuntimeCapabilityPolicy.Create().ContainsRequiredExplicitlyDisabledCapabilities(required));
    }

    [Fact]
    public void P0_kernel_supports_optional_single_timer_lane_without_modeling_operational_enablement()
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.SupportsOptionalTimerLane);
        Assert.True(policy.IsTimerTriggerSupportedByP0(
            RuntimeTriggerIdentifiers.TimerEventFamily,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType));
        Assert.False(policy.IsTriggerSupportedByP0(
            RuntimeTriggerIdentifiers.TimerEventFamily,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType));
    }

    [Fact]
    public void P0_kernel_supports_text_interaction_capability()
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.IsCapabilitySupportedByP0(RuntimeCapabilityIdentifiers.TextInteraction));
    }

    [Theory]
    [InlineData(RuntimeTriggerIdentifiers.ParticipantInputFamily, RuntimeTriggerIdentifiers.ParticipantMessageType)]
    [InlineData(RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentOpeningType)]
    [InlineData(RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentClosingType)]
    public void P0_kernel_supports_approved_non_timer_triggers(string triggerFamily, string triggerType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.IsTriggerSupportedByP0(triggerFamily, triggerType));
    }

    [Theory]
    [InlineData("interaction_signal", "interaction_signal.voice_end")]
    [InlineData("interaction_signal", "interaction_signal.silence_detected")]
    [InlineData("tool_result", "tool_result.participant_tool")]
    [InlineData("workflow_event", "workflow_event.custom_stage_transition")]
    [InlineData("timer_event", "timer_event.parallel_lane")]
    [InlineData("system_event", "system_event.evaluation_ready")]
    [InlineData("participant_input", "participant_input.voice_utterance")]
    public void P0_kernel_does_not_support_deferred_or_prohibited_triggers(
        string triggerFamily,
        string triggerType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsTriggerSupportedByP0(triggerFamily, triggerType));
        Assert.False(policy.IsTimerTriggerSupportedByP0(triggerFamily, triggerType));
    }

    [Theory]
    [InlineData(RuntimeDecisionTypes.EmitMessage)]
    [InlineData(RuntimeDecisionTypes.NoAction)]
    public void P0_kernel_supports_approved_decision_types(string decisionType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.True(policy.IsDecisionTypeSupportedByP0(decisionType));
    }

    [Theory]
    [InlineData(RuntimeDecisionTypes.RequestTool)]
    [InlineData(RuntimeDecisionTypes.ProposeTransition)]
    [InlineData(RuntimeDecisionTypes.Escalate)]
    public void P0_kernel_does_not_support_deferred_decision_types(string decisionType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsDecisionTypeSupportedByP0(decisionType));
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
    public void P0_kernel_does_not_support_deferred_capabilities(string capabilityIdentifier)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsCapabilitySupportedByP0(capabilityIdentifier));
    }

    [Theory]
    [InlineData("future.capability.example")]
    [InlineData("voice")]
    [InlineData("")]
    public void P0_kernel_fails_closed_for_unknown_decision_types(string decisionType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsDecisionTypeSupportedByP0(decisionType));
    }

    [Theory]
    [InlineData("future.capability.example")]
    [InlineData("voice")]
    [InlineData("")]
    public void P0_kernel_fails_closed_for_unknown_capability_identifiers(string capabilityIdentifier)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsCapabilitySupportedByP0(capabilityIdentifier));
    }

    [Theory]
    [InlineData("unknown_family", "unknown.type")]
    [InlineData("participant_input", "participant_input.")]
    public void P0_kernel_fails_closed_for_unknown_trigger_identifiers(
        string triggerFamily,
        string triggerType)
    {
        var policy = P0TextSessionRuntimeCapabilityPolicy.Create();

        Assert.False(policy.IsTriggerSupportedByP0(triggerFamily, triggerType));
        Assert.False(policy.IsTimerTriggerSupportedByP0(triggerFamily, triggerType));
    }
}
