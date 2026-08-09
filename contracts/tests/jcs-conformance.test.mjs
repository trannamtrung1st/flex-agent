import { createHash } from "node:crypto";
import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import assert from "node:assert/strict";
import canonicalize from "canonicalize";

const contractsRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const jcsRoot = path.join(contractsRoot, "fixtures", "jcs");

async function collectFixtures(outcome) {
  const fixtures = [];
  const procedures = await import("node:fs/promises").then((fs) => fs.readdir(jcsRoot, { withFileTypes: true }));
  for (const procedure of procedures) {
    if (!procedure.isDirectory()) continue;
    const caseDirs = await import("node:fs/promises").then((fs) =>
      fs.readdir(path.join(jcsRoot, procedure.name), { withFileTypes: true }),
    );
    for (const caseDir of caseDirs) {
      if (!caseDir.isDirectory()) continue;
      const fixturePath = path.join(jcsRoot, procedure.name, caseDir.name, "fixture.json");
      const raw = await readFile(fixturePath, "utf8");
      const fixture = JSON.parse(raw);
      if (fixture.outcome === outcome) {
        fixtures.push({ fixturePath, fixture });
      }
    }
  }
  return fixtures;
}

test("success JCS fixtures match independent Node canonicalization", async () => {
  const fixtures = await collectFixtures("success");
  assert.ok(fixtures.length > 0);
  for (const { fixture } of fixtures) {
    const canonical = canonicalize(fixture.digest_document);
    assert.ok(canonical);
    const bytes = Buffer.from(canonical, "utf8");
    const hex = bytes.toString("hex");
    const sha256 = createHash("sha256").update(bytes).digest("hex");
    assert.equal(hex, fixture.expected_canonical_utf8_hex);
    assert.equal(sha256, fixture.expected_sha256_hex);
  }
});

test("failure JCS fixtures are rejected by independent Node canonicalization", async () => {
  const fixtures = await collectFixtures("failure");
  assert.ok(fixtures.length > 0);
  for (const { fixture } of fixtures) {
    if (fixture.raw_digest_document_utf8_hex) {
      const raw = Buffer.from(fixture.raw_digest_document_utf8_hex, "hex").toString("utf8");
      assert.throws(() => canonicalize(JSON.parse(raw)));
      continue;
    }

    assert.throws(() => canonicalize(fixture.digest_document));
  }
});
