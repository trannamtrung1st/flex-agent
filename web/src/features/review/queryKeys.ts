import type { ReviewQueryScope } from "../../api/production-review";

export const reviewKeys = {
  all: (scope: ReviewQueryScope) => ["review", "v1", scope.organizationId, scope.actorId] as const,
  work: (scope: ReviewQueryScope, cursor?: string | null) =>
    [...reviewKeys.all(scope), "work", cursor ?? ""] as const,
  case: (scope: ReviewQueryScope, reviewCaseId: string) =>
    [...reviewKeys.all(scope), "cases", reviewCaseId] as const,
  criterion: (scope: ReviewQueryScope, reviewCaseId: string, criterionId: string) =>
    [...reviewKeys.case(scope, reviewCaseId), "criteria", criterionId] as const,
  evidence: (scope: ReviewQueryScope, reviewCaseId: string, evidenceId: string) =>
    [...reviewKeys.case(scope, reviewCaseId), "evidence", evidenceId] as const,
};

export function isProcessingReviewState(state: string | undefined) {
  return state === "awaiting" || state === "queued" || state === "running";
}
