import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import assert from "node:assert/strict";

const contractsRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const INT64_MAX = 9223372036854775807n;

const fixturePath = path.join(
  contractsRoot,
  "fixtures/schema/v1/session/state-event-envelope/valid-unsafe-int-sequence.json",
);

function isPositiveInt64WireString(value) {
  if (!/^[1-9][0-9]*$/.test(value)) return false;
  try {
    return BigInt(value) >= 1n && BigInt(value) <= INT64_MAX;
  } catch {
    return false;
  }
}

test("positive int64 wire strings preserve values above Number.MAX_SAFE_INTEGER", async () => {
  const raw = await readFile(fixturePath, "utf8");
  const fixture = JSON.parse(raw);
  const unsafe = "9007199254740993";

  assert.equal(fixture.session_sequence, unsafe);
  assert.equal(Number(unsafe), 9007199254740992);
  assert.notEqual(String(Number(unsafe)), unsafe);
  assert.ok(isPositiveInt64WireString(unsafe));

  const roundTrip = JSON.parse(JSON.stringify(fixture));
  assert.equal(roundTrip.session_sequence, unsafe);
});

test("positive int64 wire strings reject zero and overflow", () => {
  assert.equal(isPositiveInt64WireString("0"), false);
  assert.equal(isPositiveInt64WireString("9999999999999999999"), false);
  assert.equal(isPositiveInt64WireString("9223372036854775807"), true);
  assert.equal(isPositiveInt64WireString("9223372036854775808"), false);
});
