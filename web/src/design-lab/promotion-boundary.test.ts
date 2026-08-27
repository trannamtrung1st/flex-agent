import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { designLabImportViolations, designLabOutboundImportViolations } from "../../../build/scripts/frontend-isolation-lib.mjs";
import { DESIGN_LAB_BASENAME, designLabRouter } from "./app/router";

const designLabRoot = dirname(fileURLToPath(import.meta.url));
const srcRoot = join(designLabRoot, "..");
const repoRoot = join(srcRoot, "../..");
const designSystemRoot = join(srcRoot, "design-system");

function walk(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const fullPath = join(directory, entry);
    if (statSync(fullPath).isDirectory()) {
      return walk(fullPath);
    }
    return [fullPath];
  });
}

describe("Phase 7.5 design-lab promotion boundary", () => {
  it("uses /design-lab as the only lab route namespace", () => {
    expect(DESIGN_LAB_BASENAME).toBe("/design-lab");
    expect(designLabRouter.basename).toBe("/design-lab");
    const routerSource = readFileSync(join(designLabRoot, "app/router.tsx"), "utf8");
    expect(routerSource).not.toContain("/prototypes");
  });

  it("owns promoted modules under design-system foundations, components, and patterns", () => {
    for (const folder of ["foundations", "components", "patterns"]) {
      expect(existsSync(join(designSystemRoot, folder)), folder).toBe(true);
    }
    expect(existsSync(join(srcRoot, "lib/cx.ts"))).toBe(true);
    expect(existsSync(join(designSystemRoot, "components/keys/Key.tsx"))).toBe(true);
    expect(existsSync(join(designSystemRoot, "patterns/TableActions.tsx"))).toBe(true);
  });

  it("lets the design lab import promoted modules and keeps those modules free of lab/fixture paths", () => {
    const labSources = walk(designLabRoot).filter((path) => /\.(ts|tsx)$/.test(path));
    const labImportsDesignSystem = labSources.some((path) =>
      readFileSync(path, "utf8").includes("../design-system/"),
    );
    expect(labImportsDesignSystem).toBe(true);

    const promotedSources = [
      ...walk(designSystemRoot),
      ...walk(join(srcRoot, "lib")),
    ].filter((path) => /\.(ts|tsx)$/.test(path));

    const violations = promotedSources.flatMap((path) => {
      const content = readFileSync(path, "utf8");
      const relativePath = relative(repoRoot, path);
      const importHits = designLabImportViolations(path, content).map(
        (violation) => `${relativePath} ${violation.slice(path.length).trim()}`,
      );
      const hits = [
        [".work", "resources"].join("/"),
        ["impeccable", "prototype"].join("-"),
      ].filter((needle) => content.includes(needle));
      return [...importHits, ...hits.map((needle) => `${relativePath} contains '${needle}'`)];
    });
    expect(violations).toEqual([]);
  });

  it("keeps the design lab from importing future production modules", () => {
    const labSources = walk(designLabRoot).filter(
      (path) => /\.(ts|tsx)$/.test(path) && !/\.(test|spec)\.(ts|tsx)$/.test(path),
    );
    const violations = labSources.flatMap((path) => {
      const content = readFileSync(path, "utf8");
      return designLabOutboundImportViolations(path, content, repoRoot).map(
        (violation) => `${relative(repoRoot, path)} ${violation.slice(path.length + 1)}`,
      );
    });
    expect(violations).toEqual([]);
  });
});
