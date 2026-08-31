import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

function walkTsx(dir: string, acc: string[] = []): string[] {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      walkTsx(path, acc);
      continue;
    }
    if (entry.name.endsWith(".tsx") && !entry.name.endsWith(".test.tsx")) acc.push(path);
  }
  return acc;
}

describe("OperateArea production call sites", () => {
  it("does not pass host, frame, or head class escapes from production composition", () => {
    const root = join(import.meta.dirname, "../../..");
    const leaks: string[] = [];
    const escape = /\b(hostClassName|frameClassName|headClassName|OperateAreaHost|bay="setup")/;
    for (const dir of ["pages", "router", "components", "features"] as const) {
      for (const file of walkTsx(join(root, dir))) {
        const source = readFileSync(file, "utf8");
        if (escape.test(source)) {
          leaks.push(file.replace(`${root}/`, ""));
        }
      }
    }
    expect(leaks).toEqual([]);
  });
});
