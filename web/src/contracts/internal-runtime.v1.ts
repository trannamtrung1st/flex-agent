/**
 * Protected Session runtime contracts mirrored from canonical JSON Schema.
 * These types are for contract parity and server-side tooling only; they must
 * not be imported by participant-facing browser API surfaces.
 */
import type { PositiveInt64WireString, SchemaVersionV1, SessionOwnershipRefV1 } from './v1';

export interface ProtectedPayloadRefV1 {
  protected_ref: string;
  content_digest: string;
}

export interface TrustedTriggerV1 {
  schema_version: SchemaVersionV1;
  trigger_family:
    | 'participant_input'
    | 'interaction_signal'
    | 'workflow_event'
    | 'timer_event'
    | 'tool_result'
    | 'system_event';
  trigger_type: string;
  trigger_id: string;
  idempotency_key: string;
  ownership: SessionOwnershipRefV1;
  purpose: string;
  turn_id?: string;
  response_slot_id?: string;
  provenance_ref?: ProtectedPayloadRefV1;
}

export interface TrustedTriggerProvenanceV1 {
  schema_version: SchemaVersionV1;
  trigger_family:
    | 'participant_input'
    | 'interaction_signal'
    | 'workflow_event'
    | 'timer_event'
    | 'tool_result'
    | 'system_event';
  trigger_type: string;
  trigger_id: string;
  idempotency_key: string;
  purpose: string;
  turn_id?: string;
  response_slot_id?: string;
  provenance_ref?: ProtectedPayloadRefV1;
}

interface AgentInvocationCoreV1 {
  schema_version: SchemaVersionV1;
  agent_invocation_id: string;
  invocation_contract_version: 'v1';
  purpose: string;
  ownership: SessionOwnershipRefV1;
  trigger: TrustedTriggerProvenanceV1;
  session_sequence: PositiveInt64WireString;
  policy_digest?: string;
  context_ref?: ProtectedPayloadRefV1;
}

export interface InProgressAgentInvocationV1 extends AgentInvocationCoreV1 {
  status: 'admitted' | 'executing';
}

export interface DecidedAgentInvocationV1 extends AgentInvocationCoreV1 {
  status: 'decided';
  agent_decision_id: string;
}

export interface ExecutionFailedAgentInvocationV1 extends AgentInvocationCoreV1 {
  status: 'execution_failed';
  execution_outcome_id: string;
}

export interface CancelledAgentInvocationV1 extends AgentInvocationCoreV1 {
  status: 'cancelled';
  execution_outcome_id: string;
}

export type AgentInvocationV1 =
  | InProgressAgentInvocationV1
  | DecidedAgentInvocationV1
  | ExecutionFailedAgentInvocationV1
  | CancelledAgentInvocationV1;

interface ExecutionAttemptCoreV1 {
  schema_version: SchemaVersionV1;
  execution_attempt_id: string;
  agent_invocation_id: string;
  attempt_ordinal: number;
  started_at: string;
  completed_at: string;
  provider_request_ref?: ProtectedPayloadRefV1;
}

export interface DecisionProducedExecutionAttemptV1 extends ExecutionAttemptCoreV1 {
  outcome_category: 'decision_produced';
  agent_decision_id: string;
}

export interface FailedExecutionAttemptV1 extends ExecutionAttemptCoreV1 {
  outcome_category:
    | 'provider_timeout'
    | 'provider_unavailable'
    | 'malformed_control'
    | 'incomplete_control'
    | 'cancelled'
    | 'late_result';
}

export type AgentInvocationExecutionAttemptV1 =
  | DecisionProducedExecutionAttemptV1
  | FailedExecutionAttemptV1;

export type ExecutionFailedReasonCategoryV1 =
  | 'provider_timeout'
  | 'provider_unavailable'
  | 'malformed_control'
  | 'incomplete_control';

export type CancelledReasonCategoryV1 = 'lifecycle_cancelled' | 'cutoff_exceeded';

export type PreExecutionRejectedReasonCategoryV1 =
  | 'state_ineligible'
  | 'authorization_revoked'
  | 'policy_prohibited'
  | 'budget_exhausted';

interface ExecutionOutcomeCoreV1 {
  schema_version: SchemaVersionV1;
  execution_outcome_id: string;
  agent_invocation_id: string;
  terminal_at: string;
  last_execution_attempt_id?: string;
}

export interface ExecutionFailedOutcomeV1 extends ExecutionOutcomeCoreV1 {
  outcome_category: 'execution_failed';
  reason_category: ExecutionFailedReasonCategoryV1;
}

export interface CancelledOutcomeV1 extends ExecutionOutcomeCoreV1 {
  outcome_category: 'cancelled';
  reason_category: CancelledReasonCategoryV1;
}

