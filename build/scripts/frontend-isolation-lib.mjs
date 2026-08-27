import path from "node:path";

const DESIGN_LAB_SEGMENT = /(?:^|\/)design-lab(?:\/|$)/;

const SPECIFIER_PATTERNS = [
  /(?:import|export)\s+(?:type\s+)?(?:[\s\S]*?\sfrom\s+)?["']([^"']+)["']/g,
  /import\s*\(\s*["']([^"']+)["']\s*\)/g,
  /require\s*\(\s*["']([^"']+)["']\s*\)/g,
  /@import\s+(?:url\(\s*)?["']([^"']+)["']/g,
];

export function extractImportSpecifiers(content) {
  const specifiers = [];
  for (const pattern of SPECIFIER_PATTERNS) {
    pattern.lastIndex = 0;
    let match;
    while ((match = pattern.exec(content)) !== null) {
      specifiers.push(match[1]);
    }
  }
  return specifiers;
}

export function specifierResolvesToDesignLab(fromFile, specifier) {
  const normalized = specifier.replaceAll("\\", "/");
  if (normalized.startsWith(".") || path.isAbsolute(specifier)) {
    const resolved = path.normalize(path.join(path.dirname(fromFile), specifier)).replaceAll("\\", "/");
    return DESIGN_LAB_SEGMENT.test(resolved);
  }
  return DESIGN_LAB_SEGMENT.test(normalized);
}

export function designLabImportViolations(fromFile, content) {
  return extractImportSpecifiers(content)
    .filter((specifier) => specifierResolvesToDesignLab(fromFile, specifier))
    .map((specifier) => `${fromFile} imports '${specifier}'`);
}
