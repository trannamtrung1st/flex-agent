import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { pageRows, sortAndFilterRows, useTableController } from "./tableController";

type Row = { id: string; n: number; tag: string };

const rows: Row[] = [
  { id: "a", n: 2, tag: "live" },
  { id: "b", n: 1, tag: "done" },
  { id: "c", n: 3, tag: "live" },
];

describe("tableController", () => {
  it("filters then sorts then pages", () => {
    const visible = sortAndFilterRows(rows, {
      match: (row) => row.tag === "live",
      sorts: [{ key: "n", dir: "asc" }],
      getSortValue: (row, key) => row[key],
    });
    expect(visible.map((row) => row.id)).toEqual(["a", "c"]);
    expect(pageRows(visible, 0, 1).pageRows.map((row) => row.id)).toEqual(["a"]);
    expect(pageRows(visible, 1, 1).page).toBe(1);
  });

  it("useTableController returns visible and paged rows", () => {
    const { result } = renderHook(() =>
      useTableController({
        rows,
        match: (row) => row.tag === "live",
        sorts: [{ key: "n", dir: "asc" }],
        page: 0,
        pageSize: 1,
        getSortValue: (row, key) => row[key],
      }),
    );
    expect(result.current.visibleRows.map((row) => row.id)).toEqual(["a", "c"]);
    expect(result.current.pageRows.map((row) => row.id)).toEqual(["a"]);
    expect(result.current.pageCount).toBe(2);
  });
});
