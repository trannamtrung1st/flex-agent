export type SchemaVersionV1 = "v1";

export interface ActorContextV1 {
  schema_version: SchemaVersionV1;
  actor_id: string;
  display_name: string;
  organization_id: string;
  organization_name: string;
  capabilities: string[];
  actor_stage: string;
  is_synthetic: boolean;
}

export interface PermittedActionV1 {
  action_id: string;
  label: string;
  description?: string | null;
  is_destructive: boolean;
}

export interface NavigationDestinationV1 {
  destination_id: string;
  label: string;
  route: string;
  tier: string;
  is_available: boolean;
  unavailable_reason?: string | null;
}

export interface NavigationProjectionV1 {
  schema_version: SchemaVersionV1;
  destinations: NavigationDestinationV1[];
}

export interface HomeWorkItemV1 {
  item_id: string;
  title: string;
  status_label: string;
  priority_band: string;
  route?: string | null;
  next_action_label?: string | null;
}

export interface HomeProjectionV1 {
  schema_version: SchemaVersionV1;
  greeting: string;
  work_items: HomeWorkItemV1[];
  permitted_actions: PermittedActionV1[];
}

export interface ActivitySummaryV1 {
  activity_id: string;
  title: string;
  form: string;
  type: string;
  status_label: string;
  route?: string | null;
}

export interface ActivitiesListProjectionV1 {
  schema_version: SchemaVersionV1;
  activities: ActivitySummaryV1[];
  permitted_actions: PermittedActionV1[];
}

export interface ReadinessCategoryV1 {
  category_id: string;
  label: string;
  status: string;
  is_blocking: boolean;
  detail?: string | null;
}

export interface ActivityDetailProjectionV1 {
  schema_version: SchemaVersionV1;
  activity_id: string;
  title: string;
  form: string;
  type: string;
  lifecycle_state: string;
  expected_version: number;
  readiness_categories: ReadinessCategoryV1[];
  permitted_actions: PermittedActionV1[];
  baseline_summary?: string | null;
}

export interface EnrollmentSummaryV1 {
  enrollment_id: string;
  participant_label: string;
  status_label: string;
}

export interface ParticipantChoiceV1 {
  participant_id: string;
  display_label: string;
}

export interface EnrollmentProjectionV1 {
  schema_version: SchemaVersionV1;
  activity_id: string;
  lifecycle_state: string;
  expected_version: number;
  enrollments: EnrollmentSummaryV1[];
  permitted_participants: ParticipantChoiceV1[];
  permitted_actions: PermittedActionV1[];
}

export interface SubmissionVersionV1 {
  version_id: string;
  label: string;
  status_label: string;
  content_preview?: string | null;
}

export interface AssignmentProjectionV1 {
  schema_version: SchemaVersionV1;
  enrollment_id: string;
  activity_title: string;
  task_summary: string;
  timezone: string;
  deadline?: string | null;
  attempt_status: string;
  submission_versions: SubmissionVersionV1[];
  permitted_actions: PermittedActionV1[];
  lifecycle_state: string;
}

export interface SessionTranscriptItemV1 {
  item_id: string;
  role: string;
  content: string;
  status: string;
  occurred_at?: string | null;
}

export interface SessionProjectionV1 {
  schema_version: SchemaVersionV1;
  session_id: string;
  lifecycle_state: string;
  remaining_time?: string | null;
  transcript: SessionTranscriptItemV1[];
  permitted_actions: PermittedActionV1[];
  bound_submission_summary?: string | null;
  session_version: number;
  last_sequence?: string | null;
}

export interface ReviewCaseSummaryV1 {
  case_id: string;
  title: string;
  status_label: string;
  route?: string | null;
}

export interface ReviewWorkProjectionV1 {
  schema_version: SchemaVersionV1;
  cases: ReviewCaseSummaryV1[];
  permitted_actions: PermittedActionV1[];
}

export interface EvidenceItemV1 {
  evidence_id: string;
  label: string;
  locator_summary: string;
  content_preview?: string | null;
}

export interface CriterionResultV1 {
  criterion_id: string;
  label: string;
  outcome: string;
  evidence: EvidenceItemV1[];
}

export interface ReviewCaseDetailProjectionV1 {
  schema_version: SchemaVersionV1;
  case_id: string;
  status_label: string;
  candidate_lineage: string;
  criteria: CriterionResultV1[];
  permitted_actions: PermittedActionV1[];
  human_revision_draft?: string | null;
  lifecycle_state: string;
  expected_version: number;
}

export interface ReleaseItemSummaryV1 {
  release_id: string;
  title: string;
  status_label: string;
  route?: string | null;
}

export interface ReleaseWorkProjectionV1 {
  schema_version: SchemaVersionV1;
  items: ReleaseItemSummaryV1[];
  permitted_actions: PermittedActionV1[];
}

export interface ReleaseDetailProjectionV1 {
  schema_version: SchemaVersionV1;
  release_id: string;
  status_label: string;
  result_preview: string;
  audience_policy: string;
  permitted_actions: PermittedActionV1[];
  expected_version: number;
  lifecycle_state: string;
}

export interface ResultItemV1 {
  result_id: string;
  activity_title: string;
  status_label: string;
  route?: string | null;
}

export interface ResultsProjectionV1 {
  schema_version: SchemaVersionV1;
  results: ResultItemV1[];
  permitted_actions: PermittedActionV1[];
}

export interface ResultDetailProjectionV1 {
  schema_version: SchemaVersionV1;
  result_id: string;
  status_label: string;
  content?: string | null;
  lifecycle_state: string;
  correction_note?: string | null;
}

export interface GovernanceEntryV1 {
  entry_id: string;
  action: string;
  actor_label: string;
  occurred_at: string;
  outcome: string;
}

export interface GovernanceProjectionV1 {
  schema_version: SchemaVersionV1;
  entries: GovernanceEntryV1[];
  permitted_actions: PermittedActionV1[];
  is_partial: boolean;
}

export interface PlannedTierProjectionV1 {
  schema_version: SchemaVersionV1;
  module_name: string;
  tier: string;
  message: string;
  permitted_actions: PermittedActionV1[];
}

export interface BrowserCommandEnvelopeV1 {
  schema_version: SchemaVersionV1;
  command_id: string;
  idempotency_key: string;
  command_type: string;
  resource_id?: string | null;
  expected_version?: number | null;
  payload?: Record<string, string> | null;
}

export interface BrowserCommandResultV1 {
  schema_version: SchemaVersionV1;
  outcome: string;
  correlation_id?: string | null;
  new_version?: number | null;
  lifecycle_state?: string | null;
  permitted_recovery_action?: string | null;
  safe_message?: string | null;
}
