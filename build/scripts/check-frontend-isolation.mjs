#!/usr/bin/env node

import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

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

const importNeedles = ["web-legacy", "design-lab", ".work/resources", "impeccable-prototype"];
const fixtureNeedles = ["HOME_ENROLLMENTS", "Approve & Release", "Mark Submission Complete"];

function isCodeSource(file) {
  return /\.(ts|tsx|js|jsx|html)$/.test(file);
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
  const content = await readFile(file, "utf8");
  if (file.startsWith(designSystemRoot + path.sep)) {
    addViolations(violations, relative, content, [
      "web-legacy",
      ".work/resources",
      "impeccable-prototype",
      "src/design-lab",
      "web/src/design-lab",
    ]);
    continue;
  }
  addViolations(violations, relative, content, importNeedles);
  if (isCodeSource(file)) {
    addViolations(violations, relative, content, fixtureNeedles);
  }
}

try {
  for (const file of await walk(designLabRoot)) {
    const relative = path.relative(root, file);
    const content = await readFile(file, "utf8");
    addViolations(violations, relative, content, [
      "web-legacy",
      ".work/resources",
      "impeccable-prototype",
    ]);
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
