using System.Text.RegularExpressions;

namespace FlexAgent.Sessions.Domain;

public static partial class FrozenRuntimePolicyResolver
{
    private static readonly P0TextSessionRuntimeCapabilityPolicy P0Kernel =
        P0TextSessionRuntimeCapabilityPolicy.Create();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowercaseSha256Pattern();

    public static RuntimePolicyResolutionResult Resolve(RuntimePolicyResolutionRequest request)
    {
        if (!LowercaseSha256Pattern().IsMatch(request.ExpectedBaselineDigest)
            || !string.Equals(
                request.ExpectedBaselineDigest,
                request.Baseline.BaselineDigest,
                StringComparison.Ordinal))
        {
            return Failure(RuntimePolicyResolutionOutcomeCodes.BaselineDigestMismatch);
        }

        if (!RuntimePolicyEffectiveValuesValidator.HasRequiredCommunicationPolicy(
                request.Baseline.EffectiveValues))
        {
            return Failure(RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues);
        }

        var computedBaselineDigest =
            RuntimePolicyEffectiveValuesDigestComputer.Compute(request.Baseline.EffectiveValues);
        if (!string.Equals(
                computedBaselineDigest,
                request.ExpectedBaselineDigest,
                StringComparison.Ordinal))
        {
            return Failure(RuntimePolicyResolutionOutcomeCodes.BaselineContentDigestMismatch);
        }

        if (!TryValidateScopeKinds(request.NarrowingOverrides, out var failureCode))
        {
            return Failure(failureCode);
        }

        if (HasWidening(request.Baseline.EffectiveValues, request.NarrowingOverrides))
        {
            return Failure(RuntimePolicyResolutionOutcomeCodes.WideningRejected);
        }

        var merged = MergeEffectiveValues(
            request.Baseline.EffectiveValues,
            request.NarrowingOverrides);

        if (!TryValidateMergedValues(merged, out failureCode))
        {
            return Failure(failureCode);
        }

        if (!TryValidateAgainstP0Kernel(merged, out failureCode))
        {
            return Failure(failureCode);
        }

        if (!TryBuildPolicy(merged, out var policy, out failureCode))
        {
            return Failure(failureCode);
        }

        return new RuntimePolicyResolutionResult(
            true,
            RuntimePolicyResolutionOutcomeCodes.Succeeded,
            policy);
    }

    private static bool TryValidateScopeKinds(
        IReadOnlyList<RuntimePolicyNarrowingOverride> overrides,
        out string failureCode)
    {
        failureCode = RuntimePolicyResolutionOutcomeCodes.UnknownScopeKind;
        foreach (var scopeOverride in overrides)
        {
            if (!IsKnownScopeKind(scopeOverride.ScopeKind))
            {
                return false;
            }
        }

        failureCode = string.Empty;
        return true;
    }

    private static bool IsKnownScopeKind(string scopeKind) =>
        scopeKind is RuntimePolicyScopeKinds.Harness
            or RuntimePolicyScopeKinds.Activity
            or RuntimePolicyScopeKinds.Session;

    private static RuntimePolicyEffectiveValues MergeEffectiveValues(
        RuntimePolicyEffectiveValues baseline,
        IReadOnlyList<RuntimePolicyNarrowingOverride> overrides)
    {
        var merged = baseline;
        foreach (var scopeOverride in overrides.OrderBy(static item => ScopeOrder(item.ScopeKind)))
        {
            merged = ApplyNarrowing(merged, scopeOverride.Narrowing);
        }

        return merged;
    }

    private static int ScopeOrder(string scopeKind) =>
        scopeKind switch
        {
            RuntimePolicyScopeKinds.Harness => 0,
            RuntimePolicyScopeKinds.Activity => 1,
            RuntimePolicyScopeKinds.Session => 2,
            _ => throw new InvalidOperationException("unknown scope"),
        };

