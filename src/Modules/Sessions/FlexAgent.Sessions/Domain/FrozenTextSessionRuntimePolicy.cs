namespace FlexAgent.Sessions.Domain;

/// <summary>
/// Immutable frozen runtime policy snapshot bound to a Session. Operational admission,
/// timer scheduling, and effect enforcement consume this snapshot rather than mutable
/// configuration.
/// </summary>
public sealed class FrozenTextSessionRuntimePolicy
{
    internal FrozenTextSessionRuntimePolicy(
        string invocationContractVersion,
        string decisionContractVersion,
        IReadOnlyList<RuntimeTriggerDescriptor> permittedNonTimerTriggers,
        IReadOnlyList<string> permittedDecisionTypes,
        bool agentInitiatedOpeningPermitted,
        bool agentInitiatedClosingPermitted,
        bool noActionPermitted,
        InvocationBounds invocationBounds,
        TimerLanePolicy? timerLane,
        IReadOnlyList<string> explicitlyDisabledCapabilities,
        string policyDigest)
    {
        InvocationContractVersion = invocationContractVersion;
        DecisionContractVersion = decisionContractVersion;
        PermittedNonTimerTriggers = permittedNonTimerTriggers;
        PermittedDecisionTypes = permittedDecisionTypes;
        AgentInitiatedOpeningPermitted = agentInitiatedOpeningPermitted;
        AgentInitiatedClosingPermitted = agentInitiatedClosingPermitted;
        NoActionPermitted = noActionPermitted;
        InvocationBounds = invocationBounds;
        TimerLane = timerLane;
        ExplicitlyDisabledCapabilities = explicitlyDisabledCapabilities;
        PolicyDigest = policyDigest;
        IsTimerTriggerPermitted = timerLane is { IsEnabled: true };
    }

    public string InvocationContractVersion { get; }

    public string DecisionContractVersion { get; }

    public IReadOnlyList<RuntimeTriggerDescriptor> PermittedNonTimerTriggers { get; }

    public IReadOnlyList<string> PermittedDecisionTypes { get; }

    public bool AgentInitiatedOpeningPermitted { get; }

    public bool AgentInitiatedClosingPermitted { get; }

    public bool NoActionPermitted { get; }

    public InvocationBounds InvocationBounds { get; }

    public TimerLanePolicy? TimerLane { get; }

    public bool IsTimerTriggerPermitted { get; }

    public IReadOnlyList<string> ExplicitlyDisabledCapabilities { get; }

    public string PolicyDigest { get; }
}
