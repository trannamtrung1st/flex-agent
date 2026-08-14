namespace FlexAgent.Sessions.Domain;

public static class RuntimeContractVersions
{
    public const string InvocationV1 = "v1";
    public const string DecisionV1 = "v1";
    public const string DecisionValidationPolicyV1 = "v1";
    public const string AgentDecisionSchemaV1 = "v1";

    public static bool IsSupportedInvocationContractVersion(string? version) =>
        string.Equals(version, InvocationV1, StringComparison.Ordinal);

    public static bool IsSupportedDecisionContractVersion(string? version) =>
        string.Equals(version, DecisionV1, StringComparison.Ordinal);

    public static bool IsSupportedDecisionValidationPolicyVersion(string? version) =>
        string.Equals(version, DecisionValidationPolicyV1, StringComparison.Ordinal);

    public static bool IsSupportedAgentDecisionSchemaVersion(string? version) =>
        string.Equals(version, AgentDecisionSchemaV1, StringComparison.Ordinal);
}

public static class RuntimePolicyDomainKeys
{
    public const string AgentInvocationPolicy = "agent_invocation_policy";
}

public static class RuntimePolicyScopeKinds
{
    public const string Harness = "harness";
    public const string Activity = "activity";
    public const string Session = "session";
}

public static class TimerLaneClockBasis
{
    public const string ActiveSessionTime = "active_session_time";
}

public static class RuntimePolicyResolutionOutcomeCodes
{
    public const string Succeeded = "runtime_policy_resolution.succeeded";
    public const string BaselineDigestMismatch = "runtime_policy_resolution.baseline_digest_mismatch";
    public const string BaselineContentDigestMismatch = "runtime_policy_resolution.baseline_content_digest_mismatch";
    public const string WideningRejected = "runtime_policy_resolution.widening_rejected";
    public const string InvalidPolicyValues = "runtime_policy_resolution.invalid_policy_values";
    public const string P0CapabilityExceeded = "runtime_policy_resolution.p0_capability_exceeded";
    public const string UnknownScopeKind = "runtime_policy_resolution.unknown_scope_kind";
}

public sealed record DecisionTypeSchemaBinding(string DecisionType, string SchemaVersion);

public sealed record RuntimeTriggerDescriptor(string TriggerFamily, string TriggerType);

public sealed record InvocationBounds(
    int MaxAttemptsPerInvocation,
    int MaxChainedInvocationsPerSession,
    int MaxToolIterations,
    int CooldownSeconds,
    int DuplicateSuppressionWindowSeconds);

/// <summary>
/// Positive frozen publication limits. Concrete numbers come from resolved
/// policy; test fixtures are not product defaults.
/// </summary>
public sealed record StreamingPublicationBounds(
    int MaxFragmentUtf8Bytes,
    int MaxFragmentsPerSecond,
    int MaxFragmentCountPerMessage,
    int MaxAssembledResponseUtf8Bytes,
    int MaxInFlightStreamsPerSession);

public sealed record StreamingPublicationBoundsNarrowing(
    int? MaxFragmentUtf8Bytes,
    int? MaxFragmentsPerSecond,
    int? MaxFragmentCountPerMessage,
    int? MaxAssembledResponseUtf8Bytes,
    int? MaxInFlightStreamsPerSession);

public sealed record TimerLaneBudgets(
    int MaxAcceptedReplacementsPerSession,
    int MaxTimerTriggeredInvocationsPerSession,
    int CooldownSeconds,
    int MaxConcurrentReplacements,
    int DuplicateSuppressionWindowSeconds);

public sealed class TimerLanePolicy
{
    public TimerLanePolicy(
        Iso8601PositiveDuration defaultDelay,
        Iso8601PositiveDuration minRequestedDelay,
        Iso8601PositiveDuration maxRequestedDelay,
        string clockBasis,
        IReadOnlyList<string> permittedStages,
        IReadOnlyList<string> permittedDecisionTypes,
        TimerLaneBudgets budgets)
    {
        DefaultDelay = defaultDelay;
        MinRequestedDelay = minRequestedDelay;
        MaxRequestedDelay = maxRequestedDelay;
        ClockBasis = clockBasis;
        PermittedStages = RuntimePolicySnapshots.CopyStrings(permittedStages);
        PermittedDecisionTypes = RuntimePolicySnapshots.CopyStrings(permittedDecisionTypes);
        Budgets = budgets;
        IsEnabled = true;
    }

