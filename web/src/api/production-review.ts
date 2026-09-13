import type {
  ReviewCaseReadV1,
  ReviewCriterionReadV1,
  ReviewEvidenceOpenV1,
  ReviewWorkItemV1,
} from "../contracts/v1";
import { ProductionApiError } from "./production-api";

export interface ReviewWorkListV1 {
  schema_version: "v1";
  items: ReviewWorkItemV1[];
  next_cursor?: string | null;
  has_more: boolean;
}

export interface ReviewQueryScope {
  actorId: string;
  organizationId: string;
}

export function isReviewAccessLoss(error: unknown): error is ProductionApiError {
  return error instanceof ProductionApiError
    && (error.status === 401 || error.status === 403 || error.outcomeCode === "review.denied");
}

export function createProductionReviewClient(fetchJson: <T>(path: string, init?: RequestInit) => Promise<T>) {
  return {
    listWork(cursor?: string | null, signal?: AbortSignal) {
      const params = new URLSearchParams();
      if (cursor) {
        params.set("cursor", cursor);
      }
      const query = params.toString();
      return fetchJson<ReviewWorkListV1>(`/v1/review/work${query ? `?${query}` : ""}`, { signal });
    },
    getCase(reviewCaseId: string, signal?: AbortSignal) {
      return fetchJson<ReviewCaseReadV1>(`/v1/review/cases/${encodeURIComponent(reviewCaseId)}`, { signal });
    },
    getCriterion(reviewCaseId: string, criterionId: string, signal?: AbortSignal) {
      return fetchJson<ReviewCriterionReadV1>(
        `/v1/review/cases/${encodeURIComponent(reviewCaseId)}/criteria/${encodeURIComponent(criterionId)}`,
        { signal },
      );
    },
    openEvidence(reviewCaseId: string, evidenceId: string, signal?: AbortSignal) {
      return fetchJson<ReviewEvidenceOpenV1>(
        `/v1/review/cases/${encodeURIComponent(reviewCaseId)}/evidence/${encodeURIComponent(evidenceId)}`,
        { signal },
      );
    },
  };
}

export type ProductionReviewClient = ReturnType<typeof createProductionReviewClient>;
