import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const styles = join(dirname(fileURLToPath(import.meta.url)), "../../../styles");
const platesCss = readFileSync(join(styles, "components/plates.css"), "utf8");
const appShellCss = readFileSync(join(styles, "app-shell.css"), "utf8");
const aliasesCss = readFileSync(join(styles, "semantic-aliases.css"), "utf8");
const briefingCss = readFileSync(join(styles, "surfaces/participant-session.css"), "utf8");

function rule(css: string, selector: string): string {
  const escaped = selector.replaceAll(".", "\\.");
  const match = css.match(new RegExp(`${escaped}\\s*\\{([^}]+)\\}`));
  if (!match?.[1]) {
    throw new Error(`Rule not found: ${selector}`);
  }
  return match[1];
}

describe("prose section marks", () => {
  it("keeps work-well section labels free of a leading instrument tick", () => {
    expect(platesCss).not.toMatch(/\.work-well__section h3::before/);
    expect(rule(platesCss, ".work-well__section h3")).toMatch(/color:\s*var\(--teal\)/);
    expect(rule(platesCss, ".work-well__section h3")).toMatch(/margin-bottom:\s*var\(--field-label-gap\)/);
    expect(rule(platesCss, ".work-well__head")).toMatch(/var\(--frame-inset-block-end\)/);
    expect(rule(platesCss, ".work-well__head")).not.toMatch(/18px/);
    expect(rule(briefingCss, ".briefing-sec h2")).toMatch(/margin-bottom:\s*var\(--field-label-gap\)/);
    expect(rule(briefingCss, ".briefing-sec")).toMatch(/margin-bottom:\s*var\(--operate-bay-gap\)/);
  });

  it("uses a 7×1px teal tick only on prose unordered lists in work wells", () => {
    expect(platesCss).not.toMatch(/\.work-well__section li::before/);
    expect(platesCss).toMatch(
      /\.work-well__section > ul:not\(\.intake-item-list\) > li::before\s*\{[^}]*width:\s*7px/,
    );
    expect(platesCss).toMatch(
      /\.work-well__section > ul:not\(\.intake-item-list\) > li::before\s*\{[^}]*background:\s*var\(--teal\)/,
    );
  });

  it("keeps ordered work-well lists on the same row gap and inset without ticks", () => {
    expect(platesCss).toMatch(
      /\.work-well__section > ul:not\(\.intake-item-list\),\s*\n\.work-well__section > ol\s*\{[^}]*gap:\s*var\(--space-2\)/,
    );
    expect(platesCss).toMatch(/\.work-well__section > ol\s*\{[^}]*counter-reset:\s*work-well-ol/);
    expect(platesCss).toMatch(
      /\.work-well__section > ol > li\s*\{[^}]*grid-template-columns:\s*var\(--space-6\)/,
    );
    expect(platesCss).toMatch(
      /\.work-well__section > ol > li::before\s*\{[^}]*content:\s*counter\(work-well-ol\)/,
    );
    expect(platesCss).toMatch(
      /\.work-well__section > ol > li\[data-sequence\]::before\s*\{[^}]*content:\s*attr\(data-sequence\)/,
    );
    expect(platesCss).not.toMatch(/\.work-well__section > ol > li::before\s*\{[^}]*width:\s*7px/);
  });

  it("limits prose measure to prose list items, not structured intake rows", () => {
    expect(platesCss).not.toMatch(/\.work-well__section li\s*\{/);
    expect(platesCss).not.toMatch(/\.work-well__section > ul > li\s*\{/);
    expect(platesCss).toMatch(
      /\.work-well__section > ul:not\(\.intake-item-list\) > li\s*\{[^}]*max-width:\s*var\(--content-width-prose\)/,
    );
    expect(platesCss).not.toMatch(/\.intake-item-row[^}]*max-width/);
  });

  it("sizes structured assignment lists to the shared column measure token", () => {
    expect(aliasesCss).toMatch(/--content-width-structured:\s*36rem/);
    expect(appShellCss).toMatch(
      /\.intake-item-list\s*\{[^}]*width:\s*min\(100%,\s*var\(--content-width-structured\)\)/,
    );
  });

  it("does not keep a dedicated version-list row layout", () => {
    const journeyCss = readFileSync(join(styles, "surfaces/participant-journey.css"), "utf8");
    for (const css of [appShellCss, journeyCss, platesCss]) {
      expect(css).not.toMatch(/\.version-list/);
      expect(css).not.toMatch(/\.version-row/);
    }
  });

  it("keeps session briefing section labels free of a leading instrument tick", () => {
    expect(briefingCss).not.toMatch(/\.briefing-sec h2::before/);
    expect(rule(briefingCss, ".briefing-sec h2")).toMatch(/color:\s*var\(--teal\)/);
  });

  it("uses the same unordered-list tick on session briefing prose", () => {
    expect(briefingCss).not.toMatch(/\.briefing-sec li::before\s*\{/);
    const tick = rule(briefingCss, ".briefing-sec ul li::before");
    expect(tick).toMatch(/width:\s*7px/);
    expect(tick).toMatch(/height:\s*1px/);
    expect(tick).toMatch(/background:\s*var\(--teal\)/);
  });
});
