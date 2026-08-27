import { assessmentKeys } from "./queryKeys";

describe("assessment query keys", () => {
  it("versions list and source-option keys without embedding actor or organization identity", () => {
    expect(assessmentKeys.all).toEqual(["assessment"]);
    expect(assessmentKeys.v1).toEqual(["assessment", "v1"]);
    expect(assessmentKeys.activities()).toEqual(["assessment", "v1", "activities", "list"]);
    expect(assessmentKeys.sourceOptions()).toEqual(["assessment", "v1", "activities", "source-options"]);
    expect(assessmentKeys.activity("act-1")).toEqual(["assessment", "v1", "activities", "detail", "act-1"]);
    expect(JSON.stringify(assessmentKeys.activities())).not.toMatch(/org|actor|csrf/i);
  });
});
