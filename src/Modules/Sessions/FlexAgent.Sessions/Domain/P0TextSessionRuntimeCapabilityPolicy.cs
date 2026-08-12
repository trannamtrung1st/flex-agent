namespace FlexAgent.Sessions.Domain;

public static class RuntimeCapabilityIdentifiers
{
    public const string TextInteraction = "text_interaction";
    public const string VoiceInteraction = "voice_interaction";
    public const string InteractionController = "interaction_controller";
    public const string SilenceDrivenTrigger = "silence_driven_trigger";
    public const string ParticipantSessionTools = "participant_session_tools";
    public const string ToolResultTrigger = "tool_result_trigger";
    public const string ToolExecution = "tool_execution";
    public const string ParallelTimerLane = "parallel_timer_lane";
    public const string ArbitraryTimerLane = "arbitrary_timer_lane";
    public const string ConfigurableWorkflowTriggers = "configurable_workflow_triggers";
    public const string DynamicMemoryWrite = "dynamic_memory_write";
    public const string DynamicMemoryLearning = "dynamic_memory_learning";
    public const string SharedSession = "shared_session";
    public const string ModelAuthorizedEvaluation = "model_authorized_evaluation";
    public const string ModelAuthorizedResultRelease = "model_authorized_result_release";
}

public static class RuntimeTriggerIdentifiers
{
    public const string ParticipantInputFamily = "participant_input";
    public const string ParticipantMessageType = "participant_input.message";

    public const string WorkflowEventFamily = "workflow_event";
    public const string AgentOpeningType = "workflow_event.agent_opening";
    public const string AgentClosingType = "workflow_event.agent_closing";

    public const string TimerEventFamily = "timer_event";
    public const string TimerLaneDefaultType = "timer_event.lane_default";
}

public static class RuntimeDecisionTypes
{
    public const string EmitMessage = "emit_message";
    public const string NoAction = "no_action";
    public const string RequestTool = "request_tool";
    public const string ProposeTransition = "propose_transition";
    public const string Escalate = "escalate";
}

/// <summary>
/// Immutable P0 capability ceiling. This type expresses which trigger, decision, and
/// capability identifiers may appear in a later frozen runtime policy; it does not
/// model operational Session admission, timer enablement, or numeric bounds.
/// </summary>
public sealed class P0TextSessionRuntimeCapabilityPolicy
{
    private static readonly HashSet<string> SupportedCapabilities =
    [
        RuntimeCapabilityIdentifiers.TextInteraction,
    ];

    private static readonly HashSet<string> SupportedDecisionTypes =
    [
        RuntimeDecisionTypes.EmitMessage,
        RuntimeDecisionTypes.NoAction,
    ];

    private static readonly HashSet<(string Family, string Type)> SupportedNonTimerTriggers =
    [
        (RuntimeTriggerIdentifiers.ParticipantInputFamily, RuntimeTriggerIdentifiers.ParticipantMessageType),
        (RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentOpeningType),
        (RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentClosingType),
    ];

    private P0TextSessionRuntimeCapabilityPolicy()
    {
    }

    /// <summary>
    /// P0 text Session profiles may include one optional system timer lane once fully
    /// resolved under REQ-RSC-51 through REQ-RSC-53. This kernel does not represent an
    /// operationally enabled lane or any timer timing values.
    /// </summary>
    public bool SupportsOptionalTimerLane => true;

    public static P0TextSessionRuntimeCapabilityPolicy Create() => new();

    public bool IsTriggerSupportedByP0(string triggerFamily, string triggerType)
    {
        if (string.IsNullOrWhiteSpace(triggerFamily) || string.IsNullOrWhiteSpace(triggerType))
        {
            return false;
        }

        return SupportedNonTimerTriggers.Contains((triggerFamily, triggerType));
    }

    public bool IsTimerTriggerSupportedByP0(string triggerFamily, string triggerType) =>
        SupportsOptionalTimerLane
        && triggerFamily == RuntimeTriggerIdentifiers.TimerEventFamily
        && triggerType == RuntimeTriggerIdentifiers.TimerLaneDefaultType;

    public bool IsDecisionTypeSupportedByP0(string decisionType) =>
        !string.IsNullOrWhiteSpace(decisionType) && SupportedDecisionTypes.Contains(decisionType);

    public bool IsCapabilitySupportedByP0(string capabilityIdentifier) =>
        !string.IsNullOrWhiteSpace(capabilityIdentifier)
        && SupportedCapabilities.Contains(capabilityIdentifier);
}
