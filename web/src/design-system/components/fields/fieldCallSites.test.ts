import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

function walkTsx(dir: string, acc: string[] = []): string[] {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === "node_modules") continue;
      walkTsx(path, acc);
      continue;
    }
    if (entry.name.endsWith(".tsx") && !entry.name.endsWith(".test.tsx")) acc.push(path);
  }
  return acc;
}

function fieldTags(source: string, tag: "FieldInput" | "FieldTextarea"): string[] {
  const chunks: string[] = [];
  const open = new RegExp(`<${tag}\\b`, "g");
  let match: RegExpExecArray | null;
  while ((match = open.exec(source))) {
    const start = match.index;
    const selfClose = source.indexOf("/>", start);
    if (selfClose === -1) {
      chunks.push(source.slice(start, start + 80));
      continue;
    }
    chunks.push(source.slice(start, selfClose + 2));
  }
  return chunks;
}

describe("field control call sites", () => {
  it("passes placeholder on every FieldInput and FieldTextarea", () => {
    const root = join(import.meta.dirname, "../../..");
    const missing: string[] = [];
    for (const file of walkTsx(root)) {
      const source = readFileSync(file, "utf8");
      for (const tag of ["FieldInput", "FieldTextarea"] as const) {
        for (const chunk of fieldTags(source, tag)) {
          if (!/\bplaceholder=/.test(chunk)) {
            missing.push(`${file.replace(`${root}/`, "")}: ${chunk.split("\n")[0]}`);
          }
        }
      }
    }
    expect(missing).toEqual([]);
  });
});