export interface LateResultOutcomeV1 extends ExecutionOutcomeCoreV1 {
  outcome_category: 'late_result';
  reason_category: 'late_provider_result';
}

export interface PreExecutionRejectedOutcomeV1 extends ExecutionOutcomeCoreV1 {
  outcome_category: 'pre_execution_rejected';
  reason_category: PreExecutionRejectedReasonCategoryV1;
}

export interface AttemptsExhaustedOutcomeV1 extends ExecutionOutcomeCoreV1 {
  outcome_category: 'attempts_exhausted';
  reason_category: 'retry_budget_exhausted';
}

export type AgentInvocationExecutionOutcomeV1 =
  | ExecutionFailedOutcomeV1
  | CancelledOutcomeV1
  | LateResultOutcomeV1
  | PreExecutionRejectedOutcomeV1
  | AttemptsExhaustedOutcomeV1;

export interface EmitMessageDecisionPayloadV1 {
  communication_purpose: string;
  turn_id?: string;
  response_slot_id?: string;
}

export interface NoActionDecisionPayloadV1 {
  reason_category: 'intentional_silence' | 'workflow_complete' | 'awaiting_input';
}

export interface NextTimerRequestV1 {
  relative_delay: string;
  expected_schedule_revision: PositiveInt64WireString;
}

interface AgentDecisionCoreV1 {
  schema_version: SchemaVersionV1;
  agent_decision_id: string;
  agent_invocation_id: string;
  produced_at: string;
  next_timer_request?: NextTimerRequestV1;
  payload_ref?: ProtectedPayloadRefV1;
}

export interface EmitMessageAgentDecisionV1 extends AgentDecisionCoreV1 {
  decision_type: 'emit_message';
  emit_message: EmitMessageDecisionPayloadV1;
}

export interface NoActionAgentDecisionV1 extends AgentDecisionCoreV1 {
  decision_type: 'no_action';
  no_action: NoActionDecisionPayloadV1;
}

export interface DeferredAgentDecisionV1 extends AgentDecisionCoreV1 {
  decision_type: 'request_tool' | 'propose_transition' | 'escalate';
}

export type AgentDecisionV1 =
  | EmitMessageAgentDecisionV1
  | NoActionAgentDecisionV1
  | DeferredAgentDecisionV1;

interface DecisionValidationEffectCoreV1 {
  schema_version: SchemaVersionV1;
  validation_effect_id: string;
  agent_decision_id: string;
  validated_at: string;
  session_sequence?: PositiveInt64WireString;
  timer_validation_outcome?: 'accepted' | 'rejected' | 'omitted' | 'not_present';
  schedule_revision_id?: string;
}

export interface AcceptedDecisionValidationEffectV1 extends DecisionValidationEffectCoreV1 {
  validation_outcome: 'accepted';
  effect_outcome: 'applied' | 'no_domain_effect' | 'effect_failed';
}

export type RejectionReasonCategoryV1 =
  | 'policy_prohibited'
  | 'capability_disabled'
  | 'payload_invalid'
  | 'state_ineligible'
  | 'budget_exhausted'
  | 'cutoff_exceeded';

export interface RejectedDecisionValidationEffectV1 extends DecisionValidationEffectCoreV1 {
  validation_outcome: 'rejected';
  effect_outcome: 'not_attempted';
  rejection_reason_category: RejectionReasonCategoryV1;
}

export type SuppressionReasonCategoryV1 =
  | 'visibility_bounded'
  | 'duplicate_stale'
  | 'workflow_bounds'
  | 'policy_prohibited';

export interface SuppressedDecisionValidationEffectV1 extends DecisionValidationEffectCoreV1 {
  validation_outcome: 'suppressed';
  effect_outcome: 'not_attempted';
  suppression_reason_category: SuppressionReasonCategoryV1;
}

export type DecisionValidationEffectV1 =
  | AcceptedDecisionValidationEffectV1
  | RejectedDecisionValidationEffectV1
  | SuppressedDecisionValidationEffectV1;

export interface TimerScheduleRevisionV1 {
  schema_version: SchemaVersionV1;
  schedule_revision_id: string;
  session_id: string;
  schedule_revision: PositiveInt64WireString;
  lane_state: 'pending' | 'claimed' | 'fired' | 'cancelled' | 'superseded' | 'expired';
  relative_delay: string;
  requested_by_category: 'default_cadence' | 'agent_recommendation' | 'successor_after_fire';
  created_at: string;
  active_remaining_delay?: string;
  due_at?: string;
  driving_decision_id?: string;
  fired_invocation_id?: string;
}
