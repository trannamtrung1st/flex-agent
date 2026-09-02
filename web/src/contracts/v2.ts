export type SchemaVersionV2 = 'v2';

export type SubmissionMaterialCategoryV2 =
  | 'direct_text'
  | 'text_plain_attachment'
  | 'text_markdown_attachment';

export type IntakeStatusV2 =
  | 'receiving'
  | 'received'
  | 'validating'
  | 'cancelling'
  | 'cancelled'
  | 'rejected'
  | 'failed'
  | 'reconciling'
  | 'accepted';

export type SubmissionPermittedActionV2 =
  | 'begin_intake'
  | 'complete_item'
  | 'cancel_intake'
  | 'finalize_intake'
  | 'preview_item'
  | 'download_item'
  | 'return_to_my_work';

export interface BeginIntakeCommandV2 {
  schema_version: SchemaVersionV2;
  idempotency_key: string;
}

export interface CompleteIntakeItemCommandV2 {
  schema_version: SchemaVersionV2;
  category: SubmissionMaterialCategoryV2;
  filename?: string | null;
  declared_mime_type?: string | null;
  content: string;
  expected_revision: number;
  idempotency_key: string;
}

export interface IntakeRevisionCommandV2 {
  schema_version: SchemaVersionV2;
  expected_revision: number;
  idempotency_key: string;
}

export interface IntakeMutationOutcomeV2 {
  schema_version: SchemaVersionV2;
  succeeded: boolean;
  outcome_code: string;
  intake_id?: string | null;
  submission_id?: string | null;
  status?: IntakeStatusV2 | null;
  revision?: number | null;
  version_id?: string | null;
  version_number?: number | null;
  permitted_actions: SubmissionPermittedActionV2[];
}

export interface MaterialRequirementsV2 {
  contract_version: string;
  max_attachment_count: number;
  max_attachment_aggregate_bytes: number;
  max_direct_text_bytes: number;
  scanner_mode: 'disabled_by_approved_policy' | 'required';
  categories: Array<{
    category: SubmissionMaterialCategoryV2;
    available: boolean;
    max_bytes: number;
  }>;
}

export interface MyWorkSubmissionV2 {
  schema_version: SchemaVersionV2;
  enrollment_id: string;
  enrollment_status: string;
  intake_available: boolean;
  unavailable_reason?: string | null;
  requirements?: MaterialRequirementsV2 | null;
  active_intake?: {
    intake_id: string;
    submission_id: string;
    status: IntakeStatusV2;
    revision: number;
    created_at_utc: string;
    updated_at_utc: string;
    complete_receipt_at_utc?: string | null;
    items: Array<{
      item_id: string;
      category: string;
      filename?: string | null;
      byte_count: number;
      receipt_state?: string | null;
    }>;
    permitted_actions: SubmissionPermittedActionV2[];
  } | null;
  version_history: Array<{
    version_id: string;
    version_number: number;
    accepted_at_utc: string;
    item_count: number;
  }>;
  permitted_actions: SubmissionPermittedActionV2[];
}

export interface AcceptedVersionDetailV2 {
  schema_version: SchemaVersionV2;
  version_id: string;
  version_number: number;
  accepted_at_utc: string;
  items: Array<{
    item_id: string;
    category: SubmissionMaterialCategoryV2;
    filename?: string | null;
    byte_count: number;
    preview_authorized: boolean;
    download_authorized: boolean;
  }>;
  permitted_actions: SubmissionPermittedActionV2[];
}

export interface ProtectedItemPreviewV2 {
  schema_version: SchemaVersionV2;
  version_id: string;
  item_id: string;
  category: string;
  filename?: string | null;
  content_type: string;
  content: string;
}

export type AttemptReadinessStateV2 =
  | 'eligible'
  | 'too_early'
  | 'expired'
  | 'exhausted'
  | 'missing_accepted_material'
  | 'material_not_agent_readable'
  | 'active_conflict'
  | 'configuration_unavailable'
  | 'enrollment_unavailable'
  | 'dependency_unavailable';

export type AttemptPermittedActionV2 =
  | 'start_attempt'
  | 'continue_attempt'
  | 'return_to_my_work';

export interface AcknowledgeAttemptNoticeCommandV2 {
  schema_version: SchemaVersionV2;
  notice_id: string;
  source_version_id: string;
  outcome: 'affirmed' | 'declined' | 'withdrawn';
  idempotency_key: string;
}

export interface StartAttemptCommandV2 {
  schema_version: SchemaVersionV2;
  idempotency_key: string;
  trusted_command_digest: string;
}

