import { ProductionApiError } from "./production-api";
import { createProductionEnrollmentClient, enrollmentOutcomeCopy } from "./production-enrollment";

describe("production enrollment client", () => {
  it("maps a 409 conflict onto a failed mutation outcome", async () => {
    const fetchJson = vi.fn().mockRejectedValue(
      new ProductionApiError(409, "Request failed", "enrollment.conflict"),
    );
    const client = createProductionEnrollmentClient(fetchJson);
    const outcome = await client.assign("act-1", "coh-1", "part-1", "enr-key-1");
    expect(outcome.succeeded).toBe(false);
    expect(outcome.outcome_code).toBe("enrollment.conflict");
  });

  it("maps a 429 onto a failed mutation outcome", async () => {
    const fetchJson = vi.fn().mockRejectedValue(
      new ProductionApiError(429, "Request failed", "enrollment.rate_limited"),
    );
    const client = createProductionEnrollmentClient(fetchJson);
    const outcome = await client.assign("act-1", "coh-1", "part-1", "enr-key-1");
    expect(outcome.succeeded).toBe(false);
    expect(outcome.outcome_code).toBe("enrollment.rate_limited");
  });

  it("does not swallow access-loss errors", async () => {
    const fetchJson = vi.fn().mockRejectedValue(new ProductionApiError(403, "Your access changed"));
    const client = createProductionEnrollmentClient(fetchJson);
    await expect(client.assign("act-1", "coh-1", "part-1", "enr-key-1")).rejects.toMatchObject({ status: 403 });
  });

  it("sends the retained idempotency key", async () => {
    const fetchJson = vi.fn().mockResolvedValue({
      schema_version: "v1",
      succeeded: true,
      outcome_code: "enrollment.suspended",
      permitted_actions: [],
    });
    const client = createProductionEnrollmentClient(fetchJson);
    await client.mutate("act-1", "coh-1", "enr-1", "suspend", "temporary_restriction", 1, "enr-retry-1");
    const init = fetchJson.mock.calls[0]?.[1] as RequestInit | undefined;
    const body = init?.body;
    if (typeof body !== "string") {
      throw new Error("expected a JSON request body");
    }

    expect(JSON.parse(body)).toEqual(
      expect.objectContaining({ idempotency_key: "enr-retry-1" }),
    );
  });

  it("passes a signed list cursor on the next page request", async () => {
    const fetchJson = vi.fn().mockResolvedValue({ schema_version: "v1", items: [], has_more: false });
    const client = createProductionEnrollmentClient(fetchJson);
    await client.listEnrollments("act-1", "coh-1", "cur-1");
    await client.listCandidates("act-1", "coh-1", "cur-2");
    expect(fetchJson.mock.calls[0]?.[0]).toBe(
      "/v1/assessment/activities/act-1/cohorts/coh-1/enrollments?cursor=cur-1",
    );
    expect(fetchJson.mock.calls[1]?.[0]).toBe(
      "/v1/assessment/activities/act-1/cohorts/coh-1/participant-options?cursor=cur-2",
    );
  });

  it("names a duplicate assignment without treating it as a second success", () => {
    expect(enrollmentOutcomeCopy("enrollment.assignment.deduplicated", "Assignment did not complete.")).toBe(
      "Already assigned",
    );
  });
});
