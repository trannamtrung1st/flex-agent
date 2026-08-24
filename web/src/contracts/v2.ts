export type SchemaVersionV2 = 'v2';

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
  current_accommodations: CurrentAccommodationEffectV2[];
  participant_consequence_code: AccommodationConsequenceCodeV2;
}
