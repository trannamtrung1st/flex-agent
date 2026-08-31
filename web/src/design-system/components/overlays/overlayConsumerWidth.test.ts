import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const select = join(here, "..", "select");
const menu = join(here, "..", "menu", "DropdownMenu.tsx");
const temporal = join(here, "..", "temporal", "DateTimePicker.tsx");
const keys = join(here, "..", "keys", "TooltipHost.tsx");

describe("anchored overlay width wiring", () => {
  it("keeps each select shell on the shared placement helper with the matching stretch/lock recipe", () => {
    const fieldSearch = readFileSync(join(select, "SearchableDropdownSelect.tsx"), "utf8");
    const fieldMulti = readFileSync(join(select, "SearchableMultiSelect.tsx"), "utf8");
    const disclosure = readFileSync(join(select, "SearchableDisclosureMenu.tsx"), "utf8");
    const listbox = readFileSync(join(select, "listboxMenus.tsx"), "utf8");
    const command = readFileSync(menu, "utf8");
    const datetime = readFileSync(temporal, "utf8");
    const plaque = readFileSync(keys, "utf8");

    expect(fieldSearch).toMatch(/align="stretch"/);
    expect(fieldMulti).toMatch(/align="stretch"/);
    expect(listbox).toMatch(/align=\{isToolbar \? "start" : "stretch"\}/);
    expect(disclosure).toMatch(/<AnchoredOverlay open=\{open\} triggerRef=\{rootRef\} tokenSourceRef=\{rootRef\} floatingRef=\{panelRef\}>/);
    expect(disclosure).not.toMatch(/align="stretch"/);
    expect(command).toMatch(/tokenSourceRef=\{shellRef\}/);
    expect(datetime).toMatch(/lockMinWidthToTrigger=\{false\}/);
    expect(plaque).toMatch(/lockMinWidthToTrigger: false/);
  });
});
