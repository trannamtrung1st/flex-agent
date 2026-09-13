import { useEffect, useRef } from "react";
import { useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { ProductionApiError } from "../../api/production-api";
import type { ProductionReviewClient, ReviewQueryScope } from "../../api/production-review";
import { isReviewAccessLoss } from "../../api/production-review";
import { purgeReviewEvaluationCache, purgeReviewProtectedCache } from "./queryCache";
import { isProcessingReviewState, reviewKeys } from "./queryKeys";

const PROCESSING_POLL_MS = 2000;

async function runReviewQuery<T>(
  queryClient: QueryClient,
  scope: ReviewQueryScope,
  target: { reviewCaseId?: string },
  query: () => Promise<T>,
): Promise<T> {
  try {
    return await query();
  } catch (error) {
    if (isReviewAccessLoss(error)) {
      await purgeReviewProtectedCache(queryClient, scope, target.reviewCaseId);
    }
    throw error;
  }
}

function assertSelectedEvaluationIdentity(
  result: { evaluation_id: string },
  expectedEvaluationId: string,
) {
  if (result.evaluation_id !== expectedEvaluationId) {
    throw new ProductionApiError(
      409,
      "Evaluation identity changed",
      "review.stale_evaluation",
    );
  }
}

export function usePruneStaleReviewEvaluationCache(
  scope: ReviewQueryScope | null,
  reviewCaseId: string | undefined,
  evaluationId: string | undefined,
) {
  const queryClient = useQueryClient();
  const previousEvaluationIdRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    if (!scope?.actorId || !scope.organizationId || !reviewCaseId || !evaluationId) {
      return;
    }

    const previousEvaluationId = previousEvaluationIdRef.current;
    if (previousEvaluationId && previousEvaluationId !== evaluationId) {
      void purgeReviewEvaluationCache(queryClient, scope, reviewCaseId, previousEvaluationId);
    }
    previousEvaluationIdRef.current = evaluationId;
  }, [evaluationId, queryClient, reviewCaseId, scope]);
}

export function useReviewWorkQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  cursor?: string | null,
) {
  const queryClient = useQueryClient();
  const resolvedScope = scope ?? { actorId: "", organizationId: "" };

  return useQuery({
    queryKey: reviewKeys.work(resolvedScope, cursor),
    queryFn: ({ signal }) => runReviewQuery(
      queryClient,
      resolvedScope,
      {},
      () => client.listWork(cursor, signal),
    ),
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
  const queryClient = useQueryClient();
  const resolvedScope = scope ?? { actorId: "", organizationId: "" };

  return useQuery({
    queryKey: reviewKeys.case(resolvedScope, reviewCaseId ?? ""),
    queryFn: ({ signal }) => runReviewQuery(
      queryClient,
      resolvedScope,
      { reviewCaseId },
      () => client.getCase(reviewCaseId!, signal),
    ),
    enabled: Boolean(scope?.actorId && scope.organizationId && reviewCaseId),
    refetchInterval: (query) =>
      isProcessingReviewState(query.state.data?.evaluation_processing_state) ? PROCESSING_POLL_MS : false,
  });
}

export function useReviewCriterionQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  reviewCaseId: string | undefined,
  evaluationId: string | undefined,
  criterionId: string | undefined,
  enabled: boolean,
) {
  const queryClient = useQueryClient();
  const resolvedScope = scope ?? { actorId: "", organizationId: "" };

  return useQuery({
    queryKey: reviewKeys.criterion(
      resolvedScope,
      reviewCaseId ?? "",
      evaluationId ?? "",
      criterionId ?? "",
    ),
    queryFn: async ({ signal }) => {
      const result = await runReviewQuery(
        queryClient,
        resolvedScope,
        { reviewCaseId },
        () => client.getCriterion(reviewCaseId!, criterionId!, signal),
      );
      assertSelectedEvaluationIdentity(result, evaluationId!);
      return result;
    },
    enabled: Boolean(
      enabled
      && scope?.actorId
      && scope.organizationId
      && reviewCaseId
      && evaluationId
      && criterionId,
    ),
  });
}

export function useReviewEvidenceQuery(
  client: ProductionReviewClient,
  scope: ReviewQueryScope | null,
  reviewCaseId: string | undefined,
  evaluationId: string | undefined,
  evidenceId: string | undefined,
  enabled: boolean,
) {
  const queryClient = useQueryClient();
  const resolvedScope = scope ?? { actorId: "", organizationId: "" };

  return useQuery({
    queryKey: reviewKeys.evidence(
      resolvedScope,
      reviewCaseId ?? "",
      evaluationId ?? "",
      evidenceId ?? "",
    ),
    queryFn: async ({ signal }) => {
      const result = await runReviewQuery(
        queryClient,
        resolvedScope,
        { reviewCaseId },
        () => client.openEvidence(reviewCaseId!, evidenceId!, signal),
      );
      assertSelectedEvaluationIdentity(result, evaluationId!);
      return result;
    },
    enabled: Boolean(
      enabled
      && scope?.actorId
      && scope.organizationId
      && reviewCaseId
      && evaluationId
      && evidenceId,
    ),
  });
}
