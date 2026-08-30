import { REQUIRED_SOURCE_CATEGORIES } from "../../api/production-assessment";
import {
  inheritedSourceCategories,
  INTENT_SOURCE_CATEGORIES,
  sourceCategoryLabel,
  createSourceEligibilityMode,
  sourceEligibilityLabel,
  sourceRevisionCaption,
  sourceSelectOptionLabel,
} from "./campaignCreatePresentation";

describe("campaign create presentation", () => {
  it("names required categories in product language", () => {
    expect(sourceCategoryLabel("agent")).toBe("Agent");
    expect(sourceCategoryLabel("organization_policy")).toBe("Organization policy");
    expect(sourceCategoryLabel("adaptive_follow_up")).toBe("Adaptive follow-up");
    expect(sourceCategoryLabel("review_release")).toBe("Review and Release");
    expect(REQUIRED_SOURCE_CATEGORIES.map(sourceCategoryLabel)).not.toContain("organization_policy");
  });

  it("keeps Agent and Harness as the intent pair and the rest as inherited berths", () => {
    expect([...INTENT_SOURCE_CATEGORIES]).toEqual(["agent", "harness"]);
    expect(inheritedSourceCategories()).toEqual([
      "organization_policy",
      "workflow",
      "adaptive_follow_up",
      "rubric_evaluation",
      "model_deployment",
      "capability",
      "review_release",
      "task_submission",
    ]);
  });

  it("does not put a UUID version in the primary option caption", () => {
    expect(sourceRevisionCaption("v1")).toBe("v1");
    expect(sourceRevisionCaption("33333333-3333-3333-3333-333333333301")).toBe("33333333");
    expect(sourceSelectOptionLabel("assessment.agent revision.v1", "33333333-3333-3333-3333-333333333302"))
      .toBe("agent revision · 33333333");
    expect(sourceSelectOptionLabel("assessment.organization_policy.v1", "33333333-3333-3333-3333-333333333301", "revision"))
      .toBe("33333333");
    expect(sourceEligibilityLabel(true)).toBe("available");
    expect(sourceEligibilityLabel(false)).toBe("development");
  });

  it("uses one plate note when the whole selected set is development-only", () => {
    expect(createSourceEligibilityMode([
      { production_eligible: false },
      { production_eligible: false },
    ])).toBe("plate");
    expect(createSourceEligibilityMode([
      { production_eligible: true },
      { production_eligible: false },
    ])).toBe("berth");
    expect(createSourceEligibilityMode([{ production_eligible: true }])).toBe("silent");
  });
});
