import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export type ReadoutListRow = {
  term: ReactNode;
  value: ReactNode;
  className?: string;
};

export function ReadoutList({
  rows,
  className = "readout-stack",
  rowClassName = "readout",
  label,
}: {
  rows: readonly ReadoutListRow[];
  className?: string;
  rowClassName?: string;
  label?: string;
}) {
  return (
    <dl className={className} aria-label={label}>
      {rows.map((row, index) => (
        <div
          className={cx(rowClassName, row.className) || undefined}
          key={index}
        >
          <dt>{row.term}</dt>
          <dd>{row.value}</dd>
        </div>
      ))}
    </dl>
  );
}
