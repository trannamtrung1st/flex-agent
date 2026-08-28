export type SchemaVersionV1 = 'v1';

/** Decimal string wire encoding for canonical positive int64 sequence and cursor fields. */
export type PositiveInt64WireString = string;

/** Decimal string wire encoding for canonical nonnegative int64 fields (includes zero). */
export type NonnegativeInt64WireString = string;

export interface SessionLocatorV1 {
  session_id: string;
}

export interface MessageSendPayloadV1 {
  message_text: string;
}

export interface TerminateCommandPayloadV1 {
  reason_code: string;
}

export type EmptyCommandPayloadV1 = Record<string, never>;

interface SessionCommandEnvelopeCoreV1 {
  schema_version: SchemaVersionV1;
  command_id: string;
  idempotency_key: string;
  session_locator: SessionLocatorV1;
  expected_session_version: number;
  client_last_seen_sequence?: PositiveInt64WireString;
}

export interface SessionMessageSendCommandV1 extends SessionCommandEnvelopeCoreV1 {
  command_type: 'session.message.send.v1';
  payload: MessageSendPayloadV1;
}

export interface SessionPauseCommandV1 extends SessionCommandEnvelopeCoreV1 {
  command_type: 'session.pause.v1';
  payload: EmptyCommandPayloadV1;
}

export interface SessionResumeCommandV1 extends SessionCommandEnvelopeCoreV1 {
  command_type: 'session.resume.v1';
  payload: EmptyCommandPayloadV1;
}

export interface SessionCompleteCommandV1 extends SessionCommandEnvelopeCoreV1 {
  command_type: 'session.complete.v1';
  payload: EmptyCommandPayloadV1;
}

export interface SessionTerminateCommandV1 extends SessionCommandEnvelopeCoreV1 {
  command_type: 'session.terminate.v1';
  payload: TerminateCommandPayloadV1;
}

export interface SessionReconcileCommandV1 extends SessionCommandEnvelopeCoreV1 {
  command_type: 'session.reconcile.v1';
  client_last_seen_sequence: PositiveInt64WireString;
  payload: EmptyCommandPayloadV1;
}

export type SessionCommandEnvelopeV1 =
  | SessionMessageSendCommandV1
  | SessionPauseCommandV1
  | SessionResumeCommandV1
  | SessionCompleteCommandV1
  | SessionTerminateCommandV1
  | SessionReconcileCommandV1;

export interface SessionStateEventPayloadV1 {
  summary: string;
  turn_id?: string;
}

export interface SessionStateEventEnvelopeV1 {
  schema_version: SchemaVersionV1;
  event_type: string;
  session_id: string;
  session_sequence: PositiveInt64WireString;
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
  sequence: PositiveInt64WireString;
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
    procedure_id: 'manifest-jcs-sha256-v1' | 'manifest-jcs-sha256-v2';
    seal_digest: string;
  };
}

export interface WholeItemLocationV1 {
  location_type: 'whole_item';
  item_id: string;
}

export interface LineRangeLocationV1 {
  location_type: 'line_range';
  item_id: string;
  start_line_inclusive: number;
  end_line_inclusive: number;
  line_split_procedure_version: string;
}

export interface Utf8ByteRangeLocationV1 {
  location_type: 'utf8_byte_range';
  item_id: string;
  start_inclusive: number;
  end_exclusive: number;
  excerpt_digest: string;
}

export interface JsonPointerLocationV1 {
  location_type: 'json_pointer';
  json_pointer: string;
}

export type EvidenceLocationV1 =
  | WholeItemLocationV1
  | LineRangeLocationV1
  | Utf8ByteRangeLocationV1
  | JsonPointerLocationV1;

export interface EvidenceLocatorV1 {
  locator_schema: 'evidence-locator.v1';
  source_type: string;
  source_ref: {
    source_id: string;
    source_version: string;
    terminal_cutoff_sequence?: PositiveInt64WireString;
  };
  ownership_ref: {
    organization_id: string;
    activity_id: string;
    participant_id: string;
    attempt_id: string;
    session_id: string;
    evaluation_id: string;
  };
  location: EvidenceLocationV1;
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
  session_sequence?: NonnegativeInt64WireString;
}

export interface SseSessionEventPayloadV1 {
  summary: string;
  fragment_sequence?: number;
  agent_message_id?: string;
  text_delta?: string;
  turn_id?: string;
  work_state?: 'queued' | 'working' | 'resolved';
  resolution_category?: 'message_stream' | 'no_action' | 'suppressed_failure' | 'execution_failure';
  show_persistent_turn_status?: boolean;
  assembled_content_digest?: string;
  fragment_count?: number;
}

export interface SseSessionEventV1 {
  schema_version: SchemaVersionV1;
  event_type:
    | 'session.agent.fragment.v1'
    | 'session.agent.complete.v1'
    | 'session.agent.work.v1'
    | 'session.state.changed.v1'
    | 'session.terminal.v1';
  session_id: string;
  session_sequence: PositiveInt64WireString;
  occurred_at: string;
  payload: SseSessionEventPayloadV1;
}

export interface EnrollmentAssignCommandV1 {
  schema_version: SchemaVersionV1;
  participant_actor_id: string;
  idempotency_key: string;
}

export interface EnrollmentLifecycleCommandV1 {
  schema_version: SchemaVersionV1;
  reason_code: 'temporary_restriction' | 'restriction_removed' | 'activity_or_enrollment_end' | 'access_revoked';
  expected_revision: number;
  idempotency_key: string;
}

export interface EnrollmentMutationOutcomeV1 {
  schema_version: SchemaVersionV1;
  succeeded: boolean;
  outcome_code: string;
  enrollment_id?: string | null;
  status?: string | null;
  revision?: number | null;
  visibility?: string | null;
  permitted_actions: string[];
}

export interface MyWorkAssignmentV1 {
  enrollment_id: string;
  status: string;
  visibility: string;
  activity_title?: string | null;
  task_title?: string | null;
  time_zone_id?: string | null;
  starts_at_utc?: string | null;
  ends_at_utc?: string | null;
  deadline_utc?: string | null;
  summary_available: boolean;
  permitted_actions: string[];
}