export interface MyWorkAttemptReadinessV2 {
  schema_version: SchemaVersionV2;
  enrollment_id: string;
  readiness_state: AttemptReadinessStateV2;
  next_ordinal: number;
  remaining_entitlement: number;
  entitlement_source: 'baseline' | 'retry';
  baseline_attempt_limit: number;
  active_attempt_id?: string | null;
  active_session_id?: string | null;
  start_command_digest: string;
  bound_version_candidates: Array<{
    version_id: string;
    version_number: number;
    accepted_at_utc: string;
    item_count: number;
  }>;
  history: Array<{
    attempt_id: string;
    ordinal: number;
    status: 'active' | 'completed' | 'aborted';
    consumed: boolean;
    session_id?: string | null;
    started_at_utc: string;
    terminal_at_utc?: string | null;
    terminal_reason_category?: string | null;
  }>;
  required_notices: Array<{
    notice_id: string;
    notice_type: 'instructions' | 'consent' | 'data_use';
    required_outcome: 'affirmed';
    protected_content_ref: string;
    source_version_id: string;
    content_digest: string;
    source_id: string;
  }>;
  permitted_actions: AttemptPermittedActionV2[];
}

export interface AcknowledgmentMutationOutcomeV2 {
  schema_version: SchemaVersionV2;
  succeeded: boolean;
  outcome_code: string;
  record_id?: string | null;
  outcome?: string | null;
}

export interface StartAttemptOutcomeV2 {
  schema_version: SchemaVersionV2;
  succeeded: boolean;
  outcome_code: string;
  readiness_state?: AttemptReadinessStateV2 | null;
  attempt_id?: string | null;
  ordinal?: number | null;
  session_id?: string | null;
  remaining_entitlement: number;
  permitted_actions: AttemptPermittedActionV2[];
}

export type AccommodationDimensionV2 =
  | 'submission_deadline_utc'
  | 'attempt_start_not_before_utc'
  | 'attempt_start_before_utc'
  | 'per_attempt_duration_seconds';

export type AccommodationStatusV2 =
  | 'pending_approval'
  | 'granted'
  | 'rejected'
  | 'revoked'
  | 'superseded';

export type TimingEligibilityStateV2 =
  | 'too_early'
  | 'open'
  | 'submission_closed'
  | 'attempt_start_closed'
  | 'unavailable';

export type AccommodationConsequenceCodeV2 =
  | 'none'
  | 'deadline_replacement'
  | 'attempt_start_replacement'
  | 'duration_replacement'
  | 'multiple_replacements';

export interface CurrentAccommodationEffectV2 {
  accommodation_id: string;
  dimension: AccommodationDimensionV2;
  consequence_code: Exclude<AccommodationConsequenceCodeV2, 'none' | 'multiple_replacements'>;
}

export interface GrantAccommodationCommandV2 {
  schema_version: SchemaVersionV2;
  dimension: AccommodationDimensionV2;
  requested_value: string;
  reason_category: string;
  expires_at_utc?: string | null;
  fairness_exception: boolean;
  expected_revision: number;
  idempotency_key: string;
}

export interface DecideAccommodationCommandV2 {
  schema_version: SchemaVersionV2;
  approve: boolean;
  expected_revision: number;
  idempotency_key: string;
}

export interface RevokeAccommodationCommandV2 {
  schema_version: SchemaVersionV2;
  expected_revision: number;
  idempotency_key: string;
}

export interface AccommodationMutationOutcomeV2 {
  schema_version: SchemaVersionV2;
  succeeded: boolean;
  outcome_code: string;
  accommodation_id?: string | null;
  enrollment_id?: string | null;
  status?: AccommodationStatusV2 | null;
  revision?: number | null;
  permitted_actions: string[];
}

export interface TimingEffectiveWindowV2 {
  submission_starts_at_utc: string;
  submission_exclusive_end_utc: string;
  attempt_start_utc: string;
  attempt_start_exclusive_end_utc: string;
  per_attempt_duration_seconds?: number | null;
  evaluated_at_utc: string;
  eligibility_state: TimingEligibilityStateV2;
  is_authoritative: boolean;
  time_zone_id: string;
  participant_consequence_code: AccommodationConsequenceCodeV2;
}

export interface EnrollmentTimingV2 {
  schema_version: SchemaVersionV2;
  enrollment: {
    enrollment_id: string;
    status: string;
    revision: number;
    visibility: string;
    permitted_actions: string[];
  };
  baseline: {
    starts_at_utc: string;
    ends_at_utc: string;
    deadline_utc: string;
    time_zone_id: string;
    attempt_limit: number;
    per_attempt_duration_seconds?: number | null;
  };
  effective: TimingEffectiveWindowV2;
  current_accommodations: CurrentAccommodationEffectV2[];
  policy_available: boolean;
  permitted_dimensions: AccommodationDimensionV2[];
  permitted_reason_categories: string[];
  history: Array<{
    accommodation_id: string;
    dimension: AccommodationDimensionV2;
    status: AccommodationStatusV2;
    normalized_value: string;
    reason_category: string;
    fairness_exception: boolean;
    revision: number;
    created_at_utc: string;
    decided_at_utc?: string | null;
    expires_at_utc?: string | null;
  }>;
}

export interface MyWorkTimingV2 {
  schema_version: SchemaVersionV2;
  assignment: {
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
  };
  effective?: TimingEffectiveWindowV2 | null;
  participant_consequence_code: AccommodationConsequenceCodeV2;
}
