import { createProductionAssessmentClient, mapActivityToSetupView } from "./production-assessment";
import { ProductionApiError } from "./production-api";

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
});
