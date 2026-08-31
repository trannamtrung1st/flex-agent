import { datatableColMin, type DatatableColMin } from "./datatableColMin";
import { cx } from "../../../lib/cx";

export function StaticHeader({
  label,
  colMin,
  className,
}: {
  label: string;
  colMin?: DatatableColMin;
  className?: string;
}) {
  return (
    <th
      scope="col"
      className={cx(
        colMin === "action" && "col-action",
        colMin === "state" && "col-state",
        className,
      )}
      {...(colMin ? datatableColMin(colMin) : {})}
    >
      <span className="col-head">{label}</span>
    </th>
  );
}
