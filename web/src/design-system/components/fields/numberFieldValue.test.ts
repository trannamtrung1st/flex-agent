import { describe, expect, it } from "vitest";
import { decimalPlacesFromStep, parseNumberFieldValue, stepNumberFieldValue } from "./numberFieldValue";

describe("stepNumberFieldValue", () => {
  it("increments from the current value by step", () => {
    expect(stepNumberFieldValue("3", 1, { step: 1 })).toBe("4");
    expect(stepNumberFieldValue("3.0", 1, { step: 0.5 })).toBe("3.5");
  });

  it("decrements and clamps to min and max", () => {
    expect(stepNumberFieldValue("1", -1, { min: 0, max: 4, step: 1 })).toBe("0");
    expect(stepNumberFieldValue("0", -1, { min: 0, max: 4, step: 1 })).toBe("0");
    expect(stepNumberFieldValue("4", 1, { min: 0, max: 4, step: 1 })).toBe("4");
  });

  it("treats an empty slot as zero before stepping", () => {
    expect(stepNumberFieldValue("", 1, { min: 0, max: 4, step: 1 })).toBe("1");
    expect(stepNumberFieldValue("", -1, { min: 0, max: 4, step: 1 })).toBe("0");
  });
});

describe("parseNumberFieldValue", () => {
  it("returns null for empty or non-numeric text", () => {
    expect(parseNumberFieldValue("")).toBeNull();
    expect(parseNumberFieldValue("  ")).toBeNull();
    expect(parseNumberFieldValue("n/a")).toBeNull();
  });
});

describe("decimalPlacesFromStep", () => {
  it("counts places from the step literal", () => {
    expect(decimalPlacesFromStep(1)).toBe(0);
    expect(decimalPlacesFromStep(0.5)).toBe(1);
    expect(decimalPlacesFromStep(0.25)).toBe(2);
  });
});
