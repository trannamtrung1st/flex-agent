import { reviewKeys } from "./queryKeys";

describe("reviewKeys", () => {
  it("scopes every key by organization and actor", () => {
    const scope = { actorId: "actor-1", organizationId: "org-1" };
    const other = { actorId: "actor-2", organizationId: "org-1" };

    expect(reviewKeys.work(scope)).toEqual(["review", "v1", "org-1", "actor-1", "work", ""]);
    expect(reviewKeys.case(scope, "case-1")).toEqual(["review", "v1", "org-1", "actor-1", "cases", "case-1"]);
    expect(reviewKeys.criterion(scope, "case-1", "crit-1")).toContain("actor-1");
    expect(reviewKeys.evidence(scope, "case-1", "ev-1")).toContain("org-1");
    expect(reviewKeys.work(scope)).not.toEqual(reviewKeys.work(other));
  });
});
