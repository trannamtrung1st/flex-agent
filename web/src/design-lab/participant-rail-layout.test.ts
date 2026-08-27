import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles/surfaces");

function desktopRule(css: string, selector: string): string {
  const desktop = css.split(/@media/)[0] ?? css;
  const escaped = selector.replaceAll(".", "\\.");
  const match = desktop.match(new RegExp(`${escaped}\\s*\\{([^}]+)\\}`));
  if (!match?.[1]) {
    throw new Error(`Desktop rule not found: ${selector}`);
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
    const css = readFileSync(join(stylesRoot, "participant-journey.css"), "utf8");
    expect(desktopRule(css, ".station")).toMatch(/padding:\s*0 22px 0 0/);
    expect(desktopRule(css, ".station")).toMatch(/height:\s*100dvh/);
    expect(desktopRule(css, ".station")).not.toMatch(/min-height:\s*620px/);
    expect(desktopRule(css, ".station-main")).toMatch(/padding:\s*18px 0/);
    expect(desktopRule(css, ".phase-rail")).toMatch(/--instrument-bulkhead-fill/);
  });

  it("seats the examination console rail flush to the hull on desktop", () => {
    const css = readFileSync(join(stylesRoot, "participant-session.css"), "utf8");
    expect(desktopRule(css, ".console")).toMatch(/padding:\s*0 20px 0 0/);
    expect(desktopRule(css, ".console")).toMatch(/height:\s*100dvh/);
    expect(desktopRule(css, ".console")).not.toMatch(/min-height:\s*620px/);
    expect(desktopRule(css, ".session-main")).toMatch(/padding:\s*18px 0/);
    expect(desktopRule(css, ".rail")).toMatch(/--instrument-bulkhead-fill/);
  });

  it("scrolls assignment rail content without shrinking the brand header", () => {
    const css = readFileSync(join(stylesRoot, "participant-journey.css"), "utf8");
    expect(css).toMatch(/\.phase-rail\s*>\s*\*:not\(\.phase-rail-scroll\)/);
    expect(desktopRule(css, ".phase-rail")).toMatch(/overflow-y:\s*hidden/);
    expect(desktopRule(css, ".phase-rail-scroll")).toMatch(/overflow-y:\s*auto/);
    expect(desktopRule(css, ".phase-rail-scroll")).toMatch(/flex:\s*1 1 auto/);
  });

  it("scrolls session rail content without moving the brand header", () => {
    const css = readFileSync(join(stylesRoot, "participant-session.css"), "utf8");
    expect(css).toMatch(/\.rail\s*>\s*\*:not\(\.rail-scroll\)/);
    expect(desktopRule(css, ".rail")).toMatch(/overflow-y:\s*hidden/);
    expect(desktopRule(css, ".rail-scroll")).toMatch(/overflow-y:\s*auto/);
    expect(desktopRule(css, ".rail-scroll")).toMatch(/flex:\s*1 1 auto/);
  });

  it("docks assignment and session rail scrollports to the bulkhead hairline", () => {
    const assignment = readFileSync(join(stylesRoot, "participant-journey.css"), "utf8");
    expect(desktopRule(assignment, ".phase-rail")).toMatch(/padding:\s*18px 0 16px 26px/);
    expect(desktopRule(assignment, ".phase-rail-scroll")).toMatch(/padding-right:\s*18px/);
    expect(desktopRule(assignment, ".phase-rail-scroll")).not.toMatch(/scrollbar-gutter/);

    const session = readFileSync(join(stylesRoot, "participant-session.css"), "utf8");
    expect(desktopRule(session, ".rail")).toMatch(/padding:\s*18px 0 16px 26px/);
    expect(desktopRule(session, ".rail-scroll")).toMatch(/padding-right:\s*18px/);
    expect(desktopRule(session, ".rail-scroll")).not.toMatch(/scrollbar-gutter/);
  });

  it("enables page scroll on narrow session layout for 400% zoom reflow", () => {
    const css = readFileSync(join(stylesRoot, "participant-session.css"), "utf8");
    const narrow = narrowSessionBlock(css);
    expect(narrow).toMatch(/body\s*\{[^}]*overflow:\s*auto/);
    expect(narrow).toMatch(/\.console\s*\{[^}]*height:\s*auto/);
    expect(narrow).toMatch(/\.console\s*\{[^}]*min-height:\s*100dvh/);
    expect(narrow).toMatch(/\.console\s*\{[^}]*overflow:\s*visible/);
    expect(narrow).not.toMatch(/\sheight:\s*100dvh;/);
    expect(narrow).toMatch(/grid-template-rows:\s*auto auto auto/);
  });
});
