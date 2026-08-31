import { useMemo } from "react";

export type SortSpec<K extends string> = { key: K; dir: "asc" | "desc" };

export type TableControllerOptions<T, K extends string = string> = {
  rows: readonly T[];
  match?: (row: T) => boolean;
  sorts?: SortSpec<K>[];
  page: number;
  pageSize: number;
  getSortValue?: (row: T, key: K) => string | number;
};

/**
 * Shared table sort/filter/page math. Domain match and sort-value
 * extractors stay in the consumer (`enrollment/tableLogic`, `campaignRegistryLogic`).
 */
export function sortAndFilterRows<T, K extends string>(
  rows: readonly T[],
  options: {
    match?: (row: T) => boolean;
    sorts?: SortSpec<K>[];
    getSortValue?: (row: T, key: K) => string | number;
  },
) {
  let list = rows.slice();
  if (options.match) list = list.filter(options.match);
  const sorts = options.sorts ?? [];
  const getSortValue = options.getSortValue;
  if (sorts.length && getSortValue) {
    list.sort((a, b) => {
      for (const spec of sorts) {
        const dir = spec.dir === "asc" ? 1 : -1;
        const av = getSortValue(a, spec.key);
        const bv = getSortValue(b, spec.key);
        if (av < bv) return -dir;
        if (av > bv) return dir;
      }
      return 0;
    });
  }
  return list;
}

export function pageRows<T>(list: readonly T[], page: number, pageSize: number) {
  const total = list.length;
  const maxPage = Math.max(0, Math.ceil(total / pageSize) - 1);
  const resolvedPage = Math.min(page, maxPage);
  const startIdx = resolvedPage * pageSize;
  return {
    total,
    maxPage,
    pageCount: total === 0 ? 0 : maxPage + 1,
    startIdx,
    pageRows: list.slice(startIdx, startIdx + pageSize),
    page: resolvedPage,
  };
}

/** Memoized `sortAndFilterRows` + `pageRows` for React table surfaces. */
export function useTableController<T, K extends string>({
  rows,
  match,
  sorts,
  page,
  pageSize,
  getSortValue,
}: TableControllerOptions<T, K>) {
  const visibleRows = useMemo(
    () => sortAndFilterRows(rows, { match, sorts, getSortValue }),
    [getSortValue, match, rows, sorts],
  );
  const slice = useMemo(() => pageRows(visibleRows, page, pageSize), [page, pageSize, visibleRows]);
  return {
    visibleRows,
    ...slice,
  };
}
