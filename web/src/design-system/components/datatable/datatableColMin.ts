export const DATATABLE_COL_MIN = {
  id: "12rem",
  label: "10rem",
  state: "9rem",
  instant: "13rem",
  compactId: "9rem",
  stage: "8.5rem",
  result: "7.5rem",
  count: "5.5rem",
  rev: "4rem",
  confidence: "7.5rem",
  action: "3rem",
} as const;

export type DatatableColMin = keyof typeof DATATABLE_COL_MIN;

export function datatableColMin(kind: DatatableColMin): { "data-col-min": DatatableColMin } {
  return { "data-col-min": kind };
}
