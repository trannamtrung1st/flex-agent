import { ProductionApiError } from "./production-api";
import { createProductionEnrollmentClient } from "./production-enrollment";

describe("production enrollment client", () => {
  it("maps a 409 conflict onto a failed mutation outcome", async () => {
    const fetchJson = vi.fn().mockRejectedValue(
      new ProductionApiError(409, "Request failed", "enrollment.conflict"),
    );
    const client = createProductionEnrollmentClient(fetchJson);
    const outcome = await client.assign("act-1", "coh-1", "part-1");
    expect(outcome.succeeded).toBe(false);
    expect(outcome.outcome_code).toBe("enrollment.conflict");
  });

  it("does not swallow access-loss errors", async () => {
    const fetchJson = vi.fn().mockRejectedValue(new ProductionApiError(403, "Your access changed"));
    const client = createProductionEnrollmentClient(fetchJson);
    await expect(client.assign("act-1", "coh-1", "part-1")).rejects.toMatchObject({ status: 403 });
  });
});
