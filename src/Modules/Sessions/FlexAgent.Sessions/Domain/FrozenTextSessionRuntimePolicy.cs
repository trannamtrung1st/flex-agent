using System.Collections.Immutable;

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
        string decisionValidationPolicyVersion,
        IReadOnlyList<DecisionTypeSchemaBinding> decisionSchemaBindings,
        IReadOnlyList<RuntimeTriggerDescriptor> permittedNonTimerTriggers,
        IReadOnlyList<string> permittedDecisionTypes,
        bool agentInitiatedOpeningPermitted,
        bool agentInitiatedClosingPermitted,
        bool noActionPermitted,
        InvocationBounds invocationBounds,
        StreamingPublicationBounds streamingPublicationBounds,
        TimerLanePolicy? timerLane,
        IReadOnlyList<string> explicitlyDisabledCapabilities,
        string policyDigest)
    {
        InvocationContractVersion = invocationContractVersion;
        DecisionContractVersion = decisionContractVersion;
        DecisionValidationPolicyVersion = decisionValidationPolicyVersion;
        DecisionSchemaBindings = RuntimePolicySnapshots.CopySchemaBindings(decisionSchemaBindings);
        PermittedNonTimerTriggers = RuntimePolicySnapshots.CopyTriggers(permittedNonTimerTriggers);
        PermittedDecisionTypes = RuntimePolicySnapshots.CopyStrings(permittedDecisionTypes);
        AgentInitiatedOpeningPermitted = agentInitiatedOpeningPermitted;
        AgentInitiatedClosingPermitted = agentInitiatedClosingPermitted;
        NoActionPermitted = noActionPermitted;
        InvocationBounds = invocationBounds;
        StreamingPublicationBounds = streamingPublicationBounds;
        TimerLane = timerLane;
        ExplicitlyDisabledCapabilities = RuntimePolicySnapshots.CopyStrings(explicitlyDisabledCapabilities);
        PolicyDigest = policyDigest;
        IsTimerTriggerPermitted = timerLane is { IsEnabled: true };
    }

    public string InvocationContractVersion { get; }

    public string DecisionContractVersion { get; }

    public string DecisionValidationPolicyVersion { get; }

    public IReadOnlyList<DecisionTypeSchemaBinding> DecisionSchemaBindings { get; }

    public IReadOnlyList<RuntimeTriggerDescriptor> PermittedNonTimerTriggers { get; }

    public IReadOnlyList<string> PermittedDecisionTypes { get; }

    public bool AgentInitiatedOpeningPermitted { get; }

    public bool AgentInitiatedClosingPermitted { get; }

    public bool NoActionPermitted { get; }

    public InvocationBounds InvocationBounds { get; }

    public StreamingPublicationBounds StreamingPublicationBounds { get; }

    public TimerLanePolicy? TimerLane { get; }

    public bool IsTimerTriggerPermitted { get; }

    public IReadOnlyList<string> ExplicitlyDisabledCapabilities { get; }

    public string PolicyDigest { get; }
}
