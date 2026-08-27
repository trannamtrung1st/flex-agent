import { describe, expect, it } from "vitest";
import { campaignsConfigJson, campaignsToCsv, configFilename, summaryFilename } from "./campaignArtifacts";
import type { Campaign } from "../../data/types";

const campaign = (id: string, frozen: boolean): Campaign => ({
  id,
  name: `Name, ${id}`,
  frozen,
  config: {
    harness: "GOVERNED-EXAM-01",
    agent: "EXAMINER-CORE",
    sessionLimit: "60:00",
    timeWarning: "10:00",
    maxAttempts: "2",
    cooldown: "24H",
  },
  rows: [],
  updatedAt: new Date("2026-08-24T18:12:00"),
});

describe("campaignArtifacts", () => {
  it("serializes CSV with escaped names", () => {
    const csv = campaignsToCsv([campaign("CMP-1", false)]);
    expect(csv).toContain("id,name,activation,enrollments,cohort_deadline,updated");
    expect(csv).toContain('"Name, CMP-1"');
    expect(csv).toContain("draft");
  });

  it("serializes frozen configuration JSON", () => {
    const json = JSON.parse(campaignsConfigJson([campaign("CMP-2", true)]));
    expect(json[0].id).toBe("CMP-2");
    expect(json[0].config.harness).toBe("GOVERNED-EXAM-01");
  });

  it("uses deterministic filenames", () => {
    const stamped = new Date("2026-08-26T00:00:00");
    expect(summaryFilename(3, stamped)).toBe("campaign-summary-20260826-03.csv");
    expect(configFilename([campaign("CMP-2", true)], stamped)).toBe("campaign-config-CMP-2.json");
  });
});
