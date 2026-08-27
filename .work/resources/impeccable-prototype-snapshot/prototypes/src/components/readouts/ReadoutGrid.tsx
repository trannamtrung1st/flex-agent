import type { ReactNode } from "react";
import { cx } from "../../lib/cx";

type GridColumns = 2 | 3 | 4 | 6;
type GridSpan = 1 | 2 | 3 | 4 | 5 | 6;

export function ReadoutGrid({
  label,
  columns = 6,
  children,
  className,
}: {
  label: string;
  columns?: GridColumns;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cx("readout-grid", `readout-grid--columns-${columns}`, className)} aria-label={label}>
      {children}
    </div>
  );
}

export function ReadoutGridRow({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <dl className="readout-grid-row" aria-label={label}>
      {children}
    </dl>
  );
}

export function ReadoutGridField({
  term,
  span = 1,
  children,
  className,
}: {
  term: string;
  span?: GridSpan;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cx("readout-grid-field", `readout-grid-field--span-${span}`)}>
      <dt>{term}</dt>
      <dd className={className}>{children}</dd>
    </div>
  );
}
