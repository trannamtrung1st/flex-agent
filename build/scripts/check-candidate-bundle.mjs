#!/usr/bin/env node

import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const distAssets = path.join(root, "web", "dist", "assets");
const forbidden = [
  "HOME_ENROLLMENTS",
  "Approve & Release",
  "Mark Submission Complete",
  "design-lab",
  "prototype-only fixture",
  ".demo-plate",
  "html[data-surface=\"gallery\"]",
  "html[data-surface=\"surfaces-index\"]",
  "html[data-surface=\"participant-home\"]",
  "html[data-surface=\"participant-journey\"]",
  "html[data-surface=\"participant-session\"]",
  "html[data-surface=\"admin-console\"]",
  "html[data-surface=\"reviewer-console\"]",
  "html[data-surface=\"not-found\"]",
  "data-layout=\"reference\"",
  "ReferenceLayout",
];

let files = [];
try {
  files = (await readdir(distAssets)).filter((name) => name.endsWith(".js") || name.endsWith(".css"));
} catch (error) {
  if (error && typeof error === "object" && "code" in error && error.code === "ENOENT") {
    console.error("Candidate production dist is missing. Run the candidate web build first.");
    process.exit(1);
  }
  throw error;
}

const violations = [];
for (const name of files) {
  const content = await readFile(path.join(distAssets, name), "utf8");
  for (const needle of forbidden) {
    if (content.includes(needle)) {
      violations.push(`${path.relative(root, path.join(distAssets, name))} contains forbidden '${needle}'`);
    }
  }
}

if (violations.length > 0) {
  console.error("Candidate production bundle isolation violations:");
  for (const violation of violations) {
    console.error(`- ${violation}`);
  }
  process.exit(1);
}

console.log("Candidate production bundle isolation check passed.");
