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

public sealed record InProgressAgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerProvenanceV1 Trigger,
    string SessionSequence,
    string Status,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null) : IAgentInvocationV1;

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
    string OutcomeCategory,
    string StartedAt,
    string CompletedAt,
    ProtectedPayloadRefV1? ProviderRequestRef = null) : IAgentInvocationExecutionAttemptV1;

public sealed record AgentInvocationExecutionOutcomeV1(
    string SchemaVersion,
    string ExecutionOutcomeId,
    string AgentInvocationId,
    string OutcomeCategory,
    string ReasonCategory,
    string TerminalAt,
    string? LastExecutionAttemptId = null);

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

public sealed record DeferredAgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string DecisionType,
    string ProducedAt,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null) : IAgentDecisionV1;

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
    string EffectOutcome,
    string ValidatedAt,
    string? SessionSequence = null,
    string? TimerValidationOutcome = null,
    string? ScheduleRevisionId = null) : IDecisionValidationEffectV1
{
    public string ValidationOutcome => "accepted";
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
