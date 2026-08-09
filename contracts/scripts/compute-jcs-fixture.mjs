#!/usr/bin/env node
/**
 * Computes canonical UTF-8 hex and SHA-256 for a JSON object file.
 * Used to author language-neutral JCS fixtures; not part of runtime verification.
 */
import { createHash } from "node:crypto";
import { readFile } from "node:fs/promises";
import canonicalize from "canonicalize";

const inputPath = process.argv[2];
if (!inputPath) {
  console.error("Usage: node compute-jcs-fixture.mjs <json-file>");
  process.exit(1);
}

const raw = await readFile(inputPath, "utf8");
const value = JSON.parse(raw);
const document = value.digest_document ?? value;
const canonical = canonicalize(document);
if (canonical === undefined) {
  throw new Error("Canonicalization returned undefined");
}
const bytes = Buffer.from(canonical, "utf8");
const hex = bytes.toString("hex");
const sha256 = createHash("sha256").update(bytes).digest("hex");
console.log(JSON.stringify({ canonical_utf8_hex: hex, sha256_hex: sha256 }, null, 2));
