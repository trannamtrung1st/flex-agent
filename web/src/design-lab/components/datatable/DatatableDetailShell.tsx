import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { ChevronGlyph } from "../../../design-system/components/glyphs/ChevronGlyph";
import { IconButton } from "../../../design-system/components/keys/IconButton";

export function DatatableIdCell({
  expand,
  children,
}: {
  expand?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="datatable-id-cell">
      {expand}
      {children}
    </div>
  );
}

export function DatatableExpandButton({
  expanded,
  controls,
  label,
  onClick,
}: {
  expanded: boolean;
  controls?: string;
  label: string;
  onClick: () => void;
}) {
  return (
    <IconButton
      className={cx("command-menu-trigger", "command-menu-trigger--icon", expanded && "is-open")}
      label={label}
      expanded={expanded}
      controls={controls}
      onClick={onClick}
    >
      <ChevronGlyph />
    </IconButton>
  );
}

export function DatatableDetailRow({
  colSpan,
  children,
  className,
  plateClassName,
  id,
}: {
  colSpan: number;
  children: ReactNode;
  className?: string;
  plateClassName?: string;
  id?: string;
}) {
  return (
    <tr className={cx("datatable-detail", className)}>
      <td colSpan={colSpan}>
        <div className="datatable-detail-cut is-revealing" id={id}>
          <div className={cx("datatable-detail-plate", plateClassName)}>
            {children}
          </div>
        </div>
      </td>
    </tr>
  );
}

export function DatatableDetailBody({ children }: { children: ReactNode }) {
  return <div className="datatable-detail-body">{children}</div>;
}
