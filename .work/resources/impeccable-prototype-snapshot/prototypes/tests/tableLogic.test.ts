import { describe, expect, it } from "vitest";
import { enrollmentQueryKey, matchingEnrollmentIds, pageSlice, sortAndFilter } from "../src/components/tableLogic";
import { EMPTY_SELECTION } from "../src/components/tableSelection";
import type { DataTableState, EnrollmentRow } from "../src/data/types";

const rows: EnrollmentRow[] = [
  { id: "P-2", campaign: "B", stage: "REVIEW", result: "READY", deadline: new Date("2026-08-29T00:00:00"), attempt: "1 OF 2", duration: "—", submission: "V1", evidence: "—" },
  { id: "P-1", campaign: "A", stage: "BRIEFING", result: "PENDING", deadline: new Date("2026-08-28T00:00:00"), attempt: "1 OF 2", duration: "—", submission: "NONE", evidence: "—" },
  { id: "P-3", campaign: "A", stage: "EXAMINATION", result: "LIVE", deadline: new Date("2026-08-30T00:00:00"), attempt: "1 OF 2", duration: "42:11", submission: "V1", evidence: "4 ITEMS" },
];

const base: DataTableState = {
  stageFilter: null,
  search: "",
  sorts: [{ key: "deadline", dir: "asc" }],
  page: 0,
  pageSize: 2,
  selection: EMPTY_SELECTION,
  expandedId: null,
};

describe("sortAndFilter", () => {
  it("sorts by deadline ascending", () => {
    const list = sortAndFilter(rows, base);
    expect(list.map((r) => r.id)).toEqual(["P-1", "P-2", "P-3"]);
  });

  it("sorts by multiple keys in order", () => {
    const list = sortAndFilter(rows, {
      ...base,
      sorts: [
        { key: "campaign", dir: "asc" },
        { key: "deadline", dir: "desc" },
      ],
    });
    expect(list.map((r) => r.id)).toEqual(["P-3", "P-1", "P-2"]);
  });

  it("filters by stage and search", () => {
    const list = sortAndFilter(rows, { ...base, stageFilter: "BRIEFING", search: "P-1" });
    expect(list).toHaveLength(1);
    expect(list[0].id).toBe("P-1");
  });
});

describe("pageSlice", () => {
  it("pages filtered rows", () => {
    const list = sortAndFilter(rows, base);
    const slice = pageSlice(list, base);
    expect(slice.pageRows).toHaveLength(2);
    expect(slice.total).toBe(3);
    expect(slice.pageCount).toBe(2);
  });
});

describe("enrollmentQueryKey", () => {
  it("tracks stage and search only", () => {
    expect(enrollmentQueryKey({ stageFilter: null, search: "" })).toBe("search:|stage:");
    expect(enrollmentQueryKey({ stageFilter: "BRIEFING", search: "p-1" })).toBe("search:P-1|stage:BRIEFING");
  });

  it("derives matching ids from the filtered manifest", () => {
    const state = { ...base, stageFilter: "BRIEFING" as const, search: "P-1" };
    expect(matchingEnrollmentIds(rows, state)).toEqual(["P-1"]);
  });
});
