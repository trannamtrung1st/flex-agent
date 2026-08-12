using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

internal static class RuntimePolicyTestFixtures
{
    internal const string BaselineId = "baseline.p0.text.0001";
    internal const string BaselineDigest = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    internal static RuntimePolicyBaselineSource CreateEnabledTimerBaseline() =>
        new(
            BaselineId,
            BaselineDigest,
            new RuntimePolicyEffectiveValues
            {
                InvocationContractVersion = RuntimeContractVersions.InvocationV1,
                DecisionContractVersion = RuntimeContractVersions.DecisionV1,
                PermittedNonTimerTriggers =
                [
                    new RuntimeTriggerDescriptor(
                        RuntimeTriggerIdentifiers.ParticipantInputFamily,
                        RuntimeTriggerIdentifiers.ParticipantMessageType),
                    new RuntimeTriggerDescriptor(
                        RuntimeTriggerIdentifiers.WorkflowEventFamily,
                        RuntimeTriggerIdentifiers.AgentOpeningType),
                    new RuntimeTriggerDescriptor(
                        RuntimeTriggerIdentifiers.WorkflowEventFamily,
                        RuntimeTriggerIdentifiers.AgentClosingType),
                ],
                PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
                AgentInitiatedOpeningPermitted = true,
                AgentInitiatedClosingPermitted = true,
                NoActionPermitted = true,
                InvocationBounds = new InvocationBounds(
                    MaxAttemptsPerInvocation: 3,
                    MaxChainedInvocationsPerSession: 10,
                    MaxToolIterations: 0,
                    CooldownSeconds: 5,
                    DuplicateSuppressionWindowSeconds: 30),
                TimerLane = new TimerLanePolicyValues
                {
                    Enabled = true,
                    DefaultDelay = "PT5M",
                    MinRequestedDelay = "PT1M",
                    MaxRequestedDelay = "PT30M",
                    ClockBasis = TimerLaneClockBasis.ActiveSessionTime,
                    PermittedStages = ["active"],
                    PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
                    Budgets = new TimerLaneBudgets(
                        MaxAcceptedReplacementsPerSession: 5,
                        MaxTimerTriggeredInvocationsPerSession: 8,
                        CooldownSeconds: 10,
                        MaxConcurrentReplacements: 1,
                        DuplicateSuppressionWindowSeconds: 30),
                },
                ExplicitlyDisabledCapabilities =
                [
                    RuntimeCapabilityIdentifiers.VoiceInteraction,
                    RuntimeCapabilityIdentifiers.InteractionController,
                    RuntimeCapabilityIdentifiers.SilenceDrivenTrigger,
                    RuntimeCapabilityIdentifiers.ParticipantSessionTools,
                    RuntimeCapabilityIdentifiers.ToolResultTrigger,
                    RuntimeCapabilityIdentifiers.ToolExecution,
                    RuntimeCapabilityIdentifiers.ParallelTimerLane,
                    RuntimeCapabilityIdentifiers.ArbitraryTimerLane,
                    RuntimeCapabilityIdentifiers.ConfigurableWorkflowTriggers,
                    RuntimeCapabilityIdentifiers.DynamicMemoryWrite,
                    RuntimeCapabilityIdentifiers.DynamicMemoryLearning,
                    RuntimeCapabilityIdentifiers.SharedSession,
                    RuntimeCapabilityIdentifiers.ModelAuthorizedEvaluation,
                    RuntimeCapabilityIdentifiers.ModelAuthorizedResultRelease,
                ],
            });

    internal static RuntimePolicyBaselineSource CreateDisabledTimerBaseline()
    {
        var enabled = CreateEnabledTimerBaseline();
        return enabled with
        {
            EffectiveValues = enabled.EffectiveValues with
            {
                TimerLane = enabled.EffectiveValues.TimerLane! with { Enabled = false },
            },
        };
    }

    internal static RuntimePolicyResolutionRequest CreateResolutionRequest(
        RuntimePolicyBaselineSource baseline,
        params RuntimePolicyNarrowingOverride[] overrides) =>
        new(baseline.BaselineDigest, baseline, overrides);
}
