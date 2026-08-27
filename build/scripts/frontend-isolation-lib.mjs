import path from "node:path";

const DESIGN_LAB_SEGMENT = /(?:^|\/)design-lab(?:\/|$)/;

const LAB_OWNED_STYLESHEET = /(?:^|\/)styles\/(?:design-lab\.css|components\/demo\.css|surfaces\/)/;

const DESIGN_LAB_OUTBOUND_REPO_PREFIXES = [
  "web/src/design-lab/",
  "web/src/design-system/",
  "web/src/lib/",
  "web/src/styles/",
];

const CANDIDATE_HTML_MODULE_ENTRY = "/src/main.tsx";
const DESIGN_LAB_HTML_MODULE_ENTRY = "/src/design-lab/main.tsx";

const SPECIFIER_PATTERNS = [
  /(?:import|export)\s+(?:type\s+)?(?:[\s\S]*?\sfrom\s+)?["']([^"']+)["']/g,
  /import\s*\(\s*["']([^"']+)["']\s*\)/g,
  /require\s*\(\s*["']([^"']+)["']\s*\)/g,
  /@import\s+(?:url\(\s*)?["']([^"']+)["']/g,
];

const HTML_MODULE_SRC_PATTERNS = [
  /<script\b[^>]*\btype\s*=\s*["']module["'][^>]*\bsrc\s*=\s*["']([^"']+)["']/gi,
  /<script\b[^>]*\bsrc\s*=\s*["']([^"']+)["'][^>]*\btype\s*=\s*["']module["']/gi,
];

const HTML_STYLESHEET_HREF_PATTERNS = [
  /<link\b[^>]*\brel\s*=\s*["']stylesheet["'][^>]*\bhref\s*=\s*["']([^"']+)["']/gi,
  /<link\b[^>]*\bhref\s*=\s*["']([^"']+)["'][^>]*\brel\s*=\s*["']stylesheet["']/gi,
];

function normalizeRepoRoot(repoRoot) {
  return path.normalize(repoRoot).replaceAll("\\", "/").replace(/\/$/, "");
}

export function inferRepoRootFromWebSrcFile(fromFile) {
  const normalized = fromFile.replaceAll("\\", "/");
  const marker = "/web/src/";
  const index = normalized.indexOf(marker);
  if (index === -1) {
    throw new Error(`Cannot infer repository root from file path: ${fromFile}`);
  }
  return normalized.slice(0, index);
}

function isExternalPackageSpecifier(specifier) {
  const normalized = specifier.replaceAll("\\", "/");
  return !normalized.startsWith(".") && !normalized.startsWith("/") && !path.isAbsolute(specifier);
}

function isRemoteReference(specifier) {
  return /^[a-z][a-z0-9+.-]*:/i.test(specifier);
}

export function resolveSpecifierToAbsolute(fromFile, specifier, repoRoot) {
  const normalized = specifier.replaceAll("\\", "/");
  const normalizedRepoRoot = normalizeRepoRoot(repoRoot);

  if (isRemoteReference(normalized)) {
    return null;
  }

  if (normalized.startsWith("/") && !normalized.startsWith("//")) {
    return path.normalize(path.join(normalizedRepoRoot, "web", normalized.slice(1))).replaceAll("\\", "/");
  }

  if (normalized.startsWith(".") || path.isAbsolute(specifier)) {
    return path.normalize(path.join(path.dirname(fromFile), specifier)).replaceAll("\\", "/");
  }

  return null;
}

function repoRelativePath(absolutePath, repoRoot) {
  const normalizedRepoRoot = normalizeRepoRoot(repoRoot);
  const normalizedAbsolute = absolutePath.replaceAll("\\", "/");
  if (!normalizedAbsolute.startsWith(`${normalizedRepoRoot}/`)) {
    return null;
  }
  return normalizedAbsolute.slice(normalizedRepoRoot.length + 1);
}

function isAllowedDesignLabOutboundRepoRelative(relativeToRepo) {
  return DESIGN_LAB_OUTBOUND_REPO_PREFIXES.some(
    (prefix) => relativeToRepo === prefix.slice(0, -1) || relativeToRepo.startsWith(prefix),
  );
}

export function extractHtmlModuleScriptSources(content) {
  const references = [];
  for (const pattern of HTML_MODULE_SRC_PATTERNS) {
    pattern.lastIndex = 0;
    let match;
    while ((match = pattern.exec(content)) !== null) {
      references.push(match[1]);
    }
  }
  return references;
}

export function extractHtmlStylesheetHrefs(content) {
  const references = [];
  for (const pattern of HTML_STYLESHEET_HREF_PATTERNS) {
    pattern.lastIndex = 0;
    let match;
    while ((match = pattern.exec(content)) !== null) {
      references.push(match[1]);
    }
  }
  return references;
}

export function extractHtmlEntryReferences(content) {
  return [...extractHtmlModuleScriptSources(content), ...extractHtmlStylesheetHrefs(content)];
}

export function extractImportSpecifiers(content) {
  const specifiers = [];
  for (const pattern of SPECIFIER_PATTERNS) {
    pattern.lastIndex = 0;
    let match;
    while ((match = pattern.exec(content)) !== null) {
      specifiers.push(match[1]);
    }
  }
  if (content.includes("<")) {
    specifiers.push(...extractHtmlEntryReferences(content));
  }
  return specifiers;
}

export function specifierResolvesToDesignLab(fromFile, specifier, repoRoot = inferRepoRootFromWebSrcFile(fromFile)) {
  if (isExternalPackageSpecifier(specifier)) {
    return DESIGN_LAB_SEGMENT.test(specifier.replaceAll("\\", "/"));
  }
  const resolved = resolveSpecifierToAbsolute(fromFile, specifier, repoRoot);
  return resolved !== null && DESIGN_LAB_SEGMENT.test(resolved);
}

export function specifierResolvesToLabOwnedStylesheet(fromFile, specifier, repoRoot = inferRepoRootFromWebSrcFile(fromFile)) {
  if (isExternalPackageSpecifier(specifier)) {
    return false;
  }
  const resolved = resolveSpecifierToAbsolute(fromFile, specifier, repoRoot);
  return resolved !== null && LAB_OWNED_STYLESHEET.test(resolved);
}

export function specifierResolvesToAllowedDesignLabOutbound(fromFile, specifier, repoRoot = inferRepoRootFromWebSrcFile(fromFile)) {
  if (isExternalPackageSpecifier(specifier)) {
    return true;
  }

  const resolved = resolveSpecifierToAbsolute(fromFile, specifier, repoRoot);
  if (!resolved) {
    return false;
  }

  const relativeToRepo = repoRelativePath(resolved, repoRoot);
  if (!relativeToRepo) {
    return false;
  }

  return isAllowedDesignLabOutboundRepoRelative(relativeToRepo);
}

export function designLabImportViolations(fromFile, content, repoRoot = inferRepoRootFromWebSrcFile(fromFile)) {
  return extractImportSpecifiers(content)
    .filter((specifier) => specifierResolvesToDesignLab(fromFile, specifier, repoRoot))
    .map((specifier) => `${fromFile} imports '${specifier}'`);
}

export function labOwnedStylesheetImportViolations(fromFile, content, repoRoot) {
  const resolvedRepoRoot = repoRoot ?? inferRepoRootFromWebSrcFile(fromFile);
  return extractImportSpecifiers(content)
    .filter((specifier) => specifierResolvesToLabOwnedStylesheet(fromFile, specifier, resolvedRepoRoot))
    .map((specifier) => `${fromFile} imports lab-owned stylesheet '${specifier}'`);
}

export function designLabOutboundImportViolations(fromFile, content, repoRoot = inferRepoRootFromWebSrcFile(fromFile)) {
  return extractImportSpecifiers(content)
    .filter((specifier) => !specifierResolvesToAllowedDesignLabOutbound(fromFile, specifier, repoRoot))
    .map((specifier) => `${fromFile} imports forbidden production module '${specifier}'`);
}

export function candidateHtmlEntryViolations(htmlFile, content, repoRoot) {
  const violations = [];

  for (const ref of extractHtmlModuleScriptSources(content)) {
    if (ref !== CANDIDATE_HTML_MODULE_ENTRY) {
      violations.push(`${htmlFile} module entry must be '${CANDIDATE_HTML_MODULE_ENTRY}', found '${ref}'`);
    }
    if (specifierResolvesToDesignLab(htmlFile, ref, repoRoot)) {
      violations.push(`${htmlFile} references design-lab module '${ref}'`);
    }
  }

  for (const ref of extractHtmlStylesheetHrefs(content)) {
    if (specifierResolvesToLabOwnedStylesheet(htmlFile, ref, repoRoot)) {
      violations.push(`${htmlFile} references lab-owned stylesheet '${ref}'`);
    }
    if (specifierResolvesToDesignLab(htmlFile, ref, repoRoot)) {
      violations.push(`${htmlFile} references design-lab asset '${ref}'`);
    }
  }

  return violations;
}

export function designLabHtmlEntryViolations(htmlFile, content) {
  const violations = [];
  for (const ref of extractHtmlModuleScriptSources(content)) {
    if (ref !== DESIGN_LAB_HTML_MODULE_ENTRY) {
      violations.push(`${htmlFile} module entry must be '${DESIGN_LAB_HTML_MODULE_ENTRY}', found '${ref}'`);
    }
  }
  return violations;
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
