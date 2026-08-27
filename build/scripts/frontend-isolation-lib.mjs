import path from "node:path";

const DESIGN_LAB_SEGMENT = /(?:^|\/)design-lab(?:\/|$)/;
const WEB_SRC_SEGMENT = /(?:^|\/)web\/src\//;

const LAB_OWNED_STYLESHEET = /(?:^|\/)styles\/(?:design-lab\.css|components\/demo\.css|surfaces\/)/;

const DESIGN_LAB_OUTBOUND_ALLOW = [
  /(?:^|\/)web\/src\/design-lab\//,
  /(?:^|\/)web\/src\/design-system\//,
  /(?:^|\/)web\/src\/lib\//,
  /(?:^|\/)web\/src\/styles\//,
];

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

function normalizeResolvedPath(fromFile, specifier) {
  const normalized = specifier.replaceAll("\\", "/");
  if (normalized.startsWith(".") || path.isAbsolute(specifier)) {
    return path.normalize(path.join(path.dirname(fromFile), specifier)).replaceAll("\\", "/");
  }
  return normalized;
}

function isExternalPackageSpecifier(specifier) {
  return !specifier.startsWith(".") && !path.isAbsolute(specifier);
}

export function specifierResolvesToDesignLab(fromFile, specifier) {
  if (isExternalPackageSpecifier(specifier)) {
    return DESIGN_LAB_SEGMENT.test(specifier.replaceAll("\\", "/"));
  }
  return DESIGN_LAB_SEGMENT.test(normalizeResolvedPath(fromFile, specifier));
}

export function specifierResolvesToLabOwnedStylesheet(fromFile, specifier) {
  if (isExternalPackageSpecifier(specifier)) {
    return false;
  }
  return LAB_OWNED_STYLESHEET.test(normalizeResolvedPath(fromFile, specifier));
}

export function specifierResolvesToAllowedDesignLabOutbound(fromFile, specifier) {
  if (isExternalPackageSpecifier(specifier)) {
    return true;
  }

  const resolved = normalizeResolvedPath(fromFile, specifier);
  if (!WEB_SRC_SEGMENT.test(resolved)) {
    return true;
  }

  return DESIGN_LAB_OUTBOUND_ALLOW.some((pattern) => pattern.test(resolved));
}

export function designLabImportViolations(fromFile, content) {
  return extractImportSpecifiers(content)
    .filter((specifier) => specifierResolvesToDesignLab(fromFile, specifier))
    .map((specifier) => `${fromFile} imports '${specifier}'`);
}

export function labOwnedStylesheetImportViolations(fromFile, content) {
  return extractImportSpecifiers(content)
    .filter((specifier) => specifierResolvesToLabOwnedStylesheet(fromFile, specifier))
    .map((specifier) => `${fromFile} imports lab-owned stylesheet '${specifier}'`);
}

export function designLabOutboundImportViolations(fromFile, content) {
  return extractImportSpecifiers(content)
    .filter((specifier) => !specifierResolvesToAllowedDesignLabOutbound(fromFile, specifier))
    .map((specifier) => `${fromFile} imports forbidden production module '${specifier}'`);
}

export function isLabOwnedStylesheetFile(relativePath) {
  const normalized = relativePath.replaceAll("\\", "/");
  return (
    normalized === "web/src/styles/design-lab.css"
    || normalized.endsWith("/styles/design-lab.css")
    || normalized === "web/src/styles/components/demo.css"
    || normalized.endsWith("/styles/components/demo.css")
    || normalized.includes("/styles/surfaces/")
  );
}
