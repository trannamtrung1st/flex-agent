export type SchemaVersionV2 = 'v2';

export type DecisionDispositionV2 = 'respond' | 'no_action';

export type AgentOutputKindV2 = 'message' | 'voice';

export type AgentOutputAudienceV2 =
  | 'participant'
  | 'reviewer'
  | 'administrator'
  | 'runtime_only';

export type AgentRequestedActionKindV2 =
  | 'next_timer_request'
  | 'request_tool'
  | 'propose_transition'
  | 'escalate';

export interface AgentOutputLocalReferenceV2 {
  relation: string;
  local_ref: string;
}

export interface AgentOutputRecommendationV2 {
  kind: AgentOutputKindV2;
  local_ref: string;
  communication_purpose?: string;
  turn_id?: string;
  response_slot_id?: string;
  agent_output_id?: string;
  audience?: AgentOutputAudienceV2;
  references?: AgentOutputLocalReferenceV2[];
  payload_ref?: {
    protected_ref: string;
    content_digest: string;
  };
}

export interface AgentRequestedActionV2 {
  kind: AgentRequestedActionKindV2;
  local_ref: string;
  relative_delay?: string;
  expected_schedule_revision?: string;
}

export interface AgentDecisionEnvelopeV2 {
  schema_version: SchemaVersionV2;
  agent_decision_id: string;
  agent_invocation_id: string;
  produced_at: string;
  disposition: DecisionDispositionV2;
  outputs: AgentOutputRecommendationV2[];
  requested_actions: AgentRequestedActionV2[];
  no_action?: {
    reason_category: 'intentional_silence' | 'workflow_complete' | 'awaiting_input';
  };
  payload_ref?: {
    protected_ref: string;
    content_digest: string;
  };
}
