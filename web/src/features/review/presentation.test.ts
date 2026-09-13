import {
  BACK_TO_CRITERION,
  INTERNAL_EVALUATION_NOTICE,
  OPEN_EVIDENCE,
  RUNNING_CRITERION_UNAVAILABLE,
  WHOLE_ITEM_PRECISION,
  evaluationProcessingCopy,
  evaluatorModeCopy,
  isInspectableReviewState,
  processingWellCopy,
} from "./presentation";

describe("review presentation", () => {
  it("uses approved Evaluation and Evidence copy", () => {
    expect(INTERNAL_EVALUATION_NOTICE).toBe("Internal Evaluation · Not a released Result");
    expect(OPEN_EVIDENCE).toBe("Open Evidence");
    expect(BACK_TO_CRITERION).toBe("Back to criterion");
    expect(evaluationProcessingCopy("retryable_failure")).toBe("Failed — retryable");
    expect(evaluatorModeCopy("deterministic")).toBe("Rule-based");
    expect(evaluatorModeCopy("agent_assisted")).toBe("Agent-assisted");
    expect(evaluatorModeCopy("agent_judgment")).toBe("Agent judgment");
    expect(processingWellCopy({ evaluation_processing_state: "running" })).toBe(RUNNING_CRITERION_UNAVAILABLE);
    expect(WHOLE_ITEM_PRECISION).toMatch(/Whole item cited/);
    expect(isInspectableReviewState("completed")).toBe(true);
    expect(isInspectableReviewState("review_required")).toBe(true);
    expect(isInspectableReviewState("running")).toBe(false);
    expect(isInspectableReviewState("queued")).toBe(false);
    expect(isInspectableReviewState("awaiting")).toBe(false);
    expect(isInspectableReviewState("retryable_failure")).toBe(false);
  });
});
