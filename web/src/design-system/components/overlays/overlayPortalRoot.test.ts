import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { overlayPortalRoot } from "./overlayPortalRoot";

describe("overlayPortalRoot", () => {
  it("uses the enclosing dialog when the host is inside one", () => {
    const dialog = document.createElement("dialog");
    const host = document.createElement("span");
    dialog.append(host);
    document.body.append(dialog);
    expect(overlayPortalRoot(host)).toBe(dialog);
    dialog.remove();
  });

  it("falls back to document.body outside a dialog", () => {
    const host = document.createElement("span");
    document.body.append(host);
    expect(overlayPortalRoot(host)).toBe(document.body);
    host.remove();
  });
});

describe("useFloatingPlacement viewport binding", () => {
  it("reads visualViewport once in the measure closure", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const source = readFileSync(join(here, "AnchoredOverlay.tsx"), "utf8");
    expect(source.match(/const visualViewport = window\.visualViewport/g)).toEqual([
      "const visualViewport = window.visualViewport",
    ]);
    expect(source).not.toMatch(/const visual = window\.visualViewport/);
  });
});
