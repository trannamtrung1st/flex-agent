import type {
  AcceptedVersionDetailV2,
  CompleteIntakeItemCommandV2,
  IntakeMutationOutcomeV2,
  IntakeRevisionCommandV2,
  MyWorkSubmissionV2,
  ProtectedItemPreviewV2,
} from "../contracts/v2";

export type { AcceptedVersionDetailV2, IntakeMutationOutcomeV2, MyWorkSubmissionV2, ProtectedItemPreviewV2 };

export function createSubmissionIdempotencyKey(): string {
  return `sub-${crypto.randomUUID()}`;
}

export function createProductionSubmissionClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  return {
    getMyWorkSubmission(enrollmentId: string) {
      return fetchJson<MyWorkSubmissionV2>(`/v2/assessment/my-work/${enrollmentId}/submission`);
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
    default:
      return "The submission could not be accepted. No earlier version was changed.";
  }
}
