import { ProductionApiError } from "./production-api";
import { createProductionReviewClient, isReviewAccessLoss } from "./production-review";

describe("createProductionReviewClient", () => {
  it("requests assigned work, case, criterion, and evidence locators", async () => {
    const fetchJson = vi.fn().mockResolvedValue({ schema_version: "v1" });
    const client = createProductionReviewClient(fetchJson);
    const signal = new AbortController().signal;

    await client.listWork("cursor-1", signal);
    await client.getCase("case-1", signal);
    await client.getCriterion("case-1", "crit-1", signal);
    await client.openEvidence("case-1", "ev-1", signal);

    expect(fetchJson).toHaveBeenNthCalledWith(1, "/v1/review/work?cursor=cursor-1", { signal });
    expect(fetchJson).toHaveBeenNthCalledWith(2, "/v1/review/cases/case-1", { signal });
    expect(fetchJson).toHaveBeenNthCalledWith(3, "/v1/review/cases/case-1/criteria/crit-1", { signal });
    expect(fetchJson).toHaveBeenNthCalledWith(4, "/v1/review/cases/case-1/evidence/ev-1", { signal });
  });

  it("treats review.denied as access loss", () => {
    expect(isReviewAccessLoss(new ProductionApiError(404, "Denied", "review.denied"))).toBe(true);
    expect(isReviewAccessLoss(new ProductionApiError(403, "Your access changed"))).toBe(true);
    expect(isReviewAccessLoss(new ProductionApiError(500, "Request failed"))).toBe(false);
  });
});
