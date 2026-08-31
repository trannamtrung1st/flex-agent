import { useLayoutEffect, type RefObject } from "react";

export function useDatatableDetailGutter({
  tbodyRef,
  tableRef,
  expandedId,
  dependency,
}: {
  tbodyRef: RefObject<HTMLTableSectionElement | null>;
  tableRef: RefObject<HTMLTableElement | null>;
  expandedId: string | null | undefined;
  dependency?: unknown;
}) {
  useLayoutEffect(() => {
    if (!expandedId) return;

    const detailBody = tbodyRef.current?.querySelector<HTMLElement>(".datatable-detail-body");
    if (!detailBody) return;

    const refRow = tbodyRef.current?.querySelector("tr.datatable-row");
    const cells = refRow
      ? Array.from(refRow.children)
      : Array.from(tableRef.current?.querySelectorAll("thead th") ?? []);
    const cols = cells.map((cell) => cell.getBoundingClientRect().left);
    if (!cols.length) return;

    const bodyBox = detailBody.getBoundingClientRect();
    detailBody.style.paddingLeft = `${cols[0] - bodyBox.left}px`;
  }, [dependency, expandedId, tableRef, tbodyRef]);
}
