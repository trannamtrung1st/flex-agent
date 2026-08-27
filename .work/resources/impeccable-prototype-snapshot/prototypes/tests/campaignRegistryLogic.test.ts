import { describe, expect, it } from "vitest";
import type { CampaignRegistryRow, CampaignRegistryState } from "../src/data/types";
import { EMPTY_SELECTION } from "../src/components/tableSelection";
import { pageCampaigns, sortAndFilterCampaigns } from "../src/features/admin/campaignRegistryLogic";

const rows: CampaignRegistryRow[] = [
  { id: "CMP-0042", name: "Structural Audit Q3", frozen: false, enrollments: 120, deadline: new Date("2026-08-29T09:00:00"), updatedAt: new Date("2026-08-24T18:12:00") },
  { id: "CMP-0043", name: "Ops Integrity", frozen: false, enrollments: 64, deadline: new Date("2026-08-30T09:00:00"), updatedAt: new Date("2026-08-23T11:06:00") },
  { id: "CMP-0045", name: "Fleet Readiness", frozen: true, enrollments: 22, deadline: new Date("2026-07-14T08:00:00"), updatedAt: new Date("2026-07-14T11:40:00") },
];

const base: CampaignRegistryState = {
  search: "",
  activationFilter: "all",
  sorts: [{ key: "campaign", dir: "asc" }],
  page: 0,
  pageSize: 2,
  selection: EMPTY_SELECTION,
};

describe("sortAndFilterCampaigns", () => {
  it("sorts by campaign id ascending", () => {
    const list = sortAndFilterCampaigns(rows, base);
    expect(list.map((r) => r.id)).toEqual(["CMP-0042", "CMP-0043", "CMP-0045"]);
  });

  it("filters draft vs frozen", () => {
    expect(sortAndFilterCampaigns(rows, { ...base, activationFilter: "frozen" }).map((r) => r.id)).toEqual(["CMP-0045"]);
    expect(sortAndFilterCampaigns(rows, { ...base, activationFilter: "draft" }).map((r) => r.id)).toEqual(["CMP-0042", "CMP-0043"]);
  });

  it("searches id and name", () => {
    expect(sortAndFilterCampaigns(rows, { ...base, search: "ops" })).toHaveLength(1);
    expect(sortAndFilterCampaigns(rows, { ...base, search: "CMP-0042" })[0].id).toBe("CMP-0042");
  });

  it("sorts by enrollments descending", () => {
    const list = sortAndFilterCampaigns(rows, { ...base, sorts: [{ key: "enrollments", dir: "desc" }] });
    expect(list.map((r) => r.id)).toEqual(["CMP-0042", "CMP-0043", "CMP-0045"]);
  });
});

describe("pageCampaigns", () => {
  it("pages filtered rows", () => {
    const list = sortAndFilterCampaigns(rows, base);
    const slice = pageCampaigns(list, base);
    expect(slice.pageRows).toHaveLength(2);
    expect(slice.total).toBe(3);
    expect(slice.pageCount).toBe(2);
  });
});
