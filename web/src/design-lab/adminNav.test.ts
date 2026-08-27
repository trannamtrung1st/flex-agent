import { assessmentCampaignQuery, operationalCampaignId } from "./features/admin/adminNav";

describe("PC-06 campaign context selection", () => {
  const known = ["CMP-0042", "CMP-0043"];

  it("does not substitute the first campaign for an invalid requested identifier", () => {
    expect(operationalCampaignId("/admin-console/enrollments", "CMP-NOPE", known, "CMP-0043")).toBeUndefined();
  });

  it("uses an authorized remembered campaign only when the request is empty", () => {
    expect(operationalCampaignId("/admin-console/enrollments", null, known, "CMP-0043")).toBe("CMP-0043");
  });

  it("keeps a valid requested campaign", () => {
    expect(operationalCampaignId("/admin-console/enrollments", "CMP-0042", known, "CMP-0043")).toBe("CMP-0042");
  });

  it("does not put a fallback campaign on nav queries for an invalid request", () => {
    expect(assessmentCampaignQuery(known, undefined, "CMP-0043", "/admin-console/enrollments", "CMP-NOPE")).toBe("");
  });
});
