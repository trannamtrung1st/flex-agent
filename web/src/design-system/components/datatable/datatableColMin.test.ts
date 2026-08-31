import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { DATATABLE_COL_MIN, type DatatableColMin } from "./datatableColMin";

const cssPath = resolve(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/datatable.css");

const CSS_VAR: Record<DatatableColMin, string> = {
  id: "--datatable-col-min-id",
  label: "--datatable-col-min-label",
  state: "--datatable-col-min-state",
  instant: "--datatable-col-min-instant",
  compactId: "--datatable-col-min-compact-id",
  stage: "--datatable-col-min-stage",
  result: "--datatable-col-min-result",
  count: "--datatable-col-min-count",
  rev: "--datatable-col-min-rev",
  confidence: "--datatable-col-min-confidence",
  title: "--datatable-col-min-title",
  action: "--datatable-col-min-action",
};

describe("DATATABLE_COL_MIN", () => {
  it("matches the shared datatable CSS floors", () => {
    const css = readFileSync(cssPath, "utf8");
    for (const [kind, value] of Object.entries(DATATABLE_COL_MIN)) {
      const variable = CSS_VAR[kind as DatatableColMin];
      expect(css, kind).toMatch(new RegExp(`${variable}:\\s*${value}`));
      expect(css, kind).toMatch(new RegExp(`\\[data-col-min="${kind}"\\]`));
    }
  });
});
