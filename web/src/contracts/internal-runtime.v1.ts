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

export interface AgentInvocationV1 {
  schema_version: SchemaVersionV1;
  agent_invocation_id: string;
  invocation_contract_version: 'v1';
  purpose: string;
  ownership: SessionOwnershipRefV1;
  trigger: TrustedTriggerProvenanceV1;
  session_sequence: PositiveInt64WireString;
  status: 'admitted' | 'executing' | 'decided' | 'execution_failed' | 'cancelled';
  policy_digest?: string;
  context_ref?: ProtectedPayloadRefV1;
  agent_decision_id?: string;
  execution_outcome_id?: string;
}

export interface AgentInvocationExecutionAttemptV1 {
  schema_version: SchemaVersionV1;
  execution_attempt_id: string;
  agent_invocation_id: string;
  attempt_ordinal: number;
  outcome_category:
    | 'decision_produced'
    | 'provider_timeout'
    | 'provider_unavailable'
    | 'malformed_control'
    | 'incomplete_control'
    | 'cancelled'
    | 'late_result';
  started_at: string;
  completed_at: string;
  provider_request_ref?: ProtectedPayloadRefV1;
  agent_decision_id?: string;
}

export interface AgentInvocationExecutionOutcomeV1 {
  schema_version: SchemaVersionV1;
  execution_outcome_id: string;
  agent_invocation_id: string;
  outcome_category:
    | 'execution_failed'
    | 'cancelled'
    | 'late_result'
    | 'pre_execution_rejected'
    | 'attempts_exhausted';
  reason_category:
    | 'provider_timeout'
    | 'provider_unavailable'
    | 'malformed_control'
    | 'incomplete_control'
    | 'lifecycle_cancelled'
    | 'cutoff_exceeded'
    | 'budget_exhausted'
    | 'admission_rejected'
    | 'late_provider_result';
  terminal_at: string;
  last_execution_attempt_id?: string;
}

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

export interface AgentDecisionV1 {
  schema_version: SchemaVersionV1;
  agent_decision_id: string;
  agent_invocation_id: string;
  decision_type: 'emit_message' | 'no_action' | 'request_tool' | 'propose_transition' | 'escalate';
  produced_at: string;
  emit_message?: EmitMessageDecisionPayloadV1;
  no_action?: NoActionDecisionPayloadV1;
  next_timer_request?: NextTimerRequestV1;
  payload_ref?: ProtectedPayloadRefV1;
}

export interface DecisionValidationEffectV1 {
  schema_version: SchemaVersionV1;
  validation_effect_id: string;
  agent_decision_id: string;
  validation_outcome: 'accepted' | 'rejected' | 'suppressed';
  effect_outcome: 'applied' | 'no_domain_effect' | 'effect_failed' | 'not_attempted';
  validated_at: string;
  rejection_reason_category?:
    | 'policy_prohibited'
    | 'capability_disabled'
    | 'payload_invalid'
    | 'state_ineligible'
    | 'budget_exhausted'
    | 'cutoff_exceeded';
  session_sequence?: PositiveInt64WireString;
  timer_validation_outcome?: 'accepted' | 'rejected' | 'omitted' | 'not_present';
  schedule_revision_id?: string;
}

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
