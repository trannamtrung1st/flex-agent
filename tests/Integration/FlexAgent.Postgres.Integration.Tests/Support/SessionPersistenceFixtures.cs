using FlexAgent.Sessions.Domain;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class SessionPersistenceFixtures
{
    internal static FrozenTextSessionRuntimePolicy ResolveEnabledTimerPolicy(int cooldownSeconds = 5)
    {
        var values = new RuntimePolicyEffectiveValues
        {
            InvocationContractVersion = RuntimeContractVersions.InvocationV1,
            DecisionContractVersion = RuntimeContractVersions.DecisionV1,
            DecisionValidationPolicyVersion = RuntimeContractVersions.DecisionValidationPolicyV1,
            DecisionSchemaBindings =
            [
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.EmitMessage, RuntimeContractVersions.AgentDecisionSchemaV1),
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.NoAction, RuntimeContractVersions.AgentDecisionSchemaV1),
            ],
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
            InvocationBounds = new InvocationBounds(3, 10, 0, cooldownSeconds, 30),
            TimerLane = new TimerLanePolicyValues
            {
                Enabled = true,
                DefaultDelay = "PT5M",
                MinRequestedDelay = "PT1M",
                MaxRequestedDelay = "PT30M",
                ClockBasis = TimerLaneClockBasis.ActiveSessionTime,
                PermittedStages = ["active"],
                PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
                Budgets = new TimerLaneBudgets(5, 8, 10, 1, 30),
            },
            ExplicitlyDisabledCapabilities = P0TextSessionRuntimeCapabilityPolicy
                .RequiredExplicitlyDisabledCapabilities
                .ToArray(),
        };
        var baseline = new RuntimePolicyBaselineSource(
            "baseline.p0.text.0001",
            RuntimePolicyBaselineContentDigest.Compute(values),
            values);
        return FrozenRuntimePolicyResolver.Resolve(
            new RuntimePolicyResolutionRequest(baseline.BaselineDigest, baseline, [])).Policy
            ?? throw new InvalidOperationException("Test fixture policy resolution failed.");
    }

    internal static TrustedSessionBinding CreateBinding(Guid organizationId, int cooldownSeconds = 5)
    {
        var policy = ResolveEnabledTimerPolicy(cooldownSeconds);
        return new TrustedSessionBinding(
            new SessionOwnership(
                organizationId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()),
            "cfg.p0.text",
            policy.PolicyDigest,
            "man.p0.text",
            policy,
            [],
            [],
            []);
    }

    internal static TrustedTrigger OpeningTrigger(string triggerId = "trig.opening.1") =>
        new(
            RuntimeTriggerIdentifiers.WorkflowEventFamily,
            RuntimeTriggerIdentifiers.AgentOpeningType,
            triggerId,
            InvocationPurposes.AgentOpening,
            null,
            null);

    internal static TrustedRuntimeActor Actor(Guid actorId) =>
        new(actorId, "synthetic.test_actor");
}
