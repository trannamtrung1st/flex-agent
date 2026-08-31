import { forwardRef, type ComponentPropsWithoutRef, type ReactNode, type TableHTMLAttributes, type TdHTMLAttributes } from "react";
import { Link, type To } from "react-router-dom";
import { cx } from "../../../lib/cx";
import { KeyGroup } from "../keys/KeyGroup";
import { EmptyPlate } from "../plates/EtchedFrame";
import { StateReadout } from "../state/StateIndicator";
import { datatableColMin, type DatatableColMin } from "./datatableColMin";

export type DatatableCellKind = "id" | "content" | "state" | "select" | "action";

const CELL_KIND_CLASS: Record<DatatableCellKind, string> = {
  id: "cell-id",
  content: "cell-content",
  state: "cell-state",
  select: "cell-select",
  action: "col-action",
};

export const DatatableTable = forwardRef<
  HTMLTableElement,
  TableHTMLAttributes<HTMLTableElement> & { caption?: string }
>(function DatatableTable({ caption, className, children, ...rest }, ref) {
  return (
    <table ref={ref} className={cx("datatable-table", className)} {...rest}>
      {caption ? <caption className="visually-hidden">{caption}</caption> : null}
      {children}
    </table>
  );
});

export function DatatableRow({
  selected,
  expanded,
  className,
  children,
  ...rest
}: ComponentPropsWithoutRef<"tr"> & { selected?: boolean; expanded?: boolean }) {
  return (
    <tr
      className={cx("datatable-row", selected && "is-selected", expanded && "is-expanded", className)}
      {...rest}
    >
      {children}
    </tr>
  );
}

export function DatatableCell({
  kind,
  colMin,
  className,
  children,
  label,
  ...rest
}: TdHTMLAttributes<HTMLTableCellElement> & {
  kind: DatatableCellKind;
  colMin?: DatatableColMin;
  label?: string;
}) {
  return (
    <td
      className={cx(
        CELL_KIND_CLASS[kind],
        colMin === "result" && kind === "content" && "cell-result",
        className,
      )}
      data-label={label}
      {...(colMin ? datatableColMin(colMin) : {})}
      {...rest}
    >
      {children}
    </td>
  );
}

export function DatatableId({
  to,
  onClick,
  children,
  className,
}: {
  to?: To;
  onClick?: () => void;
  children: ReactNode;
  className?: string;
}) {
  if (to) {
    return (
      <Link className={cx("datatable-id", className)} to={to}>
        {children}
      </Link>
    );
  }
  return (
    <button type="button" className={cx("datatable-id", className)} onClick={onClick}>
      {children}
    </button>
  );
}

export function DatatableActions({
  children,
  id,
}: {
  children: ReactNode;
  id?: string;
}) {
  return (
    <div className="datatable-actions" id={id} aria-label="Table actions">
      <KeyGroup className="datatable-actions-keys" justify="end">
        {children}
      </KeyGroup>
    </div>
  );
}

export function DatatableEmpty({
  className,
  ...props
}: Parameters<typeof EmptyPlate>[0]) {
  return <EmptyPlate {...props} className={cx("datatable-empty", className)} />;
}

export function DatatableStateReadout({
  labelClassName = "state-label",
  ...props
}: ComponentPropsWithoutRef<typeof StateReadout>) {
  return <StateReadout labelClassName={labelClassName} {...props} />;
}
