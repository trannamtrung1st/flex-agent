import { describe, expect, it } from "vitest";
import { enabledMenuItems, stepMenuIndex } from "./dropdownMenuLogic";

describe("enabledMenuItems", () => {
  it("skips native-disabled and aria-disabled items", () => {
    const root = document.createElement("div");
    root.innerHTML = `
      <button role="menuitem">One</button>
      <button role="menuitem" disabled>Two</button>
      <button role="menuitem" aria-disabled="true">Three</button>
      <button role="menuitem">Four</button>
    `;
    expect(enabledMenuItems(root).map((el) => el.textContent)).toEqual(["One", "Four"]);
  });
});

describe("stepMenuIndex", () => {
  it("wraps and treats a missing current as the start of the step", () => {
    expect(stepMenuIndex(3, 2, 1)).toBe(0);
    expect(stepMenuIndex(3, 0, -1)).toBe(2);
    expect(stepMenuIndex(3, -1, 1)).toBe(0);
    expect(stepMenuIndex(3, -1, -1)).toBe(2);
    expect(stepMenuIndex(0, 0, 1)).toBe(-1);
  });
});
