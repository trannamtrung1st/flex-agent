import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, normalize, relative } from "node:path";
import { fileURLToPath } from "node:url";

const srcRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const designLabRoot = join(srcRoot, "design-lab");

const importSpecifierPattern =
  /(?:import|export)\s+(?:type\s+)?(?:[\s\S]*?\sfrom\s+)?["']([^"']+)["']|import\s*\(\s*["']([^"']+)["']\s*\)|@import\s+(?:url\(\s*)?["']([^"']+)["']/g;
const labOwnedStylesheetPattern = /\/styles\/(?:design-lab\.css|components\/demo\.css|surfaces\/)/;

function extractImportSpecifiers(content: string): string[] {
  const specifiers: string[] = [];
  for (const match of content.matchAll(importSpecifierPattern)) {
    const specifier = match[1] || match[2] || match[3];
    if (specifier) {
      specifiers.push(specifier);
    }
  }
  return specifiers;
}

function resolvesToLabOwnedStylesheet(fromFile: string, specifier: string): boolean {
  if (!specifier.startsWith(".")) {
    return false;
  }
  const resolved = normalize(join(dirname(fromFile), specifier)).replaceAll("\\", "/");
  return labOwnedStylesheetPattern.test(resolved);
}

function isLabOwnedStylesheetPath(file: string): boolean {
  const rel = relative(srcRoot, file).replaceAll("\\", "/");
  return (
    rel === "styles/design-lab.css"
    || rel === "styles/components/demo.css"
    || rel.startsWith("styles/surfaces/")
  );
}

function walkCandidateSources(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const fullPath = join(directory, entry);
    if (statSync(fullPath).isDirectory()) {
      if (fullPath === designLabRoot) {
        return [];
      }
      return walkCandidateSources(fullPath);
    }
    if (/\.(ts|tsx|js|jsx|css)$/.test(entry) && !isLabOwnedStylesheetPath(fullPath)) {
      return [fullPath];
    }
    return [];
  });
}

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

  it("rejects direct lab-owned stylesheet imports anywhere in the candidate graph", () => {
    const violations = walkCandidateSources(srcRoot).flatMap((file) => {
      const content = readFileSync(file, "utf8");
      return extractImportSpecifiers(content)
        .filter((specifier) => resolvesToLabOwnedStylesheet(file, specifier))
        .map((specifier) => `${relative(srcRoot, file)} imports lab-owned stylesheet '${specifier}'`);
    });
    expect(violations).toEqual([]);
  });
});
