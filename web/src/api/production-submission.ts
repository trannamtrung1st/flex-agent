import type { MyWorkSubmissionProjection } from "../contracts/v2";

export type { MyWorkSubmissionProjection };

export function createProductionSubmissionClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  return {
    getMyWorkSubmission(enrollmentId: string) {
      return fetchJson<MyWorkSubmissionProjection>(`/v2/assessment/my-work/${enrollmentId}/submission`);
    },
    beginIntake(enrollmentId: string, idempotencyKey: string, csrfToken: string) {
      return fetchJson(`/v2/assessment/my-work/${enrollmentId}/submission/intake`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-TOKEN": csrfToken,
        },
        body: JSON.stringify({ schema_version: "v2", idempotency_key: idempotencyKey }),
      });
    },
  };
}
