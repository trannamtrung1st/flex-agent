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

export interface SessionAgentIdentityV1 {
  display_name: string;
}

export interface SessionTimingProjectionV1 {
  policy: 'disabled' | 'active_duration' | 'absolute_deadline' | 'unavailable';
  remaining_seconds?: number | null;
  warning_code?: 'none' | 'approaching' | 'imminent' | null;
  pause_started_at?: string;
  budget_seconds?: number | null;
}

export interface SessionBoundSubmissionSummaryV1 {
  summary: string;
  accepted_version_count: number;
}

export interface SessionSnapshotTranscriptItemV1 {
  item_id: string;
  author: 'participant' | 'agent';
  status: 'accepted' | 'streaming' | 'complete' | 'incomplete' | 'cancelled' | 'unavailable';
  sequence_start: PositiveInt64WireString;
  sequence_end: PositiveInt64WireString;
  content?: string | null;
  occurred_at?: string;
  turn_id?: string;
}

export interface SessionTranscriptPageV1 {
  items: SessionSnapshotTranscriptItemV1[];
  older_available: boolean;
  oldest_sequence?: PositiveInt64WireString;
  newest_sequence?: PositiveInt64WireString;
}

export interface SessionActivityProjectionV1 {
  work_state: 'idle' | 'queued' | 'working' | 'no_action' | 'failed';
  turn_id?: string;
  resolution_category?: 'message_stream' | 'no_action' | 'suppressed_failure' | 'execution_failure';
}

export interface SessionCommandReconciliationV1 {
  last_outcome_code: string;
  last_command_id?: string;
}

export type SessionProjectionKindV1 = 'participant' | 'administrator' | 'historical';

export type SessionLifecycleStateV1 =
  | 'ready'
  | 'active'
  | 'paused'
  | 'completing'
  | 'completed'
  | 'terminated'
  | 'aborted';

export type SessionPermittedActionV1 =
  | 'send_message'
  | 'complete_session'
  | 'reconcile'
  | 'pause_session'
  | 'resume_session'
  | 'terminate_session'
  | 'view_transcript'
  | 'return_to_my_work';

export type SessionRecoveryCategoryV1 =
  | 'none'
  | 'reconcile_snapshot'
  | 'retry_later'
  | 'sign_in'
  | 'unavailable';

export interface SessionSnapshotV1 {
  schema_version: SchemaVersionV1;
  projection_kind: SessionProjectionKindV1;
  session_id: string;
  lifecycle_state: SessionLifecycleStateV1;
  session_version: number;
  last_confirmed_sequence: NonnegativeInt64WireString;
  authoritative_observed_at: string;
  permitted_actions: SessionPermittedActionV1[];
  recovery_category: SessionRecoveryCategoryV1;
  cutoff_sequence?: PositiveInt64WireString;
  agent?: SessionAgentIdentityV1;
  timing?: SessionTimingProjectionV1;
  bound_submission?: SessionBoundSubmissionSummaryV1;
  transcript?: SessionTranscriptPageV1;
  activity?: SessionActivityProjectionV1;
  command_reconciliation?: SessionCommandReconciliationV1;
}

export interface SessionCommandOutcomeV1 {
  schema_version: SchemaVersionV1;
  succeeded: boolean;
  outcome_category: 'accepted' | 'duplicate' | 'rejected' | 'conflict' | 'uncertain';
  outcome_code: string;
  command_id: string;
  command_type:
    | 'session.message.send.v1'
    | 'session.pause.v1'
    | 'session.resume.v1'
    | 'session.complete.v1'
    | 'session.terminate.v1'
    | 'session.reconcile.v1';
  session_id: string;
  permitted_recovery_action: 'none' | 'reconcile_snapshot' | 'retry_same_command' | 'wait' | 'return';
  permitted_actions: SessionPermittedActionV1[];
  session_version?: number | null;
  session_sequence?: NonnegativeInt64WireString;
  accepted_message_id?: string;
}

export interface SessionHostedEventPayloadV1 {
  summary: string;
  lifecycle_state?: SessionLifecycleStateV1;
  remaining_seconds?: number | null;
  warning_code?: 'none' | 'approaching' | 'imminent';
  message_id?: string;
  turn_id?: string;
  work_state?: 'idle' | 'queued' | 'working' | 'no_action' | 'failed';
  resolution_category?: 'message_stream' | 'no_action' | 'suppressed_failure' | 'execution_failure';
  agent_message_id?: string;
  fragment_sequence?: number;
  text_delta?: string;
  assembled_content_digest?: string;
  fragment_count?: number;
  cutoff_sequence?: PositiveInt64WireString;
  access_state?: 'authorized' | 'revalidate' | 'revoked';
  recovery_category?: SessionRecoveryCategoryV1;
  item_status?: SessionSnapshotTranscriptItemV1['status'];
}

export type SessionHostedEventTypeV1 =
  | 'session.hosted.lifecycle.changed.v1'
  | 'session.hosted.timing.updated.v1'
  | 'session.hosted.warning.issued.v1'
  | 'session.hosted.message.accepted.v1'
  | 'session.hosted.agent.work.v1'
  | 'session.hosted.agent.no_action.v1'
  | 'session.hosted.agent.fragment.v1'
  | 'session.hosted.agent.complete.v1'
  | 'session.hosted.terminal.v1'
  | 'session.hosted.access.changed.v1'
  | 'session.hosted.reconcile.required.v1';

export interface SessionHostedEventEnvelopeV1 {
  schema_version: SchemaVersionV1;
  event_type: SessionHostedEventTypeV1;
  session_id: string;
  session_sequence: PositiveInt64WireString;
  session_version: number;
  occurred_at: string;
  payload: SessionHostedEventPayloadV1;
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
