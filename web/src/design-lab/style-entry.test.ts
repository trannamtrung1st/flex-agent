import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const labRoot = dirname(fileURLToPath(import.meta.url));
const stylesRoot = join(labRoot, "../styles");

describe("design-lab style entry graph", () => {
  it("composes shared sheets plus demo and surface CSS", () => {
    const main = readFileSync(join(labRoot, "main.tsx"), "utf8");
    expect(main).toContain('import "../styles/design-lab.css"');
    expect(main).not.toContain("styles/index.css");

    const labCss = readFileSync(join(stylesRoot, "design-lab.css"), "utf8");
    expect(labCss).toContain('@import "./shared.css"');
    expect(labCss).toContain('@import "./components/demo.css"');
    expect(labCss).toContain('@import "./surfaces/gallery.css"');
    expect(labCss).toContain('@import "./surfaces/surfaces-index.css"');
  });
});
