import { useQuery } from "@tanstack/react-query";
import type { ProductionReviewClient, ReviewQueryScope } from "../../api/production-review";
import { isProcessingReviewState, reviewKeys } from "./queryKeys";

const PROCESSING_POLL_MS = 2000;

export function useReviewWorkQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  cursor?: string | null,
) {
  return useQuery({
    queryKey: reviewKeys.work(scope ?? { actorId: "", organizationId: "" }, cursor),
    queryFn: ({ signal }) => client.listWork(cursor, signal),
    enabled: Boolean(scope?.actorId && scope.organizationId),
    refetchInterval: (query) => {
      const items = query.state.data?.items ?? [];
      return items.some((item) => isProcessingReviewState(item.evaluation_processing_state))
        ? PROCESSING_POLL_MS
        : false;
    },
  });
}

export function useReviewCaseQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  reviewCaseId: string | undefined,
) {
  return useQuery({
    queryKey: reviewKeys.case(scope ?? { actorId: "", organizationId: "" }, reviewCaseId ?? ""),
    queryFn: ({ signal }) => client.getCase(reviewCaseId!, signal),
    enabled: Boolean(scope?.actorId && scope.organizationId && reviewCaseId),
    refetchInterval: (query) =>
      isProcessingReviewState(query.state.data?.evaluation_processing_state) ? PROCESSING_POLL_MS : false,
  });
}

export function useReviewCriterionQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  reviewCaseId: string | undefined,
  criterionId: string | undefined,
  enabled: boolean,
) {
  return useQuery({
    queryKey: reviewKeys.criterion(
      scope ?? { actorId: "", organizationId: "" },
      reviewCaseId ?? "",
      criterionId ?? "",
    ),
    queryFn: ({ signal }) => client.getCriterion(reviewCaseId!, criterionId!, signal),
    enabled: Boolean(enabled && scope?.actorId && scope.organizationId && reviewCaseId && criterionId),
  });
}

export function useReviewEvidenceQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  reviewCaseId: string | undefined,
  evidenceId: string | undefined,
  enabled: boolean,
) {
  return useQuery({
    queryKey: reviewKeys.evidence(
      scope ?? { actorId: "", organizationId: "" },
      reviewCaseId ?? "",
      evidenceId ?? "",
    ),
    queryFn: ({ signal }) => client.openEvidence(reviewCaseId!, evidenceId!, signal),
    enabled: Boolean(enabled && scope?.actorId && scope.organizationId && reviewCaseId && evidenceId),
  });
}
