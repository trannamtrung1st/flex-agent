#!/usr/bin/env node

import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  candidateHtmlEntryViolations,
  designLabHtmlEntryViolations,
  designLabImportViolations,
  designLabOutboundImportViolations,
  isLabOwnedStylesheetFile,
  labOwnedStylesheetImportViolations,
  outerChromeImportViolations,
  operateHeadRouteViolations,
  designLabRouteLayoutComponentViolations,
  productionPageLayoutImportViolations,
  productionReferenceLayoutViolations,
  reservedLayoutCssViolations,
  layoutRootAttributeViolations,
} from "./frontend-isolation-lib.mjs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await walk(fullPath)));
      continue;
    }

    if (/\.(ts|tsx|js|jsx|css|html)$/.test(entry.name)) {
      files.push(fullPath);
    }
  }

  return files;
}

function addViolations(violations, relative, content, needles) {
  for (const needle of needles) {
    if (content.includes(needle)) {
      violations.push(`${relative} contains forbidden '${needle}'`);
    }
  }
}

function addDesignLabImportViolations(violations, file, relative, content) {
  for (const violation of designLabImportViolations(file, content)) {
    violations.push(`${relative} imports a design-lab module (${violation.slice(file.length + " imports ".length)})`);
  }
}

function addLabOwnedStylesheetImportViolations(violations, file, relative, content) {
  for (const violation of labOwnedStylesheetImportViolations(file, content, root)) {
    violations.push(`${relative} ${violation.slice(file.length + 1)}`);
  }
}

function addDesignLabOutboundImportViolations(violations, file, relative, content) {
  for (const violation of designLabOutboundImportViolations(file, content, root)) {
    violations.push(`${relative} ${violation.slice(file.length + 1)}`);
  }
}

const snapshotNeedles = ["web-legacy", ".work/resources", "impeccable-prototype"];
const fixtureNeedles = ["HOME_ENROLLMENTS", "Approve & Release", "Mark Submission Complete"];

function isCodeSource(file) {
  return /\.(ts|tsx|js|jsx|html)$/.test(file);
}

function isLabOwnedStylesheet(relative) {
  return isLabOwnedStylesheetFile(relative.replaceAll("\\", "/"));
}

const violations = [];
const productionRoot = path.join(root, "web", "src");
const legacyRoot = path.join(root, "web-legacy", "src");
const designLabRoot = path.join(productionRoot, "design-lab");
const designSystemRoot = path.join(productionRoot, "design-system");

for (const file of await walk(productionRoot)) {
  if (file.startsWith(designLabRoot + path.sep)) {
    continue;
  }

  const relative = path.relative(root, file);
  if (isLabOwnedStylesheet(relative)) {
    continue;
  }

  const content = await readFile(file, "utf8");
  addViolations(violations, relative, content, snapshotNeedles);
  addDesignLabImportViolations(violations, file, relative, content);
  addLabOwnedStylesheetImportViolations(violations, file, relative, content);
  violations.push(...productionReferenceLayoutViolations(relative, content));
  violations.push(...reservedLayoutCssViolations(relative, content));
  violations.push(...outerChromeImportViolations(relative, content));
  violations.push(...operateHeadRouteViolations(relative, content));
  violations.push(...productionPageLayoutImportViolations(relative, content));
  violations.push(...layoutRootAttributeViolations(relative, content));
  if (isCodeSource(file)) {
    addViolations(violations, relative, content, fixtureNeedles);
  }
}

const candidateIndex = path.join(root, "web", "index.html");
const designLabIndex = path.join(root, "web", "design-lab.html");
for (const violation of candidateHtmlEntryViolations(
  candidateIndex,
  await readFile(candidateIndex, "utf8"),
  root,
)) {
  violations.push(violation);
}
for (const violation of designLabHtmlEntryViolations(
  designLabIndex,
  await readFile(designLabIndex, "utf8"),
)) {
  violations.push(violation);
}

try {
  for (const file of await walk(designLabRoot)) {
    const relative = path.relative(root, file);
    const content = await readFile(file, "utf8");
    addViolations(violations, relative, content, snapshotNeedles);
    violations.push(...reservedLayoutCssViolations(relative, content));
    violations.push(...outerChromeImportViolations(relative, content));
    violations.push(...operateHeadRouteViolations(relative, content));
    violations.push(...designLabRouteLayoutComponentViolations(relative, content));
    violations.push(...layoutRootAttributeViolations(relative, content));
    if (isCodeSource(file) && !/\.(test|spec)\.(ts|tsx)$/.test(file)) {
      addDesignLabOutboundImportViolations(violations, file, relative, content);
    }
  }
} catch (error) {
  if (error && typeof error === "object" && "code" in error && error.code !== "ENOENT") {
    throw error;
  }
}

for (const file of await walk(legacyRoot)) {
  const relative = path.relative(root, file);
  const content = await readFile(file, "utf8");
  addViolations(violations, relative, content, [
    "@flex-agent/web",
    "design-lab",
    ".work/resources",
    "impeccable-prototype",
  ]);
  if (content.includes("../web/") || content.includes("\"web/src") || content.includes("'web/src")) {
    violations.push(`${relative} imports the new web tree`);
  }
}

if (violations.length > 0) {
  console.error("Frontend isolation violations:");
  for (const violation of violations) {
    console.error(`- ${violation}`);
  }
  process.exit(1);
}

console.log("Frontend isolation check passed.");
