import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { overlayPlateClass, OVERLAY_PLATE_CLASS, OVERLAY_PLATE_OFFSET } from "./overlayPlate";

const here = dirname(fileURLToPath(import.meta.url));
const componentsRoot = join(here, "..");

const overlayConsumers = [
  "select/listboxMenus.tsx",
  "select/SearchableDropdownSelect.tsx",
  "select/SearchableMultiSelect.tsx",
  "select/SearchableDisclosureMenu.tsx",
  "menu/DropdownMenu.tsx",
  "temporal/DateTimePicker.tsx",
] as const;

describe("overlayPlateClass", () => {
  it("always includes the closed select/menu plate grammar", () => {
    expect(OVERLAY_PLATE_CLASS).toBe("select-popover popover-surface menu-surface");
    expect(OVERLAY_PLATE_OFFSET).toBe(-1);
    expect(overlayPlateClass("dropdown-menu", "floating-overlay")).toBe(
      "select-popover popover-surface menu-surface dropdown-menu floating-overlay",
    );
  });

  it("seats plate overlays with a 1px overlap on the open axis", () => {
    const source = readFileSync(join(here, "AnchoredOverlay.tsx"), "utf8");
    expect(source).toMatch(/offset = OVERLAY_PLATE_OFFSET/);
  });

  it("is the plate class helper for every portaled select, menu, and datetime overlay", () => {
    for (const file of overlayConsumers) {
      const source = readFileSync(join(componentsRoot, file), "utf8");
      expect(source, file).toMatch(/overlayPlateClass\(/);
      expect(source, file).not.toMatch(/select-popover popover-surface menu-surface/);
    }
  });

  it("does not compensate overlay width or x-offset for a fused trigger seam", () => {
    const tokens = readFileSync(join(here, "../../../styles/tokens.css"), "utf8");
    expect(tokens).toMatch(/--select-popover-offset-x-context:\s*0;/);
    expect(tokens).toMatch(/--select-popover-offset-x-toolbar:\s*0;/);
    expect(tokens).toMatch(/--select-popover-min-width-context:\s*100%;/);
    expect(tokens).toMatch(/--select-popover-min-width-toolbar:\s*max\(100%, 16rem\);/);
    expect(tokens).toMatch(/--select-popover-min-width-toolbar-foot:\s*max\(100%, 148px\);/);
    expect(tokens).not.toMatch(/calc\(100% \+ 2px\)/);
  });
});
