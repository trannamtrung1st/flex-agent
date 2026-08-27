import { describe, expect, it } from "vitest";
import { boundedReasonError, clearValidationErrorOnValid, trimmedTextError } from "./fieldValidation";

describe("trimmedTextError", () => {
  it("requires non-empty trimmed input", () => {
    expect(trimmedTextError("   ", { emptyMessage: "Required." })).toBe("Required.");
  });

  it("enforces a minimum trimmed length", () => {
    expect(
      trimmedTextError("abc", {
        minLength: 8,
        emptyMessage: "Required.",
        minLengthMessage: (min) => `At least ${min}.`,
      }),
    ).toBe("At least 8.");
  });

  it("accepts input that satisfies the minimum", () => {
    expect(trimmedTextError("12345678", { minLength: 8 })).toBeUndefined();
  });
});

describe("boundedReasonError", () => {
  it("uses bounded-reason copy for empty and short values", () => {
    expect(boundedReasonError("")).toBe("Enter a bounded reason.");
    expect(boundedReasonError("a a")).toBe("Enter at least 8 characters.");
    expect(boundedReasonError("policy mismatch on criterion 2")).toBeUndefined();
  });
});

describe("clearValidationErrorOnValid", () => {
  it("clears a visible error once validation passes", () => {
    expect(clearValidationErrorOnValid("Enter a bounded reason.", "valid reason text", boundedReasonError)).toBe("");
    expect(clearValidationErrorOnValid("", "still short", boundedReasonError)).toBe("");
    expect(clearValidationErrorOnValid("Enter at least 8 characters.", "short", boundedReasonError)).toBe(
      "Enter at least 8 characters.",
    );
  });
});
