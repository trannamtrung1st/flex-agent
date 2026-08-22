import type { EnrollmentMutationOutcomeV1, MyWorkAssignmentV1 } from "../contracts/v1";
import { ProductionApiError } from "./production-api";

export type { EnrollmentMutationOutcomeV1, MyWorkAssignmentV1 };

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
  };
}

export function isEnrollmentAccessLoss(error: unknown): error is ProductionApiError {
  return error instanceof ProductionApiError && (error.status === 401 || error.status === 403);
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
