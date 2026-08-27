import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const layoutCss = join(dirname(fileURLToPath(import.meta.url)), "../styles/components/layouts.css");

function rule(css: string, selector: string): string {
  const escaped = selector.replaceAll(".", "\\.");
  const match = css.match(new RegExp(`${escaped}\\s*\\{([^}]+)\\}`));
  if (!match?.[1]) {
    throw new Error(`Rule not found: ${selector}`);
  }
  return match[1];
}

function narrowSessionBlock(css: string): string {
  const match = css.match(/@media \(max-width: 760px\)[\s\S]*?(?=@media|$)/);
  if (!match?.[0]) {
    throw new Error("Narrow session media block not found");
  }
  return match[0];
}

describe("participant instrument bulkheads", () => {
  it("seats the assignment station rail flush to the hull on desktop", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(rule(css, ".layout-guided")).toMatch(/padding:\s*0 22px 0 0/);
    expect(rule(css, ".layout-guided")).toMatch(/height:\s*100dvh/);
    expect(rule(css, ".layout-guided")).toMatch(/grid-template-rows:\s*minmax\(0, 1fr\)/);
    expect(rule(css, ".layout-guided")).not.toMatch(/min-height:\s*620px/);
    expect(rule(css, ".layout-guided__bay")).toMatch(/padding:\s*18px 0/);
    expect(rule(css, ".layout-guided__main")).toMatch(/flex:\s*1 1 auto/);
    expect(rule(css, ".layout-guided .phase-rail")).toMatch(/--instrument-bulkhead-fill/);
  });

  it("seats the examination console rail flush to the hull on desktop", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(rule(css, ".layout-session")).toMatch(/padding:\s*0 20px 0 0/);
    expect(rule(css, ".layout-session")).toMatch(/height:\s*100dvh/);
    expect(rule(css, ".layout-session")).not.toMatch(/min-height:\s*620px/);
    expect(rule(css, ".layout-session__bay")).toMatch(/padding:\s*18px 0/);
    expect(rule(css, ".layout-session .rail")).toMatch(/--instrument-bulkhead-fill/);
  });

  it("scrolls assignment rail content without shrinking the brand header", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(css).toMatch(/\.phase-rail\s*>\s*\*:not\(\.phase-rail-scroll\)/);
    expect(rule(css, ".layout-guided .phase-rail")).toMatch(/overflow-y:\s*hidden/);
    expect(rule(css, ".layout-guided .phase-rail-scroll")).toMatch(/overflow-y:\s*auto/);
    expect(rule(css, ".layout-guided .phase-rail-scroll")).toMatch(/flex:\s*1 1 auto/);
  });

  it("scrolls session rail content without moving the brand header", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(css).toMatch(/\.rail\s*>\s*\*:not\(\.rail-scroll\)/);
    expect(rule(css, ".layout-session .rail")).toMatch(/overflow-y:\s*hidden/);
    expect(rule(css, ".layout-session .rail-scroll")).toMatch(/overflow-y:\s*auto/);
    expect(rule(css, ".layout-session .rail-scroll")).toMatch(/flex:\s*1 1 auto/);
  });

  it("docks assignment and session rail scrollports to the bulkhead hairline", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(rule(css, ".layout-guided .phase-rail")).toMatch(
      /padding:\s*18px 0 16px var\(--shell-main-inset-inline\)/,
    );
    expect(rule(css, ".layout-guided .phase-rail-scroll")).toMatch(
      /padding-right:\s*var\(--instrument-rail-dock-inline\)/,
    );
    expect(rule(css, ".layout-guided .phase-rail-scroll")).not.toMatch(/scrollbar-gutter/);
    expect(rule(css, ".layout-session .rail")).toMatch(
      /padding:\s*18px 0 16px var\(--shell-main-inset-inline\)/,
    );
    expect(rule(css, ".layout-session .rail-scroll")).toMatch(
      /padding-right:\s*var\(--instrument-rail-dock-inline\)/,
    );
    expect(rule(css, ".layout-session .rail-scroll")).not.toMatch(/scrollbar-gutter/);
  });

  it("enables page scroll on narrow session layout for 400% zoom reflow", () => {
    const css = readFileSync(layoutCss, "utf8");
    const narrow = narrowSessionBlock(css);
    expect(narrow).toMatch(/body\s*\{[^}]*overflow:\s*auto/);
    expect(narrow).toMatch(/\.layout-session\s*\{[^}]*height:\s*auto/);
    expect(narrow).toMatch(/\.layout-session\s*\{[^}]*min-height:\s*100dvh/);
    expect(narrow).toMatch(/\.layout-session\s*\{[^}]*overflow:\s*visible/);
    expect(narrow).not.toMatch(/\sheight:\s*100dvh;/);
    expect(narrow).toMatch(/grid-template-rows:\s*auto auto auto/);
  });

  it("stretches the assignment briefing plate through the guided-task well", () => {
    const platesCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../styles/components/plates.css"),
      "utf8",
    );
    expect(platesCss).toMatch(
      /\.layout-guided__main\s*>\s*\.work-well\s*\{[^}]*flex:\s*1 1 auto/,
    );
    expect(platesCss).toMatch(
      /\.layout-guided__main\s*>\s*\.work-well\s*\{[^}]*width:\s*100%/,
    );
    expect(platesCss).toMatch(
      /\.layout-guided__main\s*>\s*\.work-well\s*\{[^}]*max-width:\s*none/,
    );
    expect(platesCss).toMatch(/\.work-well\s*>\s*\.work-well__foot\s*\{[^}]*margin-top:\s*auto/);
    expect(platesCss).toMatch(/\.work-well\s*>\s*\.work-well__foot\s*\{[^}]*padding-block-end:\s*var\(--frame-inset-block-end\)/);
  });

  it("stacks the guided-task instrument band at 1080px", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(css).toMatch(/@media \(max-width: 1080px\)[\s\S]*\.layout-guided\s*\{[\s\S]*grid-template-rows:\s*auto minmax\(0, 1fr\)/);
    expect(css).toMatch(/@media \(max-width: 1080px\)[\s\S]*\.layout-guided \.phase-rail-scroll\s*\{[^}]*display:\s*contents/);
  });

  it("stacks live-session at 1180px before the 760px page reflow", () => {
    const css = readFileSync(layoutCss, "utf8");
    const block = css.match(/@media \(max-width: 1180px\)[\s\S]*?(?=@media|$)/)?.[0];
    expect(block).toBeTruthy();
    expect(block).toMatch(/grid-template-areas:\s*"railband\s+railband"/);
    expect(block).toMatch(/\.layout-session \.rail-scroll\s*\{[^}]*display:\s*contents/);
  });

  it("owns print, forced-colors, and reduced-motion layout contracts", () => {
    const css = readFileSync(layoutCss, "utf8");
    expect(css).toMatch(/@media print/);
    expect(css).toMatch(/forced-colors:\s*active/);
    expect(css).toMatch(/prefers-reduced-motion:\s*reduce/);
  });
});