    public bool IsEnabled { get; }

    public Iso8601PositiveDuration DefaultDelay { get; }

    public Iso8601PositiveDuration MinRequestedDelay { get; }

    public Iso8601PositiveDuration MaxRequestedDelay { get; }

    public string ClockBasis { get; }

    public IReadOnlyList<string> PermittedStages { get; }

    public IReadOnlyList<string> PermittedDecisionTypes { get; }

    public TimerLaneBudgets Budgets { get; }
}

public sealed record TimerLanePolicyValues
{
    public bool Enabled { get; init; }

    public string? DefaultDelay { get; init; }

    public string? MinRequestedDelay { get; init; }

    public string? MaxRequestedDelay { get; init; }

    public string? ClockBasis { get; init; }

    public IReadOnlyList<string>? PermittedStages { get; init; }

    public IReadOnlyList<string>? PermittedDecisionTypes { get; init; }

    public TimerLaneBudgets? Budgets { get; init; }
}

public sealed record RuntimePolicyEffectiveValues
{
    public string? InvocationContractVersion { get; init; }

    public string? DecisionContractVersion { get; init; }

    public string? DecisionValidationPolicyVersion { get; init; }

    public IReadOnlyList<DecisionTypeSchemaBinding>? DecisionSchemaBindings { get; init; }

    public IReadOnlyList<RuntimeTriggerDescriptor>? PermittedNonTimerTriggers { get; init; }

    public IReadOnlyList<string>? PermittedDecisionTypes { get; init; }

    public bool? AgentInitiatedOpeningPermitted { get; init; }

    public bool? AgentInitiatedClosingPermitted { get; init; }

    public bool? NoActionPermitted { get; init; }

    public InvocationBounds? InvocationBounds { get; init; }

    public StreamingPublicationBounds? StreamingPublicationBounds { get; init; }

    public TimerLanePolicyValues? TimerLane { get; init; }

    public IReadOnlyList<string>? ExplicitlyDisabledCapabilities { get; init; }
}

public sealed record RuntimePolicyNarrowingValues
{
    public bool? TimerLaneEnabled { get; init; }

    public string? DefaultDelay { get; init; }

    public string? MinRequestedDelay { get; init; }

    public string? MaxRequestedDelay { get; init; }

    public IReadOnlyList<string>? TimerPermittedStages { get; init; }

    public IReadOnlyList<string>? TimerPermittedDecisionTypes { get; init; }

    public TimerLaneBudgetsNarrowing? TimerBudgets { get; init; }

    public int? MaxAttemptsPerInvocation { get; init; }

    public int? MaxChainedInvocationsPerSession { get; init; }

    public StreamingPublicationBoundsNarrowing? StreamingPublicationBounds { get; init; }
}

public sealed record TimerLaneBudgetsNarrowing
{
    public int? MaxAcceptedReplacementsPerSession { get; init; }

    public int? MaxTimerTriggeredInvocationsPerSession { get; init; }

    public int? CooldownSeconds { get; init; }

    public int? MaxConcurrentReplacements { get; init; }

    public int? DuplicateSuppressionWindowSeconds { get; init; }
}

public sealed record RuntimePolicyBaselineSource(
    string BaselineId,
    string BaselineDigest,
    RuntimePolicyEffectiveValues EffectiveValues);

public sealed record RuntimePolicyNarrowingOverride(
    string ScopeKind,
    RuntimePolicyNarrowingValues Narrowing);

public sealed record RuntimePolicyResolutionRequest(
    string ExpectedBaselineDigest,
    RuntimePolicyBaselineSource Baseline,
    IReadOnlyList<RuntimePolicyNarrowingOverride> NarrowingOverrides);

public sealed record RuntimePolicyResolutionResult(
    bool Succeeded,
    string OutcomeCode,
    FrozenTextSessionRuntimePolicy? Policy);

/// <summary>
/// Computes the canonical baseline-content digest verified by <see cref="FrozenRuntimePolicyResolver"/>.
/// </summary>
public static class RuntimePolicyBaselineContentDigest
{
    public static string Compute(RuntimePolicyEffectiveValues values)
    {
        if (!RuntimePolicyEffectiveValuesValidator.HasRequiredFreezeInputs(values))
        {
            throw new ArgumentException(
                "Agent-initiated communication, no-action, and streaming publication bounds must be explicit.",
                nameof(values));
        }

        return RuntimePolicyEffectiveValuesDigestComputer.Compute(values);
    }
}
