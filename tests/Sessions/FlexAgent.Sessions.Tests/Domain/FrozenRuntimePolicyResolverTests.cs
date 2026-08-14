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
        Assert.Equal(512, policy.StreamingPublicationBounds.MaxFragmentUtf8Bytes);
        Assert.Equal(40, policy.StreamingPublicationBounds.MaxFragmentsPerSecond);
        Assert.Equal(64, policy.StreamingPublicationBounds.MaxFragmentCountPerMessage);
        Assert.Equal(8_192, policy.StreamingPublicationBounds.MaxAssembledResponseUtf8Bytes);
        Assert.Equal(2, policy.StreamingPublicationBounds.MaxInFlightStreamsPerSession);
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
    public void Resolve_rejects_baseline_content_digest_when_effective_values_tampered()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var tampered = baseline with
        {
            EffectiveValues = baseline.EffectiveValues with
            {
                InvocationBounds = baseline.EffectiveValues.InvocationBounds! with
                {
                    MaxAttemptsPerInvocation = 99,
                },
            },
        };

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(tampered));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.BaselineContentDigestMismatch, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_rejects_baseline_digest_metadata_mismatch()
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
    public void Lower_scope_cannot_shorten_timer_default_delay()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { DefaultDelay = "PT1M" }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_may_lengthen_timer_default_delay_within_bounds()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Activity,
                new RuntimePolicyNarrowingValues { DefaultDelay = "PT10M" }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("PT10M", result.Policy!.TimerLane!.DefaultDelay.WireValue);
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
    public void Lower_scope_may_narrow_timer_permitted_decision_types_to_subset()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues
                {
                    TimerPermittedDecisionTypes = [RuntimeDecisionTypes.NoAction],
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(
            [RuntimeDecisionTypes.NoAction],
            result.Policy!.TimerLane!.PermittedDecisionTypes);
    }

    [Fact]
    public void Lower_scope_cannot_widen_timer_permitted_decision_types()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues
                {
                    TimerPermittedDecisionTypes =
                    [
                        RuntimeDecisionTypes.EmitMessage,
                        RuntimeDecisionTypes.NoAction,
                        RuntimeDecisionTypes.RequestTool,
                    ],
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_may_narrow_timer_permitted_stages_to_subset()
    {
        var baseline = RuntimePolicyTestFixtures.CreateMultiStageTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Activity,
                new RuntimePolicyNarrowingValues
                {
                    TimerPermittedStages = ["active"],
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(["active"], result.Policy!.TimerLane!.PermittedStages);
    }

    [Fact]
    public void Lower_scope_cannot_widen_timer_permitted_stages()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues
                {
                    TimerPermittedStages = ["active", "paused"],
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_may_tighten_timer_replacement_budget()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues
                {
                    TimerBudgets = new TimerLaneBudgetsNarrowing
                    {
                        MaxAcceptedReplacementsPerSession = 3,
                        MaxTimerTriggeredInvocationsPerSession = 5,
                        CooldownSeconds = 20,
                        DuplicateSuppressionWindowSeconds = 45,
                    },
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(3, result.Policy!.TimerLane!.Budgets.MaxAcceptedReplacementsPerSession);
        Assert.Equal(5, result.Policy.TimerLane.Budgets.MaxTimerTriggeredInvocationsPerSession);
        Assert.Equal(20, result.Policy.TimerLane.Budgets.CooldownSeconds);
        Assert.Equal(45, result.Policy.TimerLane.Budgets.DuplicateSuppressionWindowSeconds);
    }

    [Fact]
    public void Lower_scope_cannot_widen_timer_replacement_budget()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Harness,
                new RuntimePolicyNarrowingValues
                {
                    TimerBudgets = new TimerLaneBudgetsNarrowing
                    {
                        MaxAcceptedReplacementsPerSession = 8,
                    },
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Lower_scope_cannot_loosen_timer_cooldown_budget()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues
                {
                    TimerBudgets = new TimerLaneBudgetsNarrowing { CooldownSeconds = 5 },
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_required_positive_bounds_are_missing()
    {
        var invalidValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues().InvocationBounds! with
            {
                MaxAttemptsPerInvocation = 0,
            },
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_streaming_publication_bounds_are_missing()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            StreamingPublicationBounds = null,
        };
        var referenceBaseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var baseline = new RuntimePolicyBaselineSource(
            RuntimePolicyTestFixtures.BaselineId,
            referenceBaseline.BaselineDigest,
            values);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(baseline));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
        Assert.Null(result.Policy);
    }

    public static TheoryData<Func<StreamingPublicationBounds, StreamingPublicationBounds>>
        NonPositiveStreamingBoundCases =>
        new()
        {
            bounds => bounds with { MaxFragmentUtf8Bytes = 0 },
            bounds => bounds with { MaxFragmentsPerSecond = 0 },
            bounds => bounds with { MaxFragmentCountPerMessage = -1 },
            bounds => bounds with { MaxAssembledResponseUtf8Bytes = 0 },
            bounds => bounds with { MaxInFlightStreamsPerSession = 0 },
        };

    [Theory]
    [MemberData(nameof(NonPositiveStreamingBoundCases))]
    public void Resolve_fails_closed_when_a_streaming_publication_bound_is_not_positive(
        Func<StreamingPublicationBounds, StreamingPublicationBounds> mutate)
    {
        var invalidValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            StreamingPublicationBounds = mutate(
                RuntimePolicyTestFixtures.CreateTestOnlyStreamingPublicationBounds()),
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
        Assert.Null(result.Policy);
    }

    [Fact]
    public void Lower_scope_may_tighten_streaming_publication_bounds()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Activity,
                new RuntimePolicyNarrowingValues
                {
                    StreamingPublicationBounds = new StreamingPublicationBoundsNarrowing(
                        MaxFragmentUtf8Bytes: 256,
                        MaxFragmentsPerSecond: 10,
                        MaxFragmentCountPerMessage: 16,
                        MaxAssembledResponseUtf8Bytes: 4_096,
                        MaxInFlightStreamsPerSession: 1),
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        var bounds = result.Policy!.StreamingPublicationBounds;
        Assert.Equal(256, bounds.MaxFragmentUtf8Bytes);
        Assert.Equal(10, bounds.MaxFragmentsPerSecond);
        Assert.Equal(16, bounds.MaxFragmentCountPerMessage);
        Assert.Equal(4_096, bounds.MaxAssembledResponseUtf8Bytes);
        Assert.Equal(1, bounds.MaxInFlightStreamsPerSession);
    }

    public static TheoryData<string, StreamingPublicationBoundsNarrowing> StreamingWideningCases =>
        new()
        {
            {
                "max_fragment_utf8_bytes",
                new StreamingPublicationBoundsNarrowing(1_024, null, null, null, null)
            },
            {
                "max_fragments_per_second",
                new StreamingPublicationBoundsNarrowing(null, 41, null, null, null)
            },
            {
                "max_fragment_count_per_message",
                new StreamingPublicationBoundsNarrowing(null, null, 65, null, null)
            },
            {
                "max_assembled_response_utf8_bytes",
                new StreamingPublicationBoundsNarrowing(null, null, null, 8_193, null)
            },
            {
                "max_in_flight_streams_per_session",
                new StreamingPublicationBoundsNarrowing(null, null, null, null, 3)
            },
        };

    [Theory]
    [MemberData(nameof(StreamingWideningCases))]
    public void Lower_scope_cannot_widen_streaming_publication_bounds(
        string boundName,
        StreamingPublicationBoundsNarrowing widening)
    {
        Assert.False(string.IsNullOrWhiteSpace(boundName));
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues { StreamingPublicationBounds = widening }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
        Assert.Null(result.Policy);
    }

    [Fact]
    public void Session_cannot_re_widen_a_streaming_bound_already_tightened_by_activity()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Activity,
                new RuntimePolicyNarrowingValues
                {
                    StreamingPublicationBounds = new StreamingPublicationBoundsNarrowing(
                        MaxFragmentUtf8Bytes: 256,
                        MaxFragmentsPerSecond: null,
                        MaxFragmentCountPerMessage: null,
                        MaxAssembledResponseUtf8Bytes: null,
                        MaxInFlightStreamsPerSession: null),
                }),
            new RuntimePolicyNarrowingOverride(
                RuntimePolicyScopeKinds.Session,
                new RuntimePolicyNarrowingValues
                {
                    StreamingPublicationBounds = new StreamingPublicationBoundsNarrowing(
                        MaxFragmentUtf8Bytes: 384,
                        MaxFragmentsPerSecond: null,
                        MaxFragmentCountPerMessage: null,
                        MaxAssembledResponseUtf8Bytes: null,
                        MaxInFlightStreamsPerSession: null),
                }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.WideningRejected, result.OutcomeCode);
        Assert.Null(result.Policy);
    }

    [Fact]
    public void Policy_digest_changes_when_streaming_publication_bounds_change()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var tightenedValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            StreamingPublicationBounds = RuntimePolicyTestFixtures.CreateTestOnlyStreamingPublicationBounds()
                with { MaxFragmentUtf8Bytes = 256 },
        };
        var tightened = RuntimePolicyTestFixtures.CreateBaseline(tightenedValues);

        var original = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(baseline));
        var changed = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(tightened));

        Assert.True(original.Succeeded, original.OutcomeCode);
        Assert.True(changed.Succeeded, changed.OutcomeCode);
        Assert.NotEqual(original.Policy!.PolicyDigest, changed.Policy!.PolicyDigest);
    }

    [Fact]
    public void Baseline_content_digest_compute_fails_when_streaming_publication_bounds_are_missing()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            StreamingPublicationBounds = null,
        };

        var exception = Assert.Throws<ArgumentException>(
            () => RuntimePolicyBaselineContentDigest.Compute(values));

        Assert.Contains("streaming", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<Func<RuntimePolicyEffectiveValues, RuntimePolicyEffectiveValues>>
        MissingCommunicationPolicyCases =>
        new()
        {
            values => values with { AgentInitiatedOpeningPermitted = null },
            values => values with { AgentInitiatedClosingPermitted = null },
            values => values with { NoActionPermitted = null },
        };

    [Theory]
    [MemberData(nameof(MissingCommunicationPolicyCases))]
    public void Resolve_fails_closed_when_required_communication_policy_flag_is_missing(
        Func<RuntimePolicyEffectiveValues, RuntimePolicyEffectiveValues> clearFlag)
    {
        var values = clearFlag(RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues());
        var referenceBaseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var baseline = new RuntimePolicyBaselineSource(
            RuntimePolicyTestFixtures.BaselineId,
            referenceBaseline.BaselineDigest,
            values);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(baseline));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
    }

    [Fact]
    public void Baseline_content_digest_compute_fails_when_communication_policy_is_incomplete()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            NoActionPermitted = null,
        };

        var exception = Assert.Throws<ArgumentException>(
            () => RuntimePolicyBaselineContentDigest.Compute(values));

        Assert.Contains("no-action", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_fails_closed_for_deferred_trigger_not_supported_by_p0_kernel()
    {
        var invalidValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            PermittedNonTimerTriggers =
            [
                ..RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues().PermittedNonTimerTriggers!,
                new RuntimeTriggerDescriptor("tool_result", "tool_result.participant_tool"),
            ],
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_deferred_decision_type()
    {
        var invalidValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            PermittedDecisionTypes =
            [
                RuntimeDecisionTypes.EmitMessage,
                RuntimeDecisionTypes.RequestTool,
            ],
            DecisionSchemaBindings =
            [
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.EmitMessage, RuntimeContractVersions.AgentDecisionSchemaV1),
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.RequestTool, RuntimeContractVersions.AgentDecisionSchemaV1),
            ],
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_deferred_timer_lane_decision_type()
    {
        var enabledValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues();
        var invalidValues = enabledValues with
        {
            TimerLane = enabledValues.TimerLane! with
            {
                PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.RequestTool],
            },
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_when_required_p0_disabled_capabilities_are_missing()
    {
        var invalidValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            ExplicitlyDisabledCapabilities = [],
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_unsupported_contract_version()
    {
        var invalidValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationContractVersion = "future-v99",
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_unsupported_timer_clock_basis()
    {
        var enabledValues = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues();
        var invalidValues = enabledValues with
        {
            TimerLane = enabledValues.TimerLane! with { ClockBasis = "wall_clock" },
        };
        var invalid = RuntimePolicyTestFixtures.CreateBaseline(invalidValues);

        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(invalid));

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues, result.OutcomeCode);
    }

    [Fact]
    public void Resolve_fails_closed_for_unknown_scope_kind()
    {
        var baseline = RuntimePolicyTestFixtures.CreateEnabledTimerBaseline();
        var request = RuntimePolicyTestFixtures.CreateResolutionRequest(
            baseline,
            new RuntimePolicyNarrowingOverride(
                "campaign",
                new RuntimePolicyNarrowingValues { MaxAttemptsPerInvocation = 2 }));

        var result = FrozenRuntimePolicyResolver.Resolve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(RuntimePolicyResolutionOutcomeCodes.UnknownScopeKind, result.OutcomeCode);
    }

    [Fact]
    public void Resolved_policy_is_immutable_after_source_collection_mutation()
    {
        var triggers = new List<RuntimeTriggerDescriptor>(
            RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues().PermittedNonTimerTriggers!);
        var decisions = new List<string> { RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction };
        var disabled = P0TextSessionRuntimeCapabilityPolicy.RequiredExplicitlyDisabledCapabilities.ToList();
        var schemaBindings = RuntimePolicyTestFixtures.CreateP0DecisionSchemaBindings().ToList();

        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            PermittedNonTimerTriggers = triggers,
            PermittedDecisionTypes = decisions,
            ExplicitlyDisabledCapabilities = disabled,
            DecisionSchemaBindings = schemaBindings,
        };
        var baseline = RuntimePolicyTestFixtures.CreateBaseline(values);
        var result = FrozenRuntimePolicyResolver.Resolve(
            RuntimePolicyTestFixtures.CreateResolutionRequest(baseline));

        Assert.True(result.Succeeded, result.OutcomeCode);
        var digestBefore = result.Policy!.PolicyDigest;
        var decisionCountBefore = result.Policy.PermittedDecisionTypes.Count;

        triggers.Add(new RuntimeTriggerDescriptor("tool_result", "tool_result.participant_tool"));
        decisions.Add(RuntimeDecisionTypes.RequestTool);
        disabled.Clear();
        schemaBindings.Add(new DecisionTypeSchemaBinding(RuntimeDecisionTypes.RequestTool, "v99"));

        Assert.Equal(decisionCountBefore, result.Policy.PermittedDecisionTypes.Count);
        Assert.DoesNotContain(
            result.Policy.PermittedDecisionTypes,
            decisionType => decisionType == RuntimeDecisionTypes.RequestTool);
        Assert.Equal(digestBefore, result.Policy.PolicyDigest);
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
