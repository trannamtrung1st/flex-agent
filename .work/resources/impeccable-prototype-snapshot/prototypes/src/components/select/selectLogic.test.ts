import { describe, expect, it } from "vitest";
import { filterOptionIndices, optionNounCount, pinIndex, stepVisibleIndex } from "./selectLogic";

const options = [
  { id: "CMP-0042", label: "CMP-0042 / Structural Audit Q3" },
  { id: "CMP-0043", label: "CMP-0043 / Ops Integrity" },
  { id: "GOVERNED-OPS-02", label: "GOVERNED-OPS-02 / Cross-region failover harness" },
];

describe("filterOptionIndices", () => {
  it("matches case-insensitively on label or id by default", () => {
    expect(filterOptionIndices(options, "ops", false, (item) => [item.label, item.id])).toEqual([1, 2]);
  });

  it("matches exact case when configured", () => {
    expect(filterOptionIndices(options, "ops", true, (item) => [item.label, item.id])).toEqual([]);
    expect(filterOptionIndices(options, "Ops", true, (item) => [item.label, item.id])).toEqual([1]);
  });
});

describe("pinIndex", () => {
  it("prepends a hidden committed index", () => {
    expect(pinIndex([1, 2], 0)).toEqual([0, 1, 2]);
    expect(pinIndex([1, 2], 1)).toEqual([1, 2]);
    expect(pinIndex([1], -1)).toEqual([1]);
  });
});

describe("optionNounCount", () => {
  it("preserves the existing endsWith(s) pluralization", () => {
    expect(optionNounCount(1, "harness")).toBe("1 harness");
    expect(optionNounCount(3, "harness")).toBe("3 harnesses");
    expect(optionNounCount(2, "campaign")).toBe("2 campaigns");
  });
});

describe("stepVisibleIndex", () => {
  it("steps within the visible set and wraps from an unfocused start", () => {
    expect(stepVisibleIndex([1, 2], -1, 1)).toBe(1);
    expect(stepVisibleIndex([1, 2], 1, 1)).toBe(2);
    expect(stepVisibleIndex([1, 2], 2, 1)).toBe(2);
    expect(stepVisibleIndex([], 0, 1)).toBeUndefined();
  });
});
