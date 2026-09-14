import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles");
const css = readFileSync(join(stylesRoot, "components/work-plates.css"), "utf8");

describe("review plates layout contracts", () => {
  it("keeps Evidence provenance readable above the guided-task foot", () => {
    expect(css).toMatch(
      /\.layout-guided__bay:has\(\.layout-guided__actions\)[\s\S]*scroll-padding-block-end/,
    );
    expect(css).toMatch(
      /@media \(max-width: 1080px\)[\s\S]*padding-block-end:\s*calc\(var\(--frame-inset-block-end\)/,
    );
  });

  it("gives compact criterion SplitBay drawer a scrollable main column", () => {
    expect(css).toMatch(
      /\.review-criterion-split\[data-flow-split="drawer"\][\s\S]*min-height:\s*0/,
    );
    expect(css).toMatch(
      /\.review-criterion-split\[data-flow-split="drawer"\] > \.composition-split__main[\s\S]*display:\s*flex/,
    );
  });

  it("owns reduced-motion and forced-colors contracts on criterion nav", () => {
    expect(css).toMatch(/prefers-reduced-motion:\s*reduce/);
    expect(css).toMatch(/forced-colors:\s*active/);
    expect(css).toMatch(/\.review-criterion-nav \.nav-link\[aria-current="page"\]/);
  });
});
