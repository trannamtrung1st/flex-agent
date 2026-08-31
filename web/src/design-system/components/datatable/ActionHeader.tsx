import { datatableColMin, type DatatableColMin } from "./datatableColMin";

export function ActionHeader({ colMin = "action" }: { colMin?: DatatableColMin }) {
  return (
    <th scope="col" className="col-action" {...datatableColMin(colMin)}>
      <span className="visually-hidden">Actions</span>
    </th>
  );
}