    private static RuntimePolicyEffectiveValues ApplyNarrowing(
        RuntimePolicyEffectiveValues current,
        RuntimePolicyNarrowingValues narrowing)
    {
        var timerLane = current.TimerLane;
        if (narrowing.TimerLaneEnabled == false)
        {
            timerLane = timerLane is null
                ? new TimerLanePolicyValues { Enabled = false }
                : timerLane with { Enabled = false };
        }
        else if (narrowing.TimerLaneEnabled == true && timerLane is { Enabled: false })
        {
            timerLane = timerLane with { Enabled = true };
        }

        if (timerLane is not null)
        {
            var budgets = timerLane.Budgets;
            if (narrowing.TimerBudgets is not null && budgets is not null)
            {
                budgets = budgets with
                {
                    MaxAcceptedReplacementsPerSession = narrowing.TimerBudgets.MaxAcceptedReplacementsPerSession
                        ?? budgets.MaxAcceptedReplacementsPerSession,
                    MaxTimerTriggeredInvocationsPerSession = narrowing.TimerBudgets.MaxTimerTriggeredInvocationsPerSession
                        ?? budgets.MaxTimerTriggeredInvocationsPerSession,
                    CooldownSeconds = narrowing.TimerBudgets.CooldownSeconds
                        ?? budgets.CooldownSeconds,
                    MaxConcurrentReplacements = narrowing.TimerBudgets.MaxConcurrentReplacements
                        ?? budgets.MaxConcurrentReplacements,
                    DuplicateSuppressionWindowSeconds = narrowing.TimerBudgets.DuplicateSuppressionWindowSeconds
                        ?? budgets.DuplicateSuppressionWindowSeconds,
                };
            }

            timerLane = timerLane with
            {
                DefaultDelay = narrowing.DefaultDelay ?? timerLane.DefaultDelay,
                MinRequestedDelay = narrowing.MinRequestedDelay ?? timerLane.MinRequestedDelay,
                MaxRequestedDelay = narrowing.MaxRequestedDelay ?? timerLane.MaxRequestedDelay,
                PermittedStages = narrowing.TimerPermittedStages ?? timerLane.PermittedStages,
                PermittedDecisionTypes = narrowing.TimerPermittedDecisionTypes ?? timerLane.PermittedDecisionTypes,
                Budgets = budgets,
            };
        }

        var invocationBounds = current.InvocationBounds;
        if (invocationBounds is not null
            && (narrowing.MaxAttemptsPerInvocation is not null
                || narrowing.MaxChainedInvocationsPerSession is not null))
        {
            invocationBounds = invocationBounds with
            {
                MaxAttemptsPerInvocation = narrowing.MaxAttemptsPerInvocation
                    ?? invocationBounds.MaxAttemptsPerInvocation,
                MaxChainedInvocationsPerSession = narrowing.MaxChainedInvocationsPerSession
                    ?? invocationBounds.MaxChainedInvocationsPerSession,
            };
        }

        return current with
        {
            TimerLane = timerLane,
            InvocationBounds = invocationBounds,
        };
    }

    private static bool TryValidateMergedValues(
        RuntimePolicyEffectiveValues merged,
        out string failureCode)
    {
        failureCode = RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues;

        if (!RuntimeContractVersions.IsSupportedInvocationContractVersion(merged.InvocationContractVersion)
            || !RuntimeContractVersions.IsSupportedDecisionContractVersion(merged.DecisionContractVersion)
            || !RuntimeContractVersions.IsSupportedDecisionValidationPolicyVersion(
                merged.DecisionValidationPolicyVersion)
            || merged.PermittedNonTimerTriggers is null
            || merged.PermittedDecisionTypes is null
            || merged.DecisionSchemaBindings is null
            || merged.InvocationBounds is null
            || merged.ExplicitlyDisabledCapabilities is null
            || !RuntimePolicyEffectiveValuesValidator.HasRequiredCommunicationPolicy(merged))
        {
            return false;
        }

        if (!ValidateDecisionSchemaBindings(merged.PermittedDecisionTypes, merged.DecisionSchemaBindings))
        {
            return false;
        }

        if (!ValidateInvocationBounds(merged.InvocationBounds))
        {
            return false;
        }

        if (merged.TimerLane is { Enabled: true })
        {
            if (!ValidateEnabledTimerLane(merged.TimerLane))
            {
                return false;
            }
        }

        failureCode = string.Empty;
        return true;
    }

