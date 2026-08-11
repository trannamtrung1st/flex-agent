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

public sealed record AgentInvocationV1(
    string SchemaVersion,
    string AgentInvocationId,
    string InvocationContractVersion,
    string Purpose,
    SessionOwnershipRefV1 Ownership,
    TrustedTriggerV1 Trigger,
    string SessionSequence,
    string Status,
    string? PolicyDigest = null,
    ProtectedPayloadRefV1? ContextRef = null,
    string? AgentDecisionId = null,
    string? ExecutionOutcomeId = null);

public sealed record AgentInvocationExecutionAttemptV1(
    string SchemaVersion,
    string ExecutionAttemptId,
    string AgentInvocationId,
    int AttemptOrdinal,
    string OutcomeCategory,
    string StartedAt,
    string CompletedAt,
    ProtectedPayloadRefV1? ProviderRequestRef = null,
    string? AgentDecisionId = null);

public sealed record EmitMessageDecisionPayloadV1(
    string CommunicationPurpose,
    string? TurnId = null,
    string? ResponseSlotId = null);

public sealed record NoActionDecisionPayloadV1(string ReasonCategory);

public sealed record NextTimerRequestV1(
    string RelativeDelay,
    string ExpectedScheduleRevision);

public sealed record AgentDecisionV1(
    string SchemaVersion,
    string AgentDecisionId,
    string AgentInvocationId,
    string DecisionType,
    string ProducedAt,
    EmitMessageDecisionPayloadV1? EmitMessage = null,
    NoActionDecisionPayloadV1? NoAction = null,
    NextTimerRequestV1? NextTimerRequest = null,
    ProtectedPayloadRefV1? PayloadRef = null);

public sealed record DecisionValidationEffectV1(
    string SchemaVersion,
    string ValidationEffectId,
    string AgentDecisionId,
    string ValidationOutcome,
    string EffectOutcome,
    string ValidatedAt,
    string? RejectionReasonCategory = null,
    string? SessionSequence = null,
    string? TimerValidationOutcome = null,
    string? ScheduleRevisionId = null);

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
