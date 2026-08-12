using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class FrozenRuntimePolicyResolverTests
{
    [Fact]
    public void Resolve_produces_frozen_policy_matching_baseline_with_enabled_timer_lane()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(baseline);

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Policy);
        var policy = result.Policy!;
        Assert.Equal(RuntimeContractVersions.InvocationV1, policy.InvocationContractVersion);
        Assert.Equal(RuntimeContractVersions.DecisionV1, policy.DecisionContractVersion);
        Assert.Contains(
            policy.PermittedNonTimerTriggers,
            trigger => trigger.TriggerType == RuntimeTriggerIdentifiers.ParticipantMessageType);
        Assert.Contains(
            policy.PermittedDecisionTypes,
            decisionType => decisionType == RuntimeDecisionTypes.EmitMessage);
        Assert.Contains(
            policy.PermittedDecisionTypes,
            decisionType => decisionType == RuntimeDecisionTypes.NoAction);
        Assert.True(policy.AgentInitiatedOpeningPermitted);
        Assert.True(policy.AgentInitiatedClosingPermitted);
        Assert.True(policy.NoActionPermitted);
        Assert.NotNull(policy.TimerLane);
        Assert.True(policy.TimerLane!.IsEnabled);
        Assert.Equal("PT5M", policy.TimerLane.DefaultDelay.WireValue);
        Assert.False(string.IsNullOrWhiteSpace(policy.PolicyDigest));
    }

    [Fact]
    public void Resolve_includes_timer_trigger_only_when_lane_is_enabled()
    {
        var enabled = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(
                RuntimePolicyTestFixtures.CreateEnabledTimerBaseline()));
        var disabled = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(
                RuntimePolicyTestFixtures.CreateDisabledTimerBaseline()));

        Assert.True(enabled.Policy!.IsTimerTriggerPermitted);
        Assert.False(disabled.Policy!.IsTimerTriggerPermitted);
        Assert.Null(disabled.Policy.TimerLane);
    }

    [Fact]
    public void Resolve_rejects_baseline_digest_drift()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = new RuntimePolicyResolutionRequest(
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            baseline,
            []);

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.BaselineDigestMismatch, result.OutcomeCode);
        Assert.Null(result.Policy);
    }

    [Fact]
    public void Lower_scope_may_disable_timer_lane()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { TimerLaneEnabled = false }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Null(result.Policy!.TimerLane);
        Assert.False(result.Policy.IsTimerTriggerPermitted);
    }

    [Fact]
    public void Lower_scope_may_tighten_timer_delay_bounds()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Activity,
                new RuntimePolicyNarrowingValues
                {
                    MinRequestedDelay = "PT2M",
                    MaxRequestedDelay = "PT15M",
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("PT2M", result.Policy!.TimerLane!.MinRequestedDelay.WireValue);
        Assert.Equal("PT15M", result.Policy.TimerLane.MaxRequestedDelay.WireValue);
    }

    [Fact]
    public void Lower_scope_cannot_enable_timer_lane_when_baseline_disables_it()
    {
        var baseline = RuntimePolicyTestFixtures.CreateDisabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { TimerLaneEnabled = true }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_cannot_widen_max_requested_delay()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Harness,
                new RuntimePolicyNarrowingValues { MaxRequestedDelay = "PT45M" }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_cannot_loosen_min_requested_delay()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { MinRequestedDelay = "PT30S" }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_cannot_loosen_invocation_attempt_bound()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { MaxAttemptsPerInvocation = 5 }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_may_tighten_invocation_attempt_bound()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { MaxAttemptsPerInvocation = 2 }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(2, result.Policy!.InvocationBounds.MaxAttemptsPerInvocation);
    }

    [Fact]
    public void Resolve_fails_closed_when_required_positive_bounds_are_missing()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var invalid = baseline with
        {
            EffectiveValues = baseline.EffectiveValues with
            {
                InvocationBounds = baseline.EffectiveValues.InvocationBounds! with
                {
                    MaxAttemptsPerInvocation = 0,
                },
            },
        };

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_deferred_trigger_not_supported_by_p0_kernel()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var invalid = baseline with
        {
            EffectiveValues = baseline.EffectiveValues with
            {
                PermittedNonTimerTriggers =
                [
                    ..baseline.EffectiveValues.PermittedNonTimerTriggers!,
                    new RuntimeTriggerDescriptor("tool_result", "tool_result.participant_tool"),
                ],
            },
        };

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_deferred_decision_type()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var invalid = baseline with
        {
            EffectiveValues = baseline.EffectiveValues with
            {
                PermittedDecisionTypes =
                [
                    RuntimeDecisionTypes.EmitMessage,
                    RuntimeDecisionTypes.RequestTool,
                ],
            },
        };

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_deferred_timer_lane_decision_type()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var invalid = baseline with
        {
            EffectiveValues = baseline.EffectiveValues with
            {
                TimerLane = baseline.EffectiveValues.TimerLane! with
                {
                    PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.RequestTool],
                },
            },
        };

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Policy_digest_is_lowercase_sha256_hex()
    {
        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(
                RuntimePolicyTestFixtures.CreateEnabledTimerBaseline()));

        Assert.Matches("^[0-9a-f]{64}$", result.Policy!.PolicyDigest);
    }

    [Fact]
    public void Policy_digest_is_stable_for_identical_effective_values()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var first = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(baseline));
        var second = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(baseline));

        Assert.Equal(first.Policy!.PolicyDigest, second.Policy!.PolicyDigest);
    }

    [Fact]
    public void Policy_digest_changes_when_effective_values_change()
    {
        var enabled = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(
                RuntimePolicyTestFixtures.CreateEnabledTimerBaseline()));
        var disabled = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(
                RuntimePolicyTestFixtures.CreateDisabledTimerBaseline()));

        Assert.NotEqual(enabled.Policy!.PolicyDigest, disabled.Policy!.PolicyDigest);
    }
}
