import type { Query, QueryClient } from "@tanstack/react-query";
import type { ReviewQueryScope } from "../../api/production-review";
import { reviewKeys } from "./queryKeys";

function hasCachedPayload(query: Query) {
  return query.state.data !== undefined;
}

function resolveDenyingQueryHash(
  queryClient: QueryClient,
  denyingQueryKey?: readonly unknown[],
) {
  if (!denyingQueryKey) {
    return undefined;
  }

  return queryClient.getQueryCache().find({ queryKey: denyingQueryKey, exact: true })?.queryHash;
}

export async function purgeReviewProtectedCache(
  queryClient: QueryClient,
  scope: ReviewQueryScope,
  denyingQueryKey?: readonly unknown[],
) {
  const queryKey = reviewKeys.all(scope);
  const denyingQueryHash = resolveDenyingQueryHash(queryClient, denyingQueryKey);

  await queryClient.cancelQueries({
    queryKey,
    predicate: (query) =>
      query.state.fetchStatus === "fetching"
      && query.queryHash !== denyingQueryHash,
  });

  for (const query of queryClient.getQueryCache().findAll({ queryKey })) {
    if (!hasCachedPayload(query)) {
      continue;
    }

    if (query.getObserversCount() > 0 || query.queryHash === denyingQueryHash) {
      query.setState({
        data: undefined,
      });
      continue;
    }

    queryClient.removeQueries({ queryKey: query.queryKey, exact: true });
  }
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
    predicate: (query) => query.state.fetchStatus === "fetching",
  });
  queryClient.removeQueries({
    queryKey,
    predicate: hasCachedPayload,
  });
}
