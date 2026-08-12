using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

internal static class RuntimePolicyTestFixtures
{
    internal const string BaselineId = "baseline.p0.text.0001";

    internal static IReadOnlyList<DecisionTypeSchemaBinding> CreateP0DecisionSchemaBindings() =>
    [
        new DecisionTypeSchemaBinding(RuntimeDecisionTypes.EmitMessage, RuntimeContractVersions.AgentDecisionSchemaV1),
        new DecisionTypeSchemaBinding(RuntimeDecisionTypes.NoAction, RuntimeContractVersions.AgentDecisionSchemaV1),
    ];

    internal static RuntimePolicyEffectiveValues CreateEnabledTimerEffectiveValues() =>
        new()
        {
            InvocationContractVersion = RuntimeContractVersions.InvocationV1,
            DecisionContractVersion = RuntimeContractVersions.DecisionV1,
            DecisionValidationPolicyVersion = RuntimeContractVersions.DecisionValidationPolicyV1,
            DecisionSchemaBindings = CreateP0DecisionSchemaBindings(),
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
            ExplicitlyDisabledCapabilities = P0TextSessionRuntimeCapabilityPolicy
                .RequiredExplicitlyDisabledCapabilities
                .ToArray(),
        };

    internal static RuntimePolicyBaselineSource CreateBaseline(RuntimePolicyEffectiveValues values) =>
        new(
            BaselineId,
            RuntimePolicyBaselineContentDigest.Compute(values),
            values);

    internal static RuntimePolicyBaselineSource CreateEnabledTimerBaseline() =>
        CreateBaseline(CreateEnabledTimerEffectiveValues());

    internal static RuntimePolicyEffectiveValues CreateMultiStageTimerEffectiveValues()
    {
        var values = CreateEnabledTimerEffectiveValues();
        return values with
        {
            TimerLane = values.TimerLane! with
            {
                PermittedStages = ["active", "paused"],
            },
        };
    }

    internal static RuntimePolicyBaselineSource CreateMultiStageTimerBaseline() =>
        CreateBaseline(CreateMultiStageTimerEffectiveValues());

    internal static RuntimePolicyBaselineSource CreateDisabledTimerBaseline()
    {
        var values = CreateEnabledTimerEffectiveValues() with
        {
            TimerLane = CreateEnabledTimerEffectiveValues().TimerLane! with { Enabled = false },
        };
        return CreateBaseline(values);
    }

    internal static RuntimePolicyResolutionRequest CreateResolutionRequest(
        RuntimePolicyBaselineSource baseline,
        params RuntimePolicyNarrowingOverride[] overrides) =>
        new(baseline.BaselineDigest, baseline, overrides);
}
