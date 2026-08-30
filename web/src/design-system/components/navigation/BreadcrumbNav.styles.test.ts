import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const cssPath = resolve(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/navigation.css");

describe("breadcrumb trail styles", () => {
  it("suppresses underline on crumb links in every interactive state", () => {
    const css = readFileSync(cssPath, "utf8");
    const rest = css.match(/\.breadcrumb-nav a(?:\s*,\s*\.breadcrumb-nav a:visited)?\s*\{[^}]+\}/);
    expect(rest?.[0]).toMatch(/text-decoration:\s*none/);

    const hover = css.match(/\.breadcrumb-nav a:hover[\s\S]*?\{[^}]+\}/);
    expect(hover?.[0]).toMatch(/text-decoration:\s*none/);
  });

  it("keeps shell inset off the presentational primitive", () => {
    const css = readFileSync(cssPath, "utf8");
    const block = css.match(/\.breadcrumb-nav\s*\{[^}]+\}/);
    expect(block?.[0]).not.toMatch(/shell-main-inset/);
  });
});
