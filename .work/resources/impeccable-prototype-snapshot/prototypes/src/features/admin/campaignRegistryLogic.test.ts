import { describe, expect, it } from "vitest";
import { campaignQueryKey, matchingCampaignIds, sortAndFilterCampaigns } from "./campaignRegistryLogic";
import type { CampaignRegistryRow, CampaignRegistryState } from "../../data/types";
import { EMPTY_SELECTION } from "../../components/tableSelection";

const rows: CampaignRegistryRow[] = [
  { id: "CMP-1", name: "Alpha", frozen: false, enrollments: 2, deadline: null, updatedAt: new Date("2026-01-01") },
  { id: "CMP-2", name: "Bravo", frozen: true, enrollments: 4, deadline: null, updatedAt: new Date("2026-01-02") },
  { id: "CMP-3", name: "Alpha Two", frozen: false, enrollments: 1, deadline: null, updatedAt: new Date("2026-01-03") },
];

const base: CampaignRegistryState = {
  search: "",
  activationFilter: "all",
  sorts: [{ key: "campaign", dir: "asc" }],
  page: 0,
  pageSize: 16,
  selection: EMPTY_SELECTION,
};

describe("campaignRegistryLogic", () => {
  it("keeps query keys independent of sort and page", () => {
    expect(campaignQueryKey({ search: "", activationFilter: "all" })).toBe("activation:all|search:");
    expect(campaignQueryKey({ search: "alpha", activationFilter: "draft" })).toBe(
      "activation:draft|search:ALPHA",
    );
  });

  it("matches the rendered filter set", () => {
    const state = { ...base, search: "alpha", activationFilter: "draft" as const };
    expect(matchingCampaignIds(rows, state)).toEqual(
      sortAndFilterCampaigns(rows, state).map((row) => row.id),
    );
    expect(matchingCampaignIds(rows, state)).toEqual(["CMP-1", "CMP-3"]);
  });
});
