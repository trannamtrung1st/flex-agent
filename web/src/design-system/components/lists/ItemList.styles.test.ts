import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const cssPath = resolve(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/lists.css");

describe("ItemList styles", () => {
  it("owns a single group-rung gutter on rows and Load more", () => {
    const css = readFileSync(cssPath, "utf8");
    const block = css.match(/\.item-list__item,\s*\.item-list__more\s*\{[^}]+\}/);
    expect(block?.[0]).toMatch(/padding-inline:\s*var\(--space-4\)/);
    expect(block?.[0]).toMatch(/padding-block:\s*var\(--space-4\)/);
  });

  it("stretches the Load more key across the trailing row", () => {
    const css = readFileSync(cssPath, "utf8");
    expect(css).toMatch(/\.item-list__more \.tip-host[\s\S]*?width:\s*100%/);
    expect(css).toMatch(/\.item-list__more-key[\s\S]*?width:\s*100%/);
    expect(css).toMatch(/\.item-list__more-key[\s\S]*?justify-content:\s*center/);
  });

  it("centers the end-trigger waiting panel in the trailing row", () => {
    const css = readFileSync(cssPath, "utf8");
    const block = css.match(/\.item-list__end\.is-waiting\s*\{[^}]+\}/);
    expect(block?.[0]).toMatch(/display:\s*flex/);
    expect(block?.[0]).toMatch(/justify-content:\s*center/);
  });
});
