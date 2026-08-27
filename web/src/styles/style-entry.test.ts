import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const srcRoot = join(dirname(fileURLToPath(import.meta.url)), "..");

describe("candidate style entry graph", () => {
  it("loads shared Shipboard sheets without lab-only demo or surface CSS", () => {
    const main = readFileSync(join(srcRoot, "main.tsx"), "utf8");
    expect(main).toContain('import "./styles/shared.css"');
    expect(main).not.toContain("styles/index.css");

    const shared = readFileSync(join(srcRoot, "styles/shared.css"), "utf8");
    expect(shared).toContain('@import "@fontsource/michroma"');
    expect(shared).toContain('@import "./tokens.css"');
    expect(shared).not.toContain("demo.css");
    expect(shared).not.toContain("./surfaces/");
  });

  it("keeps demo-plate selectors out of shared component family sheets", () => {
    const componentsDir = join(srcRoot, "styles/components");
    for (const name of readdirSync(componentsDir)) {
      if (name === "demo.css" || !name.endsWith(".css")) continue;
      const css = readFileSync(join(componentsDir, name), "utf8");
      expect(css, name).not.toContain(".demo-plate");
    }
  });

  it("does not keep a combined styles index the candidate can import", () => {
    expect(existsSync(join(srcRoot, "styles/index.css"))).toBe(false);
  });
});
