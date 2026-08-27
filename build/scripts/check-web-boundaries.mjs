#!/usr/bin/env node

import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const webRoots = [path.join(root, "web", "src"), path.join(root, "web-legacy", "src")];
const forbiddenPatterns = [
  /\.\.\/\.\.\/src\//,
  /FlexAgent\./,
  /from\s+["'][^"']*\/src\/Hosts\//,
  /from\s+["'][^"']*\/Modules\//,
];

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await walk(fullPath)));
      continue;
    }

    if (/\.(ts|tsx|js|jsx)$/.test(entry.name)) {
      files.push(fullPath);
    }
  }

  return files;
}

const violations = [];

for (const webRoot of webRoots) {
  const files = await walk(webRoot);
  for (const file of files) {
    const content = await readFile(file, "utf8");
    for (const pattern of forbiddenPatterns) {
      if (pattern.test(content)) {
        violations.push(`${path.relative(root, file)} matched ${pattern}`);
      }
    }
  }
}

if (violations.length > 0) {
  console.error("Browser/backend boundary violations:");
  for (const violation of violations) {
    console.error(`- ${violation}`);
  }
  process.exit(1);
}

console.log("Browser/backend boundary check passed.");
