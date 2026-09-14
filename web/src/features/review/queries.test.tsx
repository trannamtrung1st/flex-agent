import { QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { createFlexQueryClient } from "../../api/query-client";
import { ProductionApiError } from "../../api/production-api";
import { createProductionReviewClient } from "../../api/production-review";
import { reviewKeys } from "./queryKeys";
import { useReviewCaseQuery, useReviewCriterionQuery, useReviewWorkInfiniteQuery, useReviewWorkQuery } from "./queries";

const scope = { actorId: "actor-1", organizationId: "org-1" };
const caseId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";

function wrapper(queryClient: ReturnType<typeof createFlexQueryClient>) {
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

function abortError(signal?: AbortSignal): Error {
  const reason = signal?.reason;
  if (reason instanceof Error) {
    return reason;
  }
  if (typeof reason === "string") {
    return new Error(reason);
  }
  return new DOMException("Aborted", "AbortError");
}

function waitForAbort(signal?: AbortSignal) {
  return new Promise<never>((_, reject) => {
    if (signal?.aborted) {
      reject(abortError(signal));
      return;
    }

    signal?.addEventListener(
      "abort",
      () => {
        reject(abortError(signal));
      },
      { once: true },
    );
  });
}

describe("useReviewCriterionQuery", () => {
  it("preserves review.denied on the denying query after purge", async () => {
    const queryClient = createFlexQueryClient();
    const criterionKey = reviewKeys.criterion(scope, caseId, "eval-1", "crit-1");
    queryClient.setQueryData(criterionKey, { rationale: "protected criterion text" });

    const fetchJson = vi.fn().mockRejectedValue(new ProductionApiError(404, "Denied", "review.denied"));
    const client = createProductionReviewClient(fetchJson);

    const { result } = renderHook(
      () => useReviewCriterionQuery(client, scope, caseId, "eval-1", "crit-1", true),
      { wrapper: wrapper(queryClient) },
    );

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
    expect(result.current.error).toBeInstanceOf(ProductionApiError);
    expect((result.current.error as ProductionApiError).outcomeCode).toBe("review.denied");
    expect(queryClient.getQueryData(criterionKey)).toBeUndefined();
    expect(fetchJson).toHaveBeenCalled();
  });

  it("cancels concurrent Review reads so they cannot repopulate cache after purge", async () => {
    const queryClient = createFlexQueryClient();
    const workKey = reviewKeys.work(scope);
    const caseKey = reviewKeys.case(scope, caseId);
    const criterionKey = reviewKeys.criterion(scope, caseId, "eval-1", "crit-1");

    queryClient.setQueryData(workKey, {
      items: [{ review_case_id: caseId, task_label: "Case study" }],
    });
    queryClient.setQueryData(caseKey, {
      review_case_id: caseId,
      evaluation_id: "eval-1",
      participant_label: "Participant One",
    });

    const fetchJson = vi.fn(async (path: string, init?: RequestInit) => {
      if (path.includes("/criteria/")) {
        throw new ProductionApiError(404, "Denied", "review.denied");
      }
      if (path.includes("/work")) {
        await waitForAbort(init?.signal ?? undefined);
        return {
          schema_version: "v1" as const,
          items: [{ review_case_id: caseId, task_label: "Repopulated work" }],
          has_more: false,
        };
      }
      if (path.includes("/cases/") && !path.includes("criteria") && !path.includes("evidence")) {
        await waitForAbort(init?.signal ?? undefined);
        return {
          schema_version: "v1" as const,
          review_case_id: caseId,
          evaluation_id: "eval-1",
          participant_label: "Repopulated case",
        };
      }
      throw new ProductionApiError(404, "Not found");
    });
    const client = createProductionReviewClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);

    renderHook(() => useReviewWorkQuery(client, scope), { wrapper: wrapper(queryClient) });
    renderHook(() => useReviewCaseQuery(client, scope, caseId), { wrapper: wrapper(queryClient) });
    const { result } = renderHook(
      () => useReviewCriterionQuery(client, scope, caseId, "eval-1", "crit-1", true),
      { wrapper: wrapper(queryClient) },
    );

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
    expect((result.current.error as ProductionApiError).outcomeCode).toBe("review.denied");
    await waitFor(() => {
      expect(queryClient.getQueryData(workKey)).toBeUndefined();
      expect(queryClient.getQueryData(caseKey)).toBeUndefined();
      expect(queryClient.getQueryData(criterionKey)).toBeUndefined();
    });
  });
});

describe("useReviewWorkInfiniteQuery", () => {
  it("preserves review.denied on the infinite work query after initial-load purge", async () => {
    const queryClient = createFlexQueryClient();
    const workPagesKey = reviewKeys.workPages(scope);
    const caseKey = reviewKeys.case(scope, caseId);
    queryClient.setQueryData(workPagesKey, {
      pages: [{ schema_version: "v1", items: [{ review_case_id: caseId, task_label: "Protected work" }], has_more: false }],
      pageParams: [null],
    });
    queryClient.setQueryData(caseKey, {
      review_case_id: caseId,
      evaluation_id: "eval-1",
      participant_label: "Participant One",
    });

    let workSignal: AbortSignal | undefined;
    const fetchJson = vi.fn((path: string, init?: RequestInit) => {
      if (path.includes("/work")) {
        workSignal = init?.signal ?? undefined;
        return Promise.reject(new ProductionApiError(404, "Denied", "review.denied"));
      }
      return Promise.reject(new ProductionApiError(404, "Not found"));
    });
    const client = createProductionReviewClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);

    const { result } = renderHook(
      () => useReviewWorkInfiniteQuery(client, scope),
      { wrapper: wrapper(queryClient) },
    );

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
    expect(result.current.error).toBeInstanceOf(ProductionApiError);
    expect((result.current.error as ProductionApiError).outcomeCode).toBe("review.denied");
    expect(workSignal?.aborted).toBe(false);
    expect(queryClient.getQueryData(workPagesKey)).toBeUndefined();
    expect(queryClient.getQueryData(caseKey)).toBeUndefined();
  });

  it("preserves review.denied when Load more is denied and does not self-abort", async () => {
    const queryClient = createFlexQueryClient();
    const workPagesKey = reviewKeys.workPages(scope);
    const caseKey = reviewKeys.case(scope, caseId);
    let loadMoreSignal: AbortSignal | undefined;
    let caseSignal: AbortSignal | undefined;

    const fetchJson = vi.fn(async (path: string, init?: RequestInit) => {
      if (path.includes("/work") && path.includes("cursor=")) {
        loadMoreSignal = init?.signal ?? undefined;
        throw new ProductionApiError(404, "Denied", "review.denied");
      }
      if (path.includes("/work")) {
        return {
          schema_version: "v1" as const,
          items: [{ review_case_id: caseId, task_label: "First case" }],
          has_more: true,
          next_cursor: "page-2",
        };
      }
      if (path.includes("/cases/")) {
        caseSignal = init?.signal ?? undefined;
        await waitForAbort(init?.signal ?? undefined);
        return {
          schema_version: "v1" as const,
          review_case_id: caseId,
          evaluation_id: "eval-1",
          participant_label: "Repopulated case",
        };
      }
      throw new ProductionApiError(404, "Not found");
    });
    const client = createProductionReviewClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);

    const { result } = renderHook(
      () => useReviewWorkInfiniteQuery(client, scope),
      { wrapper: wrapper(queryClient) },
    );

    await waitFor(() => {
      expect(result.current.hasNextPage).toBe(true);
      expect(result.current.data?.pages[0]?.items).toHaveLength(1);
    });

    renderHook(() => useReviewCaseQuery(client, scope, caseId), { wrapper: wrapper(queryClient) });
    await result.current.fetchNextPage();

    await waitFor(() => {
      expect(result.current.isError).toBe(true);
    });
    expect(result.current.error).toBeInstanceOf(ProductionApiError);
    expect((result.current.error as ProductionApiError).outcomeCode).toBe("review.denied");
    expect(loadMoreSignal?.aborted).toBe(false);
    await waitFor(() => {
      expect(caseSignal?.aborted).toBe(true);
      expect(queryClient.getQueryData(workPagesKey)).toBeUndefined();
      expect(queryClient.getQueryData(caseKey)).toBeUndefined();
    });
  });
});
