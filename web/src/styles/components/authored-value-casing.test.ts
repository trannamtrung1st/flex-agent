import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const css = (name: string) => readFileSync(join(here, name), "utf8");

describe("authored value casing", () => {
  it("keeps field-select triggers and option rows from forcing uppercase", () => {
    const menus = css("menus.css");
    const searchable = css("searchable.css");
    expect(menus).toMatch(/\.dropdown-key \{[^}]*text-transform:\s*none/);
    expect(menus).not.toMatch(/\.dropdown-key \{[^}]*text-transform:\s*uppercase/);
    expect(menus).toMatch(/\.option-menu li \{[^}]*text-transform:\s*none/);
    expect(menus).toMatch(/\.menu-row,\s*\n\.command-menu-item \{[^}]*text-transform:\s*uppercase/);
    expect(searchable).toMatch(/\.searchable-select-options li \{[^}]*text-transform:\s*none/);
    expect(searchable).toMatch(/\.searchable-disclosure-options li \{[^}]*text-transform:\s*none/);
    expect(searchable).toMatch(/\.multiselect-option \{[^}]*text-transform:\s*none/);
  });

  it("keeps registry search queries from forcing uppercase", () => {
    const datatable = css("datatable.css");
    expect(datatable).toMatch(/\.seg-search \{[^}]*text-transform:\s*none/);
    expect(datatable).not.toMatch(/\.seg-search \{[^}]*text-transform:\s*uppercase/);
  });
});