    private static bool ValidateDecisionSchemaBindings(
        IReadOnlyList<string> permittedDecisionTypes,
        IReadOnlyList<DecisionTypeSchemaBinding> schemaBindings)
    {
        if (schemaBindings.Count != permittedDecisionTypes.Count)
        {
            return false;
        }

        var permitted = permittedDecisionTypes.ToHashSet(StringComparer.Ordinal);
        var bound = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in schemaBindings)
        {
            if (!permitted.Contains(binding.DecisionType)
                || !RuntimeContractVersions.IsSupportedAgentDecisionSchemaVersion(binding.SchemaVersion)
                || !bound.Add(binding.DecisionType))
            {
                return false;
            }
        }

        return bound.Count == permitted.Count;
    }

    private static bool ValidateInvocationBounds(InvocationBounds bounds) =>
        bounds.MaxAttemptsPerInvocation > 0
        && bounds.MaxChainedInvocationsPerSession > 0
        && bounds.MaxToolIterations >= 0
        && bounds.CooldownSeconds >= 0
        && bounds.DuplicateSuppressionWindowSeconds >= 0;

    private static bool ValidateEnabledTimerLane(TimerLanePolicyValues timerLane)
    {
        if (!Iso8601PositiveDuration.TryParse(timerLane.DefaultDelay, out var defaultDelay)
            || !Iso8601PositiveDuration.TryParse(timerLane.MinRequestedDelay, out var minDelay)
            || !Iso8601PositiveDuration.TryParse(timerLane.MaxRequestedDelay, out var maxDelay)
            || timerLane.Budgets is null
            || timerLane.PermittedStages is null
            || timerLane.PermittedDecisionTypes is null
            || timerLane.PermittedStages.Count == 0
            || timerLane.PermittedDecisionTypes.Count == 0
            || !string.Equals(
                timerLane.ClockBasis,
                TimerLaneClockBasis.ActiveSessionTime,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (minDelay.CompareTo(maxDelay) > 0
            || defaultDelay.CompareTo(minDelay) < 0
            || defaultDelay.CompareTo(maxDelay) > 0)
        {
            return false;
        }

        var budgets = timerLane.Budgets;
        return budgets.MaxAcceptedReplacementsPerSession > 0
            && budgets.MaxTimerTriggeredInvocationsPerSession > 0
            && budgets.MaxConcurrentReplacements > 0
            && budgets.CooldownSeconds >= 0
            && budgets.DuplicateSuppressionWindowSeconds >= 0;
    }

    private static bool TryValidateAgainstP0Kernel(
        RuntimePolicyEffectiveValues merged,
        out string failureCode)
    {
        failureCode = RuntimePolicyResolutionOutcomeCodes.P0CapabilityExceeded;

        if (!P0Kernel.ContainsRequiredExplicitlyDisabledCapabilities(merged.ExplicitlyDisabledCapabilities!))
        {
            return false;
        }

        foreach (var trigger in merged.PermittedNonTimerTriggers!)
        {
            if (!P0Kernel.IsTriggerSupportedByP0(trigger.TriggerFamily, trigger.TriggerType))
            {
                return false;
            }
        }

        foreach (var decisionType in merged.PermittedDecisionTypes!)
        {
            if (!P0Kernel.IsDecisionTypeSupportedByP0(decisionType))
            {
                return false;
            }
        }

        if (merged.TimerLane is { Enabled: true })
        {
            if (!P0Kernel.IsTimerTriggerSupportedByP0(
                    RuntimeTriggerIdentifiers.TimerEventFamily,
                    RuntimeTriggerIdentifiers.TimerLaneDefaultType))
            {
                return false;
            }

            foreach (var decisionType in merged.TimerLane.PermittedDecisionTypes!)
            {
                if (!P0Kernel.IsDecisionTypeSupportedByP0(decisionType))
                {
                    return false;
                }
            }
        }

        foreach (var capability in merged.ExplicitlyDisabledCapabilities!)
        {
            if (P0Kernel.IsCapabilitySupportedByP0(capability))
            {
                return false;
            }
        }

        failureCode = string.Empty;
        return true;
    }

    private static bool TryBuildPolicy(
        RuntimePolicyEffectiveValues merged,
        out FrozenTextSessionRuntimePolicy? policy,
        out string failureCode)
    {
        policy = null;
        failureCode = string.Empty;

        TimerLanePolicy? timerLane = null;
        if (merged.TimerLane is { Enabled: true })
        {
            if (!Iso8601PositiveDuration.TryParse(merged.TimerLane.DefaultDelay, out var defaultDelay)
                || !Iso8601PositiveDuration.TryParse(merged.TimerLane.MinRequestedDelay, out var minDelay)
                || !Iso8601PositiveDuration.TryParse(merged.TimerLane.MaxRequestedDelay, out var maxDelay))
            {
                failureCode = RuntimePolicyResolutionOutcomeCodes.InvalidPolicyValues;
                return false;
            }

            timerLane = new TimerLanePolicy(
                defaultDelay,
                minDelay,
                maxDelay,
                merged.TimerLane.ClockBasis!,
                merged.TimerLane.PermittedStages!,
                merged.TimerLane.PermittedDecisionTypes!,
                merged.TimerLane.Budgets!);
        }

        var withoutDigest = new FrozenTextSessionRuntimePolicy(
            merged.InvocationContractVersion!,
            merged.DecisionContractVersion!,
            merged.DecisionValidationPolicyVersion!,
            merged.DecisionSchemaBindings!,
            merged.PermittedNonTimerTriggers!,
            merged.PermittedDecisionTypes!,
            merged.AgentInitiatedOpeningPermitted!.Value,
            merged.AgentInitiatedClosingPermitted!.Value,
            merged.NoActionPermitted!.Value,
            merged.InvocationBounds!,
            timerLane,
            merged.ExplicitlyDisabledCapabilities!,
            policyDigest: string.Empty);

        var digest = RuntimePolicyDigestComputer.ComputeDigest(withoutDigest);
        policy = new FrozenTextSessionRuntimePolicy(
            withoutDigest.InvocationContractVersion,
            withoutDigest.DecisionContractVersion,
            withoutDigest.DecisionValidationPolicyVersion,
            withoutDigest.DecisionSchemaBindings,
            withoutDigest.PermittedNonTimerTriggers,
            withoutDigest.PermittedDecisionTypes,
            withoutDigest.AgentInitiatedOpeningPermitted,
            withoutDigest.AgentInitiatedClosingPermitted,
            withoutDigest.NoActionPermitted,
            withoutDigest.InvocationBounds,
            withoutDigest.TimerLane,
            withoutDigest.ExplicitlyDisabledCapabilities,
            digest);

        return true;
    }

    private static RuntimePolicyResolutionResult Failure(string outcomeCode) =>
        new(false, outcomeCode, null);

    private static bool HasWidening(
        RuntimePolicyEffectiveValues baseline,
        IReadOnlyList<RuntimePolicyNarrowingOverride> overrides)
    {
        var current = baseline;
        foreach (var scopeOverride in overrides.OrderBy(static item => ScopeOrder(item.ScopeKind)))
        {
            if (IsWidening(current, scopeOverride.Narrowing))
            {
                return true;
            }

            current = ApplyNarrowing(current, scopeOverride.Narrowing);
        }

        return false;
    }

    private static bool IsWidening(
        RuntimePolicyEffectiveValues baseline,
        RuntimePolicyNarrowingValues narrowing)
    {
        if (narrowing.TimerLaneEnabled == true && baseline.TimerLane is { Enabled: false })
        {
            return true;
        }

        if (baseline.TimerLane is { Enabled: true } baselineTimer)
        {
            if (narrowing.MaxRequestedDelay is not null
                && Iso8601PositiveDuration.TryParse(baselineTimer.MaxRequestedDelay, out var baselineMax)
                && Iso8601PositiveDuration.TryParse(narrowing.MaxRequestedDelay, out var narrowedMax)
                && narrowedMax.CompareTo(baselineMax) > 0)
            {
                return true;
            }

            if (narrowing.MinRequestedDelay is not null
                && Iso8601PositiveDuration.TryParse(baselineTimer.MinRequestedDelay, out var baselineMin)
                && Iso8601PositiveDuration.TryParse(narrowing.MinRequestedDelay, out var narrowedMin)
                && narrowedMin.CompareTo(baselineMin) < 0)
            {
                return true;
            }

            // Shorter default delay increases timer cadence; only lengthening the default is narrowing.
            if (narrowing.DefaultDelay is not null
                && Iso8601PositiveDuration.TryParse(baselineTimer.DefaultDelay, out var baselineDefault)
                && Iso8601PositiveDuration.TryParse(narrowing.DefaultDelay, out var narrowedDefault)
                && narrowedDefault.CompareTo(baselineDefault) < 0)
            {
                return true;
            }

            if (narrowing.TimerPermittedStages is not null
                && baselineTimer.PermittedStages is not null
                && IsCapabilityWidening(baselineTimer.PermittedStages, narrowing.TimerPermittedStages))
            {
                return true;
            }

            if (narrowing.TimerPermittedDecisionTypes is not null
                && baselineTimer.PermittedDecisionTypes is not null
                && IsCapabilityWidening(
                    baselineTimer.PermittedDecisionTypes,
                    narrowing.TimerPermittedDecisionTypes))
            {
                return true;
            }

            if (narrowing.TimerBudgets is not null && baselineTimer.Budgets is not null)
            {
                if (IsTimerBudgetWidening(baselineTimer.Budgets, narrowing.TimerBudgets))
                {
                    return true;
                }
            }
        }

        if (baseline.InvocationBounds is not null)
        {
            if (narrowing.MaxAttemptsPerInvocation is not null
                && narrowing.MaxAttemptsPerInvocation.Value > baseline.InvocationBounds.MaxAttemptsPerInvocation)
            {
                return true;
            }

            if (narrowing.MaxChainedInvocationsPerSession is not null
                && narrowing.MaxChainedInvocationsPerSession.Value
                    > baseline.InvocationBounds.MaxChainedInvocationsPerSession)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCapabilityWidening(
        IReadOnlyList<string> baselineValues,
        IReadOnlyList<string> narrowedValues)
    {
        if (narrowedValues.Count == 0)
        {
            return false;
        }

        var baselineSet = baselineValues.ToHashSet(StringComparer.Ordinal);
        return narrowedValues.Any(value => !baselineSet.Contains(value));
    }

    private static bool IsTimerBudgetWidening(
        TimerLaneBudgets baselineBudgets,
        TimerLaneBudgetsNarrowing narrowing)
    {
        if (narrowing.MaxAcceptedReplacementsPerSession is not null
            && narrowing.MaxAcceptedReplacementsPerSession.Value
                > baselineBudgets.MaxAcceptedReplacementsPerSession)
        {
            return true;
        }

        if (narrowing.MaxTimerTriggeredInvocationsPerSession is not null
            && narrowing.MaxTimerTriggeredInvocationsPerSession.Value
                > baselineBudgets.MaxTimerTriggeredInvocationsPerSession)
        {
            return true;
        }

        if (narrowing.CooldownSeconds is not null
            && narrowing.CooldownSeconds.Value < baselineBudgets.CooldownSeconds)
        {
            return true;
        }

        if (narrowing.MaxConcurrentReplacements is not null
            && narrowing.MaxConcurrentReplacements.Value > baselineBudgets.MaxConcurrentReplacements)
        {
            return true;
        }

        if (narrowing.DuplicateSuppressionWindowSeconds is not null
            && narrowing.DuplicateSuppressionWindowSeconds.Value
                < baselineBudgets.DuplicateSuppressionWindowSeconds)
        {
            return true;
        }

        return false;
    }
}
