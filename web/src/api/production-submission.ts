import type {
  AcceptedVersionDetailV2,
  CompleteIntakeItemCommandV2,
  IntakeMutationOutcomeV2,
  IntakeRevisionCommandV2,
  MyWorkAttemptReadinessV2,
  MyWorkSubmissionV2,
  ProtectedItemPreviewV2,
} from "../contracts/v2";
import { ProductionApiError } from "./production-api";
import { isEnrollmentAccessLoss } from "./production-enrollment";

export type {
  AcceptedVersionDetailV2,
  IntakeMutationOutcomeV2,
  MyWorkAttemptReadinessV2,
  MyWorkSubmissionV2,
  ProtectedItemPreviewV2,
};

export function createSubmissionIdempotencyKey(): string {
  return `sub-${crypto.randomUUID()}`;
}

export function createProductionSubmissionClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  return {
    getMyWorkSubmission(enrollmentId: string) {
      return fetchJson<MyWorkSubmissionV2>(`/v2/assessment/my-work/${enrollmentId}/submission`);
    },
    getIntake(enrollmentId: string, intakeId: string) {
      return fetchJson<IntakeMutationOutcomeV2>(
        `/v2/assessment/my-work/${enrollmentId}/submission/intake/${intakeId}`,
      );
    },
    getAcceptedVersion(enrollmentId: string, versionId: string) {
      return fetchJson<AcceptedVersionDetailV2>(
        `/v2/assessment/my-work/${enrollmentId}/submission/versions/${versionId}`,
      );
    },
    getItemPreview(enrollmentId: string, versionId: string, itemId: string) {
      return fetchJson<ProtectedItemPreviewV2>(
        `/v2/assessment/my-work/${enrollmentId}/submission/versions/${versionId}/items/${itemId}/preview`,
      );
    },
    downloadItemUrl(enrollmentId: string, versionId: string, itemId: string) {
      return `/v2/assessment/my-work/${enrollmentId}/submission/versions/${versionId}/items/${itemId}/download`;
    },
    beginIntake(enrollmentId: string, idempotencyKey: string) {
      return fetchJson<IntakeMutationOutcomeV2>(`/v2/assessment/my-work/${enrollmentId}/submission/intake`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ schema_version: "v2", idempotency_key: idempotencyKey }),
      });
    },
    completeItem(enrollmentId: string, intakeId: string, command: CompleteIntakeItemCommandV2) {
      return fetchJson<IntakeMutationOutcomeV2>(
        `/v2/assessment/my-work/${enrollmentId}/submission/intake/${intakeId}/items`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(command),
        },
      );
    },
    cancelIntake(enrollmentId: string, intakeId: string, command: IntakeRevisionCommandV2) {
      return fetchJson<IntakeMutationOutcomeV2>(
        `/v2/assessment/my-work/${enrollmentId}/submission/intake/${intakeId}/cancel`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(command),
        },
      );
    },
    finalizeIntake(enrollmentId: string, intakeId: string, command: IntakeRevisionCommandV2) {
      return fetchJson<IntakeMutationOutcomeV2>(
        `/v2/assessment/my-work/${enrollmentId}/submission/intake/${intakeId}/finalize`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(command),
        },
      );
    },
    getAttemptReadiness(enrollmentId: string) {
      return fetchJson<MyWorkAttemptReadinessV2>(`/v2/assessment/my-work/${enrollmentId}/attempt`);
    },
    acknowledgeNotice(
      enrollmentId: string,
      noticeId: string,
      sourceVersionId: string,
      outcome: "affirmed" | "declined" | "withdrawn",
      idempotencyKey: string,
    ) {
      return readAttemptMutation(fetchJson, `/v2/assessment/my-work/${enrollmentId}/attempt/acknowledgments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          schema_version: "v2",
          notice_id: noticeId,
          source_version_id: sourceVersionId,
          outcome,
          idempotency_key: idempotencyKey,
        }),
      });
    },
    startAttempt(enrollmentId: string, idempotencyKey: string, trustedCommandDigest: string) {
      return readAttemptMutation(fetchJson, `/v2/assessment/my-work/${enrollmentId}/attempt/start`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          schema_version: "v2",
          idempotency_key: idempotencyKey,
          trusted_command_digest: trustedCommandDigest,
        }),
      });
    },
    reconcileAttempt(enrollmentId: string, idempotencyKey: string, trustedCommandDigest: string) {
      return readAttemptMutation(fetchJson, `/v2/assessment/my-work/${enrollmentId}/attempt/reconcile`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          schema_version: "v2",
          idempotency_key: idempotencyKey,
          trusted_command_digest: trustedCommandDigest,
        }),
      });
    },
  };
}

export function submissionFailureCopy(outcomeCode: string | undefined): string {
  switch (outcomeCode) {
    case "invalid_encoding":
      return "The material is not valid UTF-8 text.";
    case "oversized":
      return "The material exceeds the allowed size.";
    case "too_many_items":
      return "Too many attachments are included.";
    case "aggregate_oversized":
      return "The combined attachment size exceeds the allowed total.";
    case "invalid_category":
      return "That file type is not permitted. Use UTF-8 .txt or .md files.";
    case "cutoff_passed":
      return "The submission cutoff has passed. This version was not accepted.";
    case "stale_revision":
      return "This intake changed. Refresh the assignment and try again.";
    case "policy_unavailable":
      return "Submission requirements are currently unavailable.";
    case "enrollment_not_active":
      return "This assignment is not active for new submission versions.";
    case "attempt.denied":
      return "This Attempt is not available.";
    case "attempt.ineligible":
      return "This assignment is not ready to start an Attempt.";
    case "attempt.acknowledgment_invalid":
      return "Required acknowledgments were not recorded. No Attempt started.";
    case "attempt.idempotency_conflict":
      return "This start request does not match the recorded command. No additional Attempt started.";
    case "attempt.active_conflict":
      return "An Attempt is already in progress for this assignment.";
    case "attempt.unavailable":
    case "attempt.audit_unavailable":
      return "Attempt start is currently unavailable. Entitlement was not consumed.";
    default:
      return "The submission could not be accepted. No earlier version was changed.";
  }
}

async function readAttemptMutation<T extends { succeeded: boolean; outcome_code: string }>(
  fetchJson: <TResponse>(path: string, init?: RequestInit) => Promise<TResponse>,
  path: string,
  init: RequestInit,
): Promise<T> {
  try {
    return await fetchJson<T>(path, init);
  } catch (error) {
    if (isEnrollmentAccessLoss(error)) {
      throw error;
    }

    if (error instanceof ProductionApiError && error.outcomeCode && error.status < 500) {
      return {
        succeeded: false,
        outcome_code: error.outcomeCode,
      } as unknown as T;
    }

    throw error;
  }
}
