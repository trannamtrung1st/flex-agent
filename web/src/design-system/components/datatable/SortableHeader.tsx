import { datatableColMin, type DatatableColMin } from "./datatableColMin";

export type SortDirection = "asc" | "desc";

export function SortableHeader<TKey extends string>({
  sortKey,
  label,
  sorts,
  onSort,
  colMin,
}: {
  sortKey: TKey;
  label: string;
  sorts: readonly { key: TKey; dir: SortDirection }[];
  onSort: (key: TKey) => void;
  colMin?: DatatableColMin;
}) {
  const index = sorts.findIndex((sort) => sort.key === sortKey);
  const ariaSort =
    index === -1 ? "none" : sorts[index].dir === "asc" ? "ascending" : "descending";

  return (
    <th scope="col" data-sort={sortKey} aria-sort={ariaSort} {...(colMin ? datatableColMin(colMin) : {})}>
      <button className="col-key" type="button" onClick={() => onSort(sortKey)}>
        {label}
        {index !== -1 && sorts.length > 1 ? (
          <span className="col-key-rank">{index + 1}</span>
        ) : null}
      </button>
    </th>
  );
}
