import type { ReviewQueryScope } from "../../api/production-review";

export const reviewKeys = {
  all: (scope: ReviewQueryScope) => ["review", "v1", scope.organizationId, scope.actorId] as const,
  workPages: (scope: ReviewQueryScope) => [...reviewKeys.all(scope), "work", "pages"] as const,
  work: (scope: ReviewQueryScope, cursor?: string | null) =>
    [...reviewKeys.all(scope), "work", cursor ?? ""] as const,
  caseRoot: (scope: ReviewQueryScope, reviewCaseId: string) =>
    [...reviewKeys.all(scope), "cases", reviewCaseId] as const,
  case: (scope: ReviewQueryScope, reviewCaseId: string) =>
    [...reviewKeys.caseRoot(scope, reviewCaseId), "read"] as const,
  evaluation: (scope: ReviewQueryScope, reviewCaseId: string, evaluationId: string) =>
    [...reviewKeys.caseRoot(scope, reviewCaseId), "evaluations", evaluationId] as const,
  criterion: (
    scope: ReviewQueryScope,
    reviewCaseId: string,
    evaluationId: string,
    criterionId: string,
  ) => [...reviewKeys.evaluation(scope, reviewCaseId, evaluationId), "criteria", criterionId] as const,
  evidence: (
    scope: ReviewQueryScope,
    reviewCaseId: string,
    evaluationId: string,
    evidenceId: string,
  ) => [...reviewKeys.evaluation(scope, reviewCaseId, evaluationId), "evidence", evidenceId] as const,
};

export function isProcessingReviewState(state: string | undefined) {
  return state === "awaiting" || state === "queued" || state === "running";
}
