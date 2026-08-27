import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const webRoot = join(dirname(fileURLToPath(import.meta.url)), "../..");
const faviconPath = join(webRoot, "public/favicon.svg");
const prototypeManifestHash = "b25165c057bd209cee246ce21de679a6fc227a9a6544a46e133fb26e5a5f181b";

describe("document favicon", () => {
  it("matches the pinned Shipboard Terminal mark hash", () => {
    const served = readFileSync(faviconPath);
    expect(createHash("sha256").update(served).digest("hex")).toBe(prototypeManifestHash);
  });

  it("is referenced from both HTML shells with the Shipboard icon link", () => {
    for (const relative of ["index.html", "design-lab.html"]) {
      const html = readFileSync(join(webRoot, relative), "utf8");
      expect(html, relative).toContain(
        '<link rel="icon" type="image/svg+xml" href="/favicon.svg" />',
      );
    }
  });
});
