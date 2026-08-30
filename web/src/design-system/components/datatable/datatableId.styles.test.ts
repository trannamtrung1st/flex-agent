import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const cssPath = resolve(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/datatable.css");

describe("datatable identifier styles", () => {
  it("suppresses underline so Link identifiers match Design Lab buttons", () => {
    const css = readFileSync(cssPath, "utf8");
    const block = css.match(/\.datatable-id\s*\{[^}]+\}/);
    expect(block?.[0]).toMatch(/text-decoration:\s*none/);
  });
});
