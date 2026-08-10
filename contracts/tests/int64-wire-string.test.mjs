import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import assert from "node:assert/strict";

const contractsRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const fixturePath = path.join(
  contractsRoot,
  "fixtures/schema/v1/session/state-event-envelope/valid-unsafe-int-sequence.json",
);

test("int64 wire strings preserve values above Number.MAX_SAFE_INTEGER", async () => {
  const raw = await readFile(fixturePath, "utf8");
  const fixture = JSON.parse(raw);
  const unsafe = "9007199254740993";

  assert.equal(fixture.session_sequence, unsafe);
  assert.equal(Number(unsafe), 9007199254740992);
  assert.notEqual(String(Number(unsafe)), unsafe);

  const roundTrip = JSON.parse(JSON.stringify(fixture));
  assert.equal(roundTrip.session_sequence, unsafe);
});
