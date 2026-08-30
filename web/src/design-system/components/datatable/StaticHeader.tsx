import { datatableColMin, type DatatableColMin } from "./datatableColMin";

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
    <th scope="col" className={className} {...(colMin ? datatableColMin(colMin) : {})}>
      <span className="col-head">{label}</span>
    </th>
  );
}
