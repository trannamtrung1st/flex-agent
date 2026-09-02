import { canonicalizeActivityListQuery, DEFAULT_ACTIVITY_LIST_QUERY } from "../../api/production-assessment";
import { assessmentKeys } from "./queryKeys";

describe("assessment query keys", () => {
  it("versions list and source-option keys without embedding actor or organization identity", () => {
    expect(assessmentKeys.all).toEqual(["assessment"]);
    expect(assessmentKeys.v1).toEqual(["assessment", "v1"]);
    expect(assessmentKeys.activitiesRoot()).toEqual(["assessment", "v1", "activities", "list"]);
    expect(assessmentKeys.activities()).toEqual(["assessment", "v1", "activities", "list"]);
    expect(assessmentKeys.activities(DEFAULT_ACTIVITY_LIST_QUERY)).toEqual([
      "assessment",
      "v1",
      "activities",
      "list",
      canonicalizeActivityListQuery(DEFAULT_ACTIVITY_LIST_QUERY),
    ]);
    expect(assessmentKeys.sourceOptions()).toEqual(["assessment", "v1", "activities", "source-options"]);
    expect(assessmentKeys.activity("act-1")).toEqual(["assessment", "v1", "activities", "detail", "act-1"]);
    expect(JSON.stringify(assessmentKeys.activities(DEFAULT_ACTIVITY_LIST_QUERY))).not.toMatch(/org|actor|csrf/i);
  });
});
