using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Contracts.Session;

public sealed record TrustedTriggerV1(
    string SchemaVersion,
    string TriggerFamily,
    string TriggerType,
    string TriggerId,
    string IdempotencyKey,
    SessionOwnershipRefV1 Ownership,
    string Purpose,
    string? TurnId = null,
    string? ResponseSlotId = null,
    ProtectedPayloadRefV1? ProvenanceRef = null);

public sealed record TrustedTriggerProvenanceV1(
    string SchemaVersion,
    string TriggerFamily,
    string TriggerType,
    string TriggerId,
    string IdempotencyKey,
    string Purpose,
    string? TurnId = null,
    string? ResponseSlotId = null,
    ProtectedPayloadRefV1? ProvenanceRef = null);

public interface IAgentInvocationV1
{
    string SchemaVersion { get; }

    string AgentInvocationId { get; }

    string InvocationContractVersion { get; }

    string Purpose { get; }

    SessionOwnershipRefV1 Ownership { get; }

    TrustedTriggerProvenanceV1 Trigger { get; }

    string SessionSequence { get; }

    string Status { get; }

    string? PolicyDigest { get; }

    ProtectedPayloadRefV1? ContextRef { get; }
}

public sealed record AdmittedAgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerProvenanceV1 Trigger,
    string SessionSequence,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null) : IAgentInvocationV1
{
    public string Status => "admitted";
}

public sealed record ExecutingAgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerProvenanceV1 Trigger,
    string SessionSequence,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null) : IAgentInvocationV1
{
    public string Status => "executing";
}

public sealed record DecidedAgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerProvenanceV1 Trigger,
    string SessionSequence,
    string AgentDecisionId,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null) : IAgentInvocationV1
{
    public string Status => "decided";
}

public sealed record ExecutionFailedAgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerProvenanceV1 Trigger,
    string SessionSequence,
    string ExecutionOutcomeId,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null) : IAgentInvocationV1
{
    public string Status => "execution_failed";
}

public sealed record CancelledAgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerProvenanceV1 Trigger,
    string SessionSequence,
    string ExecutionOutcomeId,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null) : IAgentInvocationV1
{
    public string Status => "cancelled";
}

public interface IAgentInvocationExecutionAttemptV1
{
    string SchemaVersion { get; }

    string ExecutionAttemptId { get; }

    string AgentInvocationId { get; }

    int AttemptOrdinal { get; }

    string OutcomeCategory { get; }

    string StartedAt { get; }

    string CompletedAt { get; }

    ProtectedPayloadRefV1? ProviderRequestRef { get; }
}

public sealed record DecisionProducedExecutionAttemptV1(
    string SchemaVersion,
    string ExecutionAttemptId,
    string AgentInvocationId,
    int AttemptOrdinal,
    string StartedAt,
    string CompletedAt,
    string AgentDecisionId,
    ProtectedPayloadRefV1? ProviderRequestRef = null) : IAgentInvocationExecutionAttemptV1
{
    public string OutcomeCategory => "decision_produced";
}

public sealed record FailedExecutionAttemptV1(
    string SchemaVersion,
    string ExecutionAttemptId,
    string AgentInvocationId,
    int AttemptOrdinal,
    FailedExecutionAttemptOutcomeCategoryV1 OutcomeCategory,
    string StartedAt,
    string CompletedAt,
    ProtectedPayloadRefV1? ProviderRequestRef = null) : IAgentInvocationExecutionAttemptV1
{
    string IAgentInvocationExecutionAttemptV1.OutcomeCategory => OutcomeCategory switch
    {
        FailedExecutionAttemptOutcomeCategoryV1.ProviderTimeout => "provider_timeout",
        FailedExecutionAttemptOutcomeCategoryV1.ProviderUnavailable => "provider_unavailable",
        FailedExecutionAttemptOutcomeCategoryV1.MalformedControl => "malformed_control",
        FailedExecutionAttemptOutcomeCategoryV1.IncompleteControl => "incomplete_control",
        FailedExecutionAttemptOutcomeCategoryV1.Cancelled => "cancelled",
        FailedExecutionAttemptOutcomeCategoryV1.LateResult => "late_result",
        _ => throw new InvalidOperationException("Unknown failed execution attempt outcome category."),
    };
}

public interface IAgentInvocationExecutionOutcomeV1
{
    string SchemaVersion { get; }

    string ExecutionOutcomeId { get; }

    string AgentInvocationId { get; }

    string OutcomeCategory { get; }

    string TerminalAt { get; }
}

public sealed record ExecutionFailedOutcomeV1(
    string SchemaVersion,
    string ExecutionOutcomeId,
    string AgentInvocationId,
    ExecutionFailedReasonCategoryV1 ReasonCategory,
    string TerminalAt,
    string LastExecutionAttemptId) : IAgentInvocationExecutionOutcomeV1
{
    public string OutcomeCategory => "execution_failed";
}

public sealed record CancelledOutcomeV1(
    string SchemaVersion,
    string ExecutionOutcomeId,
    string AgentInvocationId,
    CancelledReasonCategoryV1 ReasonCategory,
    string TerminalAt,
    string? LastExecutionAttemptId = null) : IAgentInvocationExecutionOutcomeV1
{
    public string OutcomeCategory => "cancelled";
}

