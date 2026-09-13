import { QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { createFlexQueryClient } from "../../api/query-client";
import { ProductionApiError } from "../../api/production-api";
import { createProductionReviewClient } from "../../api/production-review";
import { reviewKeys } from "./queryKeys";
import { useReviewCriterionQuery } from "./queries";

const scope = { actorId: "actor-1", organizationId: "org-1" };
const caseId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";

function wrapper(queryClient: ReturnType<typeof createFlexQueryClient>) {
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("useReviewCriterionQuery", () => {
  it("purges protected criterion cache when review.denied is returned", async () => {
    const queryClient = createFlexQueryClient();
    const criterionKey = reviewKeys.criterion(scope, caseId, "eval-1", "crit-1");
    queryClient.setQueryData(criterionKey, { rationale: "protected criterion text" });

    const fetchJson = vi.fn().mockRejectedValue(new ProductionApiError(404, "Denied", "review.denied"));
    const client = createProductionReviewClient(fetchJson);

    renderHook(
      () => useReviewCriterionQuery(client, scope, caseId, "eval-1", "crit-1", true),
      { wrapper: wrapper(queryClient) },
    );

    await waitFor(() => {
      expect(queryClient.getQueryData(criterionKey)).toBeUndefined();
    });
    expect(fetchJson).toHaveBeenCalled();
  });
});
