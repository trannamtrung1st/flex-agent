export type SchemaVersionV1 = 'v1';

export interface SessionLocatorV1 {
  session_id: string;
}

export interface MessageSendPayloadV1 {
  message_text: string;
}

export interface SessionCommandEnvelopeV1 {
  schema_version: SchemaVersionV1;
  command_type: string;
  command_id: string;
  idempotency_key: string;
  session_locator: SessionLocatorV1;
  expected_session_version: number;
  client_last_seen_sequence?: number;
  payload: MessageSendPayloadV1 | Record<string, never>;
}

export interface SessionStateEventPayloadV1 {
  summary: string;
  turn_id?: string;
}

export interface SessionStateEventEnvelopeV1 {
  schema_version: SchemaVersionV1;
  event_type: string;
  session_id: string;
  session_sequence: number;
  session_version: number;
  occurred_at: string;
  correlation_id?: string;
  payload: SessionStateEventPayloadV1;
}

export interface SessionOwnershipRefV1 {
  organization_id: string;
  activity_id: string;
  participant_id: string;
  attempt_id: string;
  session_id: string;
}

export interface ConfigurationRefV1 {
  configuration_id: string;
  configuration_digest: string;
}

export interface ManifestRuntimeRecordV1 {
  sequence: number;
  record_type: string;
  service_actor: string;
  occurred_at: string;
  payload_ref: {
    protected_ref: string;
    content_digest: string;
  };
}

export interface ResolvedExecutionManifestV1 {
  schema_version: SchemaVersionV1;
  manifest_id: string;
  ownership: SessionOwnershipRefV1;
  configuration_ref: ConfigurationRefV1;
  runtime_records: ManifestRuntimeRecordV1[];
  terminal_state: string;
  terminal_seal?: {
    procedure_id: 'manifest-jcs-sha256-v1';
    seal_digest: string;
  };
}

export interface EvidenceLocatorV1 {
  locator_schema: 'evidence-locator.v1';
  source_type: string;
  source_ref: {
    source_id: string;
    source_version: string;
    terminal_cutoff_sequence?: number;
  };
  ownership_ref: {
    organization_id: string;
    activity_id: string;
    participant_id: string;
    attempt_id: string;
    session_id: string;
    evaluation_id: string;
  };
  location: Record<string, unknown>;
  precision: 'exact_range' | 'stable_segment' | 'whole_item';
  integrity: {
    source_digest: string;
    adapter_version: string;
    verification_state: 'verified' | 'degraded' | 'failed';
  };
  created_by: {
    service_id: string;
    invocation_id: string;
  };
}

export interface AuditEventV1 {
  event_schema: 'audit-event.v1';
  event_id: string;
  actor: {
    actor_type: 'human' | 'service' | 'system';
    actor_id: string;
  };
  organization_id: string;
  action: string;
  resource_ref: {
    resource_type: string;
    resource_id: string;
  };
  outcome: 'succeeded' | 'denied' | 'failed';
  reason_code?: string;
  occurred_at: string;
  correlation_id: string;
  durability_class?: 'required_durable' | 'bufferable';
}

export interface SafeErrorResponseV1 {
  schema_version: SchemaVersionV1;
  outcome: string;
  correlation_id: string;
  permitted_recovery_action: 'retry' | 'reconcile' | 'contact_administrator' | 'none';
  session_version?: number;
  session_sequence?: number;
}

export interface SseSessionEventPayloadV1 {
  summary: string;
  fragment_sequence?: number;
  agent_message_id?: string;
  text_delta?: string;
}

export interface SseSessionEventV1 {
  schema_version: SchemaVersionV1;
  event_type: string;
  session_id: string;
  session_sequence: number;
  occurred_at: string;
  payload: SseSessionEventPayloadV1;
}
