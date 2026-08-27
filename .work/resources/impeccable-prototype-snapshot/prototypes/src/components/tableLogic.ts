import type { DataTableState, EnrollmentRow, SortKey } from "../data/types";
import { matchingQueryKey } from "./tableSelection";
import { pageRows, sortAndFilterRows } from "./datatable/tableController";

export function enrollmentQueryKey(state: Pick<DataTableState, "stageFilter" | "search">) {
  return matchingQueryKey({
    stage: state.stageFilter ?? "",
    search: state.search.trim().toUpperCase(),
  });
}

export function matchingEnrollmentIds(rows: EnrollmentRow[], state: DataTableState) {
  return sortAndFilter(rows, { ...state, sorts: [], page: 0, pageSize: rows.length }).map((row) => row.id);
}

export function enrollmentMatches(row: EnrollmentRow, state: Pick<DataTableState, "stageFilter" | "search">) {
  const search = state.search.trim().toUpperCase();
  if (state.stageFilter && row.stage !== state.stageFilter) return false;
  if (search && !row.id.toUpperCase().includes(search)) return false;
  return true;
}

export function enrollmentSortValue(row: EnrollmentRow, key: SortKey) {
  if (key === "deadline") return row.deadline.getTime();
  return row[key];
}

export function sortAndFilter(rows: EnrollmentRow[], state: DataTableState) {
  return sortAndFilterRows(rows, {
    match: (row) => enrollmentMatches(row, state),
    sorts: state.sorts,
    getSortValue: enrollmentSortValue,
  });
}

export function pageSlice(list: EnrollmentRow[], state: DataTableState) {
  return pageRows(list, state.page, state.pageSize);
}
