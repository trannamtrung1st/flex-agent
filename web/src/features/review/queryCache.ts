import type { QueryClient } from "@tanstack/react-query";
import type { ReviewQueryScope } from "../../api/production-review";
import { reviewKeys } from "./queryKeys";

function hasCachedPayload(query: { state: { data: unknown } }) {
  return query.state.data !== undefined;
}

function isInFlight(query: { state: { fetchStatus: string } }) {
  return query.state.fetchStatus === "fetching";
}

function isEvaluationScopedQuery(query: { queryKey: readonly unknown[] }) {
  return query.queryKey.includes("evaluations");
}

export async function purgeReviewProtectedCache(
  queryClient: QueryClient,
  scope: ReviewQueryScope,
  reviewCaseId?: string,
) {
  if (reviewCaseId) {
    const queryKey = reviewKeys.caseRoot(scope, reviewCaseId);
    await queryClient.cancelQueries({
      queryKey,
      predicate: (query) => isInFlight(query) && isEvaluationScopedQuery(query),
    });
    queryClient.removeQueries({
      queryKey,
      predicate: (query) => hasCachedPayload(query) && isEvaluationScopedQuery(query),
    });
    return;
  }

  const queryKey = reviewKeys.all(scope);
  await queryClient.cancelQueries({
    queryKey,
    predicate: isInFlight,
  });
  queryClient.removeQueries({
    queryKey,
    predicate: hasCachedPayload,
  });
}

export async function purgeReviewEvaluationCache(
  queryClient: QueryClient,
  scope: ReviewQueryScope,
  reviewCaseId: string,
  evaluationId: string,
) {
  const queryKey = reviewKeys.evaluation(scope, reviewCaseId, evaluationId);
  await queryClient.cancelQueries({
    queryKey,
    predicate: isInFlight,
  });
  queryClient.removeQueries({
    queryKey,
    predicate: hasCachedPayload,
  });
}