public sealed record LateResultOutcomeV1(
    string SchemaVersion,
    string ExecutionOutcomeId,
    string AgentInvocationId,
    string TerminalAt,
    string LastExecutionAttemptId) : IAgentInvocationExecutionOutcomeV1
{
    public string OutcomeCategory => "late_result";

    public string ReasonCategory => "late_provider_result";
}

public sealed record PreExecutionRejectedOutcomeV1(
    string SchemaVersion,
    string ExecutionOutcomeId,
    string AgentInvocationId,
    PreExecutionRejectedReasonCategoryV1 ReasonCategory,
    string TerminalAt) : IAgentInvocationExecutionOutcomeV1
{
    public string OutcomeCategory => "pre_execution_rejected";
}

public sealed record AttemptsExhaustedOutcomeV1(
    string SchemaVersion,
    string ExecutionOutcomeId,
    string AgentInvocationId,
    string TerminalAt,
    string LastExecutionAttemptId) : IAgentInvocationExecutionOutcomeV1
{
    public string OutcomeCategory => "attempts_exhausted";

    public string ReasonCategory => "retry_budget_exhausted";
}

public sealed record EmitMessageDecisionPayloadV1(
    string CommunicationPurpose,
    string? TurnId = null,
    string? ResponseSlotId = null);

public sealed record NoActionDecisionPayloadV1(string ReasonCategory);

public sealed record NextTimerRequestV1(
    string RelativeDelay,
    string ExpectedScheduleRevision);

public interface IAgentDecisionV1
{
    string SchemaVersion { get; }

    string AgentDecisionId { get; }

    string AgentInvocationId { get; }

    string DecisionType { get; }

    string ProducedAt { get; }

    NextTimerRequestV1? NextTimerRequest { get; }

    ProtectedPayloadRefV1? PayloadRef { get; }
}

public sealed record EmitMessageAgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string ProducedAt,
    EmitMessageDecisionPayloadV1 EmitMessage,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null) : IAgentDecisionV1
{
    public string DecisionType => "emit_message";
}

public sealed record NoActionAgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string ProducedAt,
    NoActionDecisionPayloadV1 NoAction,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null) : IAgentDecisionV1
{
    public string DecisionType => "no_action";
}

public sealed record RequestToolAgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string ProducedAt,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null) : IAgentDecisionV1
{
    public string DecisionType => "request_tool";
}

public sealed record ProposeTransitionAgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string ProducedAt,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null) : IAgentDecisionV1
{
    public string DecisionType => "propose_transition";
}

public sealed record EscalateAgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string ProducedAt,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null) : IAgentDecisionV1
{
    public string DecisionType => "escalate";
}

public interface IDecisionValidationEffectV1
{
    string SchemaVersion { get; }

    string ValidationEffectId { get; }

    string AgentDecisionId { get; }

    string ValidationOutcome { get; }

    string EffectOutcome { get; }

    string ValidatedAt { get; }

    string? SessionSequence { get; }

    string? TimerValidationOutcome { get; }

    string? ScheduleRevisionId { get; }
}

public sealed record AcceptedDecisionValidationEffectV1(
    string SchemaVersion,
    string ValidationEffectId,
    string AgentDecisionId,
    AcceptedEffectOutcomeV1 EffectOutcome,
    string ValidatedAt,
    string? SessionSequence = null,
    string? TimerValidationOutcome = null,
    string? ScheduleRevisionId = null) : IDecisionValidationEffectV1
{
    public string ValidationOutcome => "accepted";

    string IDecisionValidationEffectV1.EffectOutcome => EffectOutcome switch
    {
        AcceptedEffectOutcomeV1.Applied => "applied",
        AcceptedEffectOutcomeV1.NoDomainEffect => "no_domain_effect",
        AcceptedEffectOutcomeV1.EffectFailed => "effect_failed",
        _ => throw new InvalidOperationException("Unknown accepted effect outcome."),
    };
}

public sealed record RejectedDecisionValidationEffectV1(
    string SchemaVersion,
    string ValidationEffectId,
    string AgentDecisionId,
    string RejectionReasonCategory,
    string ValidatedAt,
    string? SessionSequence = null,
    string? TimerValidationOutcome = null,
    string? ScheduleRevisionId = null) : IDecisionValidationEffectV1
{
    public string ValidationOutcome => "rejected";

    public string EffectOutcome => "not_attempted";
}

public sealed record SuppressedDecisionValidationEffectV1(
    string SchemaVersion,
    string ValidationEffectId,
    string AgentDecisionId,
    string SuppressionReasonCategory,
    string ValidatedAt,
    string? SessionSequence = null,
    string? TimerValidationOutcome = null,
    string? ScheduleRevisionId = null) : IDecisionValidationEffectV1
{
    public string ValidationOutcome => "suppressed";

    public string EffectOutcome => "not_attempted";
}

public sealed record TimerScheduleRevisionV1(
    string SchemaVersion,
    string ScheduleRevisionId,
    string SessionId,
    string ScheduleRevision,
    string LaneState,
    string RelativeDelay,
    string RequestedByCategory,
    string CreatedAt,
    string? ActiveRemainingDelay = null,
    string? DueAt = null,
    string? DrivingDecisionId = null,
    string? FiredInvocationId = null);
