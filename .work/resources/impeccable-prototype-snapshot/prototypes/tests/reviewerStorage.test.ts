import { beforeEach, describe, expect, it } from "vitest";
import { REVIEWER_STORAGE_KEY } from "../src/data/fixtures/reviewer";
import { loadReviewerState, persistReviewerState } from "../src/features/reviewer/storage";

describe("reviewer storage", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("returns the demo fixture when storage is empty", () => {
    const sessions = loadReviewerState("default");
    expect(sessions.length).toBeGreaterThan(0);
    expect(sessions[0].reviewStatus).toBe("awaiting");
  });

  it("ignores a mismatched demo key", () => {
    persistReviewerState("busy", loadReviewerState("busy"));
    const sessions = loadReviewerState("default");
    expect(sessions[0].reviewStatus).toBe("awaiting");
  });

  it("rehydrates compatible v1 patches", () => {
    const base = loadReviewerState("default");
    const patched = base.map((s, i) => (i === 0 ? { ...s, reviewStatus: "adjusted" as const } : s));
    persistReviewerState("default", patched);
    const loaded = loadReviewerState("default");
    expect(loaded[0].reviewStatus).toBe("adjusted");
  });

  it("falls back when stored JSON is corrupt", () => {
    localStorage.setItem(REVIEWER_STORAGE_KEY, "{not-json");
    expect(loadReviewerState("default")[0].id).toBe("sess-7c19");
  });
});
