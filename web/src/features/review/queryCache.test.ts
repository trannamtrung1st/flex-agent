import { createFlexQueryClient } from "../../api/query-client";
import { ProductionApiError } from "../../api/production-api";
import { reviewKeys } from "./queryKeys";
import { purgeReviewEvaluationCache, purgeReviewProtectedCache } from "./queryCache";

const scope = { actorId: "actor-1", organizationId: "org-1" };
const caseId = "case-1";

describe("purgeReviewProtectedCache", () => {
  it("removes the Review case subtree when assignment is lost", async () => {
    const queryClient = createFlexQueryClient();
    queryClient.setQueryData(reviewKeys.case(scope, caseId), { review_case_id: caseId });
    queryClient.setQueryData(
      reviewKeys.criterion(scope, caseId, "eval-1", "crit-1"),
      { rationale: "protected" },
    );
    queryClient.setQueryData(
      reviewKeys.evidence(scope, caseId, "eval-1", "ev-1"),
      { display_text: "protected" },
    );

    await purgeReviewProtectedCache(queryClient, scope, caseId);

    expect(queryClient.getQueryData(reviewKeys.case(scope, caseId))).toEqual({ review_case_id: caseId });
    expect(queryClient.getQueryData(reviewKeys.criterion(scope, caseId, "eval-1", "crit-1"))).toBeUndefined();
    expect(queryClient.getQueryData(reviewKeys.evidence(scope, caseId, "eval-1", "ev-1"))).toBeUndefined();
  });

  it("removes the entire Review scope when work access is lost", async () => {
    const queryClient = createFlexQueryClient();
    queryClient.setQueryData(reviewKeys.work(scope), { items: [] });
    queryClient.setQueryData(reviewKeys.case(scope, caseId), { review_case_id: caseId });

    await purgeReviewProtectedCache(queryClient, scope);

    expect(queryClient.getQueryData(reviewKeys.work(scope))).toBeUndefined();
    expect(queryClient.getQueryData(reviewKeys.case(scope, caseId))).toBeUndefined();
  });
});

describe("purgeReviewEvaluationCache", () => {
  it("removes only the previous Evaluation subtree", async () => {
    const queryClient = createFlexQueryClient();
    queryClient.setQueryData(
      reviewKeys.criterion(scope, caseId, "eval-1", "crit-1"),
      { rationale: "old" },
    );
    queryClient.setQueryData(
      reviewKeys.criterion(scope, caseId, "eval-2", "crit-1"),
      { rationale: "new" },
    );

    await purgeReviewEvaluationCache(queryClient, scope, caseId, "eval-1");

    expect(queryClient.getQueryData(reviewKeys.criterion(scope, caseId, "eval-1", "crit-1"))).toBeUndefined();
    expect(queryClient.getQueryData(reviewKeys.criterion(scope, caseId, "eval-2", "crit-1"))).toEqual({
      rationale: "new",
    });
  });
});

describe("review query access loss", () => {
  it("treats review.denied as access loss for cache purge decisions", () => {
    expect(new ProductionApiError(404, "Denied", "review.denied").outcomeCode).toBe("review.denied");
  });
});
