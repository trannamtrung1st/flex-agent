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
