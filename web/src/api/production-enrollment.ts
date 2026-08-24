import type { EnrollmentMutationOutcomeV1, MyWorkAssignmentV1 } from "../contracts/v1";
import type {
  AccommodationMutationOutcomeV2,
  EnrollmentTimingV2,
  GrantAccommodationCommandV2,
  MyWorkTimingV2,
} from "../contracts/v2";
import { ProductionApiError } from "./production-api";

export type { EnrollmentMutationOutcomeV1, MyWorkAssignmentV1 };
export type {
  AccommodationMutationOutcomeV2,
  EnrollmentTimingV2,
  GrantAccommodationCommandV2,
  MyWorkTimingV2,
};

export type AccommodationMutationV2 = AccommodationMutationOutcomeV2;

export interface EnrollmentCandidateV1 {
  actor_id: string;
  display_label: string;
}

export interface EnrollmentSummaryV1 {
  enrollment_id: string;
  participant_actor_id: string;
  display_label: string;
  status: string;
  revision: number;
  assigned_at: string;
  updated_at: string;
  visibility: string;
  permitted_actions: string[];
}

export interface EnrollmentPageV1 {
  schema_version: string;
  items: EnrollmentSummaryV1[];
  next_cursor?: string | null;
  has_more: boolean;
}

export interface CandidatePageV1 {
  schema_version: string;
  items: EnrollmentCandidateV1[];
  next_cursor?: string | null;
  has_more: boolean;
}

export interface EnrollmentDetailV1 {
  schema_version: string;
  enrollment: EnrollmentSummaryV1;
  history: Array<{
    sequence: number;
    prior_status: string;
    new_status: string;
    reason_code: string;
    occurred_at: string;
  }>;
}

export type AssignmentSummaryV1 = MyWorkAssignmentV1;

export interface MyWorkPageV1 {
  schema_version: string;
  items: AssignmentSummaryV1[];
  next_cursor?: string | null;
  has_more: boolean;
}

export type EnrollmentMutationV1 = EnrollmentMutationOutcomeV1;

export function createEnrollmentIdempotencyKey(): string {
  return `enr-${crypto.randomUUID()}`;
}

export function createProductionEnrollmentClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  return {
    listCandidates(activityId: string, cohortId: string) {
      return fetchJson<CandidatePageV1>(
        `/v1/assessment/activities/${activityId}/cohorts/${cohortId}/participant-options`,
      );
    },
    listEnrollments(activityId: string, cohortId: string) {
      return fetchJson<EnrollmentPageV1>(
        `/v1/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments`,
      );
    },
    getEnrollment(activityId: string, cohortId: string, enrollmentId: string) {
      return fetchJson<EnrollmentDetailV1>(
        `/v1/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollmentId}`,
      );
    },
    assign(
      activityId: string,
      cohortId: string,
      participantActorId: string,
      idempotencyKey: string,
    ) {
      return readMutation(
        fetchJson,
        `/v1/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            schema_version: "v1",
            participant_actor_id: participantActorId,
            idempotency_key: idempotencyKey,
          }),
        },
      );
    },
    mutate(
      activityId: string,
      cohortId: string,
      enrollmentId: string,
      operation: "suspend" | "restore" | "close" | "revoke",
      reasonCode: string,
      expectedRevision: number,
      idempotencyKey: string,
    ) {
      return readMutation(
        fetchJson,
        `/v1/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollmentId}/${operation}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            schema_version: "v1",
            reason_code: reasonCode,
            expected_revision: expectedRevision,
            idempotency_key: idempotencyKey,
          }),
        },
      );
    },
    listMyWork() {
      return fetchJson<MyWorkPageV1>("/v1/assessment/my-work");
    },
    getMyWork(enrollmentId: string) {
      return fetchJson<{ schema_version: string; assignment: AssignmentSummaryV1 }>(
        `/v1/assessment/my-work/${enrollmentId}`,
      );
    },
    getEnrollmentTiming(activityId: string, cohortId: string, enrollmentId: string) {
      return fetchJson<EnrollmentTimingV2>(
        `/v2/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollmentId}/timing`,
      );
    },
    getMyWorkTiming(enrollmentId: string) {
      return fetchJson<MyWorkTimingV2>(`/v2/assessment/my-work/${enrollmentId}/timing`);
    },
    grantAccommodation(
      activityId: string,
      cohortId: string,
      enrollmentId: string,
      body: Omit<GrantAccommodationCommandV2, "schema_version">,
    ) {
      return readAccommodationMutation(
        fetchJson,
        `/v2/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollmentId}/accommodations`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ schema_version: "v2", ...body }),
        },
      );
    },
    decideAccommodation(
      activityId: string,
      cohortId: string,
      enrollmentId: string,
      accommodationId: string,
      approve: boolean,
      expectedRevision: number,
      idempotencyKey: string,
    ) {
      return readAccommodationMutation(
        fetchJson,
        `/v2/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollmentId}/accommodations/${accommodationId}/decide`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            schema_version: "v2",
            approve,
            expected_revision: expectedRevision,
            idempotency_key: idempotencyKey,
          }),
        },
      );
    },
    revokeAccommodation(
      activityId: string,
      cohortId: string,
      enrollmentId: string,
      accommodationId: string,
      expectedRevision: number,
      idempotencyKey: string,
    ) {
      return readAccommodationMutation(
        fetchJson,
        `/v2/assessment/activities/${activityId}/cohorts/${cohortId}/enrollments/${enrollmentId}/accommodations/${accommodationId}/revoke`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            schema_version: "v2",
            expected_revision: expectedRevision,
            idempotency_key: idempotencyKey,
          }),
        },
      );
    },
  };
}

export const EnrollmentRateLimitedCopy = "Too many requests. Wait a moment, then try again.";

export function isEnrollmentAccessLoss(error: unknown): error is ProductionApiError {
  return error instanceof ProductionApiError && (error.status === 401 || error.status === 403);
}

export function isEnrollmentRateLimited(error: unknown): error is ProductionApiError {
  return error instanceof ProductionApiError
    && (error.status === 429 || error.outcomeCode === "enrollment.rate_limited");
}

export function enrollmentFailureCopy(error: unknown, fallback: string): string {
  return isEnrollmentRateLimited(error) ? EnrollmentRateLimitedCopy : fallback;
}

export function enrollmentOutcomeCopy(outcomeCode: string | undefined, fallback: string): string {
  return outcomeCode === "enrollment.rate_limited" ? EnrollmentRateLimitedCopy : fallback;
}

async function readMutation(
  fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>,
  path: string,
  init: RequestInit,
): Promise<EnrollmentMutationV1> {
  try {
    return await fetchJson<EnrollmentMutationV1>(path, init);
  } catch (error) {
    if (isEnrollmentAccessLoss(error)) {
      throw error;
    }

    if (error instanceof ProductionApiError && error.outcomeCode) {
      return {
        schema_version: "v1",
        succeeded: false,
        outcome_code: error.outcomeCode,
        permitted_actions: [],
      };
    }

    throw error;
  }
}

async function readAccommodationMutation(
  fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>,
  path: string,
  init: RequestInit,
): Promise<AccommodationMutationV2> {
  try {
    return await fetchJson<AccommodationMutationV2>(path, init);
  } catch (error) {
    if (isEnrollmentAccessLoss(error)) {
      throw error;
    }

    if (error instanceof ProductionApiError && error.outcomeCode) {
      return {
        schema_version: "v2",
        succeeded: false,
        outcome_code: error.outcomeCode,
        permitted_actions: [],
      };
    }

    throw error;
  }
}
