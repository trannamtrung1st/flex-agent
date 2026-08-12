using System.Text.Json.Serialization;

namespace FlexAgent.Contracts.Session;

[JsonConverter(typeof(JsonStringEnumConverter<FailedExecutionAttemptOutcomeCategoryV1>))]
public enum FailedExecutionAttemptOutcomeCategoryV1
{
    [JsonStringEnumMemberName("provider_timeout")]
    ProviderTimeout,

    [JsonStringEnumMemberName("provider_unavailable")]
    ProviderUnavailable,

    [JsonStringEnumMemberName("malformed_control")]
    MalformedControl,

    [JsonStringEnumMemberName("incomplete_control")]
    IncompleteControl,

    [JsonStringEnumMemberName("cancelled")]
    Cancelled,

    [JsonStringEnumMemberName("late_result")]
    LateResult,
}

[JsonConverter(typeof(JsonStringEnumConverter<AcceptedEffectOutcomeV1>))]
public enum AcceptedEffectOutcomeV1
{
    [JsonStringEnumMemberName("applied")]
    Applied,

    [JsonStringEnumMemberName("no_domain_effect")]
    NoDomainEffect,

    [JsonStringEnumMemberName("effect_failed")]
    EffectFailed,
}

[JsonConverter(typeof(JsonStringEnumConverter<ExecutionFailedReasonCategoryV1>))]
public enum ExecutionFailedReasonCategoryV1
{
    [JsonStringEnumMemberName("provider_timeout")]
    ProviderTimeout,

    [JsonStringEnumMemberName("provider_unavailable")]
    ProviderUnavailable,

    [JsonStringEnumMemberName("malformed_control")]
    MalformedControl,

    [JsonStringEnumMemberName("incomplete_control")]
    IncompleteControl,
}

[JsonConverter(typeof(JsonStringEnumConverter<CancelledReasonCategoryV1>))]
public enum CancelledReasonCategoryV1
{
    [JsonStringEnumMemberName("lifecycle_cancelled")]
    LifecycleCancelled,

    [JsonStringEnumMemberName("cutoff_exceeded")]
    CutoffExceeded,
}

[JsonConverter(typeof(JsonStringEnumConverter<PreExecutionRejectedReasonCategoryV1>))]
public enum PreExecutionRejectedReasonCategoryV1
{
    [JsonStringEnumMemberName("state_ineligible")]
    StateIneligible,

    [JsonStringEnumMemberName("authorization_revoked")]
    AuthorizationRevoked,

    [JsonStringEnumMemberName("policy_prohibited")]
    PolicyProhibited,

    [JsonStringEnumMemberName("budget_exhausted")]
    BudgetExhausted,
}

[JsonConverter(typeof(JsonStringEnumConverter<NoActionReasonCategoryV1>))]
public enum NoActionReasonCategoryV1
{
    [JsonStringEnumMemberName("intentional_silence")]
    IntentionalSilence,

    [JsonStringEnumMemberName("workflow_complete")]
    WorkflowComplete,

    [JsonStringEnumMemberName("awaiting_input")]
    AwaitingInput,
}

[JsonConverter(typeof(JsonStringEnumConverter<RejectionReasonCategoryV1>))]
public enum RejectionReasonCategoryV1
{
    [JsonStringEnumMemberName("policy_prohibited")]
    PolicyProhibited,

    [JsonStringEnumMemberName("capability_disabled")]
    CapabilityDisabled,

    [JsonStringEnumMemberName("payload_invalid")]
    PayloadInvalid,

    [JsonStringEnumMemberName("state_ineligible")]
    StateIneligible,

    [JsonStringEnumMemberName("budget_exhausted")]
    BudgetExhausted,

    [JsonStringEnumMemberName("cutoff_exceeded")]
    CutoffExceeded,
}

[JsonConverter(typeof(JsonStringEnumConverter<SuppressionReasonCategoryV1>))]
public enum SuppressionReasonCategoryV1
{
    [JsonStringEnumMemberName("visibility_bounded")]
    VisibilityBounded,

    [JsonStringEnumMemberName("duplicate_stale")]
    DuplicateStale,

    [JsonStringEnumMemberName("workflow_bounds")]
    WorkflowBounds,

    [JsonStringEnumMemberName("policy_prohibited")]
    PolicyProhibited,
}

[JsonConverter(typeof(JsonStringEnumConverter<TimerValidationOutcomeV1>))]
public enum TimerValidationOutcomeV1
{
    [JsonStringEnumMemberName("accepted")]
    Accepted,

    [JsonStringEnumMemberName("rejected")]
    Rejected,

    [JsonStringEnumMemberName("omitted")]
    Omitted,

    [JsonStringEnumMemberName("not_present")]
    NotPresent,
}
