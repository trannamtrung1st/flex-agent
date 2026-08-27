import { describe, expect, it } from "vitest";
import {
  adminAreaLabel,
  adminNavGroups,
  assessmentCampaignQuery,
  defaultsCampaignSelection,
  operationalCampaignId,
} from "../src/features/admin/adminNav";

const IDS = ["CMP-0042", "CMP-0043", "CMP-0044"];

describe("administrator navigation", () => {
  it("names each area from the pathname", () => {
    expect(adminAreaLabel("/admin-console/users-access")).toBe("Users & Access");
    expect(adminAreaLabel("/admin-console/audit-log")).toBe("Audit Log");
    expect(adminAreaLabel("/admin-console")).toBe("Administrator");
  });

  it("keeps campaign query on operational destinations only", () => {
    const items = adminNavGroups({
      pathname: "/admin-console/enrollments",
      campaignQuery: "?campaign=CMP-0043",
    }).flatMap((group) => group.items);
    const href = (label: string) => String(items.find((item) => item.label === label)?.to);
    expect(href("Campaigns")).toBe("/admin-console/campaigns");
    expect(href("Cohorts")).toBe("/admin-console/cohorts?campaign=CMP-0043");
    expect(href("Enrollments")).toBe("/admin-console/enrollments?campaign=CMP-0043");
    expect(href("Sessions")).toBe("/admin-console/sessions?campaign=CMP-0043");
    expect(href("Users & Access")).toBe("/admin-console/users-access");
    expect(href("Policies")).toBe("/admin-console/policies");
    expect(href("Audit Log")).toBe("/admin-console/audit-log");
  });

  it("defaults campaign selection on operational areas except the registry", () => {
    expect(defaultsCampaignSelection("/admin-console/enrollments")).toBe(true);
    expect(defaultsCampaignSelection("/admin-console/cohorts")).toBe(true);
    expect(defaultsCampaignSelection("/admin-console/sessions")).toBe(true);
    expect(defaultsCampaignSelection("/admin-console/campaigns")).toBe(false);
    expect(defaultsCampaignSelection("/admin-console/users-access")).toBe(false);
  });

  it("resolves operational campaign ids without inventing a registry selection", () => {
    expect(operationalCampaignId("/admin-console/campaigns", null, IDS, "CMP-0043")).toBeUndefined();
    expect(operationalCampaignId("/admin-console/campaigns", "CMP-0043", IDS, null)).toBe("CMP-0043");
    expect(operationalCampaignId("/admin-console/campaigns", "CMP-NOPE", IDS, "CMP-0043")).toBeUndefined();
    expect(operationalCampaignId("/admin-console/enrollments", null, IDS, "CMP-0043")).toBe("CMP-0043");
    expect(operationalCampaignId("/admin-console/enrollments", "CMP-NOPE", IDS, "CMP-0043")).toBe("CMP-0042");
    expect(operationalCampaignId("/admin-console/users-access", null, IDS, "CMP-0043")).toBeUndefined();
  });

  it("preserves remembered campaign on assessment links from org routes", () => {
    expect(assessmentCampaignQuery(IDS, undefined, "CMP-0043", "/admin-console/users-access")).toBe(
      "?campaign=CMP-0043",
    );
    expect(assessmentCampaignQuery(IDS, undefined, "CMP-0043", "/admin-console/campaigns")).toBe(
      "?campaign=CMP-0043",
    );
    expect(assessmentCampaignQuery(IDS, undefined, null, "/admin-console/enrollments")).toBe(
      "?campaign=CMP-0042",
    );
  });

  it("keeps Campaigns as one stable domain item for registry and records", () => {
    const registry = adminNavGroups({
      pathname: "/admin-console/campaigns",
      campaignQuery: "?campaign=CMP-0043",
    }).flatMap((group) => group.items).find((item) => item.label === "Campaigns");
    expect(registry?.current).toBe(true);
    expect(registry?.to).toBe("/admin-console/campaigns");

    const record = adminNavGroups({
      pathname: "/admin-console/campaigns",
      campaignQuery: "?campaign=CMP-0043",
    }).flatMap((group) => group.items).find((item) => item.label === "Campaigns");
    expect(record?.current).toBe(true);
    expect(record?.to).toBe("/admin-console/campaigns");

    const organization = adminNavGroups({
      pathname: "/admin-console/users-access",
      campaignQuery: "?campaign=CMP-0043",
    }).flatMap((group) => group.items).find((item) => item.label === "Campaigns");
    expect(organization?.current).toBe(false);
  });
});
