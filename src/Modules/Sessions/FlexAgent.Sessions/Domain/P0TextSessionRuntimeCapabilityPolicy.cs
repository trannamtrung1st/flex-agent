namespace FlexAgent.Sessions.Domain;

public enum TimerLaneAvailability
{
    Disabled,
    Enabled,
}

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

public sealed class P0TextSessionRuntimeCapabilityPolicy
{
    private static readonly HashSet<string> EnabledCapabilities =
    [
        RuntimeCapabilityIdentifiers.TextInteraction,
    ];

    private static readonly HashSet<string> PermittedDecisionTypes =
    [
        RuntimeDecisionTypes.EmitMessage,
        RuntimeDecisionTypes.NoAction,
    ];

    private static readonly HashSet<(string Family, string Type)> PermittedTriggersWithoutTimer =
    [
        (RuntimeTriggerIdentifiers.ParticipantInputFamily, RuntimeTriggerIdentifiers.ParticipantMessageType),
        (RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentOpeningType),
        (RuntimeTriggerIdentifiers.WorkflowEventFamily, RuntimeTriggerIdentifiers.AgentClosingType),
    ];

    private static readonly HashSet<(string Family, string Type)> TimerTriggers =
    [
        (RuntimeTriggerIdentifiers.TimerEventFamily, RuntimeTriggerIdentifiers.TimerLaneDefaultType),
    ];

    private P0TextSessionRuntimeCapabilityPolicy(TimerLaneAvailability timerLaneAvailability)
    {
        TimerLaneAvailability = timerLaneAvailability;
    }

    public TimerLaneAvailability TimerLaneAvailability { get; }

    public bool IsTimerLaneOptional => true;

    public static P0TextSessionRuntimeCapabilityPolicy Create(
        TimerLaneAvailability timerLaneAvailability = TimerLaneAvailability.Disabled) =>
        new(timerLaneAvailability);

    public bool IsTriggerPermitted(string triggerFamily, string triggerType)
    {
        if (string.IsNullOrWhiteSpace(triggerFamily) || string.IsNullOrWhiteSpace(triggerType))
        {
            return false;
        }

        var trigger = (triggerFamily, triggerType);
        if (PermittedTriggersWithoutTimer.Contains(trigger))
        {
            return true;
        }

        if (TimerLaneAvailability == TimerLaneAvailability.Enabled && TimerTriggers.Contains(trigger))
        {
            return true;
        }

        return false;
    }

    public bool IsDecisionTypePermitted(string decisionType) =>
        !string.IsNullOrWhiteSpace(decisionType) && PermittedDecisionTypes.Contains(decisionType);

    public bool IsCapabilityEnabled(string capabilityIdentifier) =>
        !string.IsNullOrWhiteSpace(capabilityIdentifier)
        && EnabledCapabilities.Contains(capabilityIdentifier);
}
