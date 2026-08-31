import path from "node:path";

const DESIGN_LAB_SEGMENT = /(?:^|\/)design-lab(?:\/|$)/;

const LAB_OWNED_STYLESHEET = /(?:^|\/)styles\/(?:design-lab\.css|components\/demo\.css|surfaces\/)/;

const DESIGN_LAB_OUTBOUND_REPO_PREFIXES = [
  "web/src/design-lab/",
  "web/src/design-system/",
  "web/src/lib/",
  "web/src/styles/",
  "web/src/components/work/",
  "web/src/content/",
  "web/src/features/assessment/SetupTrackReadout",
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

const OUTER_CHROME_NAMES = [
  "CommandStrip",
  "ConsoleFoot",
  "Gangway",
  "Bulkhead",
  "AreaGroupList",
  "RailBrand",
  "IndexRail",
];

const CHROME_ALLOW_PATH = /(?:^|\/)(?:design-system\/(?:patterns\/layouts|components)|design-lab\/components|design-lab\/features\/gallery\/sections)(?:\/|$)/;
const ROUTE_OR_PAGE_PATH = /(?:^|\/)(?:pages|design-lab\/routes)\//;
const LAYOUT_CSS_OWNER = /(?:^|\/)styles\/components\/layouts\.css$/;
const RESERVED_LAYOUT_SELECTOR = /(?:^|})\s*\.layout-(?:management|guided|session|reference)(?:__[a-z0-9-]+)?(?=\s*[,{:])/gi;

function extractNamedImports(content) {
  const names = [];
  const pattern = /import\s+(?:type\s+)?\{([^}]+)\}/g;
  let match;
  while ((match = pattern.exec(content)) !== null) {
    for (const part of match[1].split(",")) {
      const ident = part.replace(/\s+as\s+\w+/g, "").replace(/type\s+/g, "").trim();
      if (ident) names.push(ident);
    }
  }
  return names;
}

const LAYOUT_COMPONENT_NAMES = [
  "ManagementLayout",
  "GuidedTaskLayout",
  "LiveSessionLayout",
  "ReferenceLayout",
  "LayoutAssignment",
];

export function productionPageLayoutImportViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (!/(?:^|\/)pages\//.test(normalized) || /\.(test|spec)\.(ts|tsx)$/.test(normalized)) {
    return [];
  }
  const imported = new Set(extractNamedImports(content));
  return LAYOUT_COMPONENT_NAMES
    .filter((name) => imported.has(name) || content.includes(`<${name}`))
    .map((name) => `${relativePath} imports layout '${name}'`);
}

const OPERATE_HEAD_ROUTE_ALLOW = /(?:^|\/)design-lab\/features\/gallery\/sections\//;

const LAB_ROUTE_LAYOUT_COMPONENT = {
  "AdminPage.tsx": "ManagementLayout",
  "HomePage.tsx": "ManagementLayout",
  "JourneyPage.tsx": "GuidedTaskLayout",
  "SessionPage.tsx": "LiveSessionLayout",
  "ReviewerPage.tsx": "ManagementLayout",
  "SurfacesPage.tsx": "ReferenceLayout",
  "NotFoundPage.tsx": "ReferenceLayout",
};

export function operateHeadRouteViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (!ROUTE_OR_PAGE_PATH.test(normalized)) {
    return [];
  }
  if (/\.(test|spec)\.(ts|tsx)$/.test(normalized)) {
    return [];
  }
  if (OPERATE_HEAD_ROUTE_ALLOW.test(normalized)) {
    return [];
  }
  const imported = new Set(extractNamedImports(content));
  if (imported.has("OperateHead") || content.includes("<OperateHead")) {
    return [`${relativePath} assembles OperateHead; use OperateArea`];
  }
  return [];
}

export function designLabRouteLayoutComponentViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (!normalized.includes("/design-lab/routes/")) {
    return [];
  }
  if (/\.(test|spec)\.(ts|tsx)$/.test(normalized)) {
    return [];
  }
  const file = normalized.split("/").pop();
  const expected = LAB_ROUTE_LAYOUT_COMPONENT[file];
  if (!expected) {
    return [];
  }
  if (!content.includes(`<${expected}`)) {
    return [`${relativePath} must render ${expected}`];
  }
  return [];
}

export function outerChromeImportViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (!ROUTE_OR_PAGE_PATH.test(normalized)) {
    return [];
  }
  if (/\.(test|spec)\.(ts|tsx)$/.test(normalized)) {
    return [];
  }
  const imported = new Set(extractNamedImports(content));
  return OUTER_CHROME_NAMES
    .filter((name) => imported.has(name) || content.includes(`<${name}`))
    .map((name) => `${relativePath} composes outer chrome '${name}'`);
}

export function reservedLayoutCssViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (!normalized.endsWith(".css") || LAYOUT_CSS_OWNER.test(normalized)) {
    return [];
  }
  RESERVED_LAYOUT_SELECTOR.lastIndex = 0;
  if (!RESERVED_LAYOUT_SELECTOR.test(content)) {
    return [];
  }
  return [`${relativePath} declares reserved layout selectors outside styles/components/layouts.css`];
}

export function productionReferenceLayoutViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (normalized.includes("/design-lab/")) {
    return [];
  }
  if (normalized.includes("/design-system/patterns/layouts/")) {
    return [];
  }
  if (normalized.endsWith("/design-system/lab.ts")) {
    return [];
  }
  if (!/\.(ts|tsx|js|jsx)$/.test(normalized)) {
    return [];
  }
  const violations = [];
  if (content.includes("ReferenceLayout")) {
    violations.push(`${relativePath} references ReferenceLayout`);
  }
  if (content.includes('data-layout="reference"')) {
    violations.push(`${relativePath} selects data-layout="reference"`);
  }
  return violations;
}

const LAYOUT_ROOT_ALLOW = /(?:^|\/)(?:design-system\/patterns\/layouts\/|design-lab\/components\/layouts\/|design-lab\/features\/gallery\/sections\/)/;
const LAYOUT_ROOT_ATTR = /data-layout=["'](management|guided-task|live-session|reference)["']/;

export function layoutRootAttributeViolations(relativePath, content) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (!/\.(ts|tsx|js|jsx)$/.test(normalized)) {
    return [];
  }
  if (/\.(test|spec)\.(ts|tsx)$/.test(normalized)) {
    return [];
  }
  if (LAYOUT_ROOT_ALLOW.test(normalized)) {
    return [];
  }
  if (!LAYOUT_ROOT_ATTR.test(content)) {
    return [];
  }
  return [`${relativePath} uses a layout root attribute outside the layout library`];
}

export function routeLayoutMappingViolations(mappedPaths, leaves) {
  const violations = [];
  const counts = new Map();
  for (const path of mappedPaths) {
    counts.set(path, (counts.get(path) ?? 0) + 1);
  }
  for (const [path, count] of counts) {
    if (count > 1) {
      violations.push(`multiply mapped '${path}'`);
    }
  }
  const mapped = new Set(mappedPaths);
  for (const leaf of leaves) {
    if (leaf.redirect) {
      if (mapped.has(leaf.path)) {
        violations.push(`redirect '${leaf.path}' has an independent layout`);
      }
      continue;
    }
    if (!mapped.has(leaf.path)) {
      violations.push(`unmapped route '${leaf.path}'`);
    }
  }
  return violations;
}

export function chromeAllowPath(relativePath) {
  return CHROME_ALLOW_PATH.test(relativePath.replaceAll("\\", "/"));
}

