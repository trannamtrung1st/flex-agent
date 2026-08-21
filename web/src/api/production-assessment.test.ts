import {
  createProductionAssessmentClient,
  mapActivityToSetupView,
  resolveSelectedSources,
  sourceOptionIdentity,
} from "./production-assessment";
import { ProductionApiError } from "./production-api";

const draftView = {
  activity_id: "act-1",
  title: "Campaign",
  revision_id: "rev-1",
  revision_number: 1,
  memory_mode: "disabled",
  has_activated_cohort: false,
  permitted_actions: ["activate_cohort"],
  cohort_id: "cohort-1",
};

describe("production assessment client", () => {
  it("omits assignment and maps exact source references", () => {
    const view = mapActivityToSetupView({
      activity_id: "act-1",
      title: "Campaign",
      revision_id: "rev-1",
      revision_number: 2,
      memory_mode: "disabled",
      has_activated_cohort: true,
      baseline_digest: "a".repeat(64),
      permitted_actions: [],
      sources: {
        agent: { source_id: "s1", version_id: "v1", content_digest: "b".repeat(64) },
      },
    });

    expect(view.permitted_actions).not.toContain("assign_participants");
    expect(view.sources).toEqual([
      { category: "agent", source_id: "s1", version_id: "v1", content_digest: "b".repeat(64) },
    ]);
  });

  it("surfaces a stale save without treating it as authorization loss", async () => {
    const fetchJson = vi.fn().mockRejectedValue(
      new ProductionApiError(409, "Request failed", "assessment.stale_revision"),
    );
    const client = createProductionAssessmentClient(fetchJson);

    await expect(client.saveDraft("act-1", "Next", 1)).rejects.toMatchObject({
      status: 409,
      message: "This draft changed",
    });
  });

  it("does not treat a denied save as a stale draft", async () => {
    const fetchJson = vi.fn().mockRejectedValue(
      new ProductionApiError(409, "Request failed", "assessment.denied"),
    );
    const client = createProductionAssessmentClient(fetchJson);

    await expect(client.saveDraft("act-1", "Next", 1)).rejects.toMatchObject({
      status: 409,
      message: "Request failed",
      outcomeCode: "assessment.denied",
    });
  });

  it("reconciles a lost activation POST using the same idempotency key", async () => {
    const fetchJson = vi.fn((path: string, init?: RequestInit) => {
      if (path.includes("/activate") && init?.method === "POST") {
        return Promise.reject(new TypeError("Failed to fetch"));
      }

      if (path.includes("/activation?")) {
        return Promise.resolve({ succeeded: true, outcome_code: "assessment.ok", cohort_state: "activated" });
      }

      if (path.includes("/activities/act-1")) {
        return Promise.resolve({
          activity_id: "act-1",
          title: "Campaign",
          revision_id: "rev-1",
          revision_number: 1,
          memory_mode: "disabled",
          has_activated_cohort: true,
          baseline_digest: "a".repeat(64),
          permitted_actions: [],
        });
      }

      return Promise.reject(new Error(path));
    });
    const client = createProductionAssessmentClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);

    const view = await client.activateCohort("act-1", draftView);
    expect(view.has_activated_cohort).toBe(true);
    const postedBody = fetchJson.mock.calls[0]?.[1]?.body;
    expect(typeof postedBody).toBe("string");
    const posted = JSON.parse(postedBody as string) as { idempotency_key: string };
    expect(fetchJson.mock.calls[1]?.[0]).toContain(`idempotency_key=${encodeURIComponent(posted.idempotency_key)}`);
  });

  it("does not reconcile after an access-loss activation failure", async () => {
    const fetchJson = vi.fn((path: string) => {
      if (path.includes("/activate")) {
        return Promise.reject(new ProductionApiError(403, "Your access changed"));
      }

      return Promise.reject(new Error(`unexpected ${path}`));
    });
    const client = createProductionAssessmentClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);

    await expect(client.activateCohort("act-1", draftView)).rejects.toMatchObject({ status: 403 });
    expect(fetchJson).toHaveBeenCalledTimes(1);
  });

  it("propagates access loss when reconciliation is forbidden after a lost POST", async () => {
    const fetchJson = vi.fn((path: string, init?: RequestInit) => {
      if (path.includes("/activate") && init?.method === "POST") {
        return Promise.reject(new TypeError("Failed to fetch"));
      }

      if (path.includes("/activation?")) {
        return Promise.reject(new ProductionApiError(403, "Your access changed"));
      }

      return Promise.reject(new Error(`unexpected ${path}`));
    });
    const client = createProductionAssessmentClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);

    await expect(client.activateCohort("act-lost-403", { ...draftView, activity_id: "act-lost-403" }))
      .rejects.toMatchObject({ status: 403, message: "Your access changed" });
  });

  it("reuses an idempotency key only for the same expected revision", async () => {
    const keys: string[] = [];
    const fetchJson = vi.fn((path: string, init?: RequestInit) => {
      if (path.includes("/activate") && init?.method === "POST") {
        const rawBody = init.body;
        if (typeof rawBody !== "string") {
          throw new Error("expected activation body");
        }

        const body = JSON.parse(rawBody) as { idempotency_key: string };
        keys.push(body.idempotency_key);
        return Promise.reject(new ProductionApiError(409, "Request failed", "assessment.stale_revision"));
      }

      if (path.includes("/activation?")) {
        return Promise.resolve({ succeeded: false, outcome_code: "assessment.stale_revision" });
      }

      return Promise.reject(new Error(`unexpected ${path}`));
    });
    const client = createProductionAssessmentClient(fetchJson as <T>(path: string, init?: RequestInit) => Promise<T>);
    const first = { ...draftView, activity_id: "act-rev", revision_id: "rev-5", revision_number: 5 };

    await expect(client.activateCohort("act-rev", first)).rejects.toBeTruthy();
    await expect(client.activateCohort("act-rev", first)).rejects.toBeTruthy();
    await expect(client.activateCohort("act-rev", { ...first, revision_id: "rev-6", revision_number: 6 })).rejects.toBeTruthy();

    expect(keys).toHaveLength(3);
    expect(keys[0]).toEqual(keys[1]);
    expect(keys[2]).not.toEqual(keys[0]);
  });

  it("selects sources by category and source/version identity", () => {
    const sources = [
      { category: "agent", source_id: "agent-a", version_id: "v1", content_digest: "a".repeat(64), source_kind: "agent", production_eligible: true },
      { category: "harness", source_id: "harness-a", version_id: "v1", content_digest: "b".repeat(64), source_kind: "harness", production_eligible: true },
    ];
    const selected = {
      agent: sourceOptionIdentity(sources[0]),
      harness: sourceOptionIdentity(sources[1]),
    };

    expect(resolveSelectedSources(sources, selected, ["agent", "harness"])).toEqual({
      agent: sources[0],
      harness: sources[1],
    });
  });
});
