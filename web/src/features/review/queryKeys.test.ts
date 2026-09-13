import { reviewKeys } from "./queryKeys";

describe("reviewKeys", () => {
  it("scopes every key by organization, actor, and selected Evaluation identity", () => {
    const scope = { actorId: "actor-1", organizationId: "org-1" };
    const other = { actorId: "actor-2", organizationId: "org-1" };

    expect(reviewKeys.work(scope)).toEqual(["review", "v1", "org-1", "actor-1", "work", ""]);
    expect(reviewKeys.case(scope, "case-1")).toEqual([
      "review",
      "v1",
      "org-1",
      "actor-1",
      "cases",
      "case-1",
      "read",
    ]);
    expect(reviewKeys.criterion(scope, "case-1", "eval-1", "crit-1")).toEqual([
      "review",
      "v1",
      "org-1",
      "actor-1",
      "cases",
      "case-1",
      "evaluations",
      "eval-1",
      "criteria",
      "crit-1",
    ]);
    expect(reviewKeys.evidence(scope, "case-1", "eval-2", "ev-1")).toContain("eval-2");
    expect(reviewKeys.work(scope)).not.toEqual(reviewKeys.work(other));
    expect(reviewKeys.criterion(scope, "case-1", "eval-1", "crit-1")).not.toEqual(
      reviewKeys.criterion(scope, "case-1", "eval-2", "crit-1"),
    );
  });
});
