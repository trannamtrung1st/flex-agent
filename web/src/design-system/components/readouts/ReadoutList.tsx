import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export type ReadoutListRow = {
  term: ReactNode;
  value: ReactNode;
  className?: string;
};

export function ReadoutList({
  rows,
  className,
  rowClassName,
  tone = "rail",
  label,
}: {
  rows: readonly ReadoutListRow[];
  className?: string;
  rowClassName?: string;
  tone?: "rail" | "horizon";
  label?: string;
}) {
  const stackClass = className ?? (tone === "horizon" ? "readout-stack readout-stack--horizon" : "readout-stack");
  const rowClass = rowClassName ?? (tone === "horizon" ? "readout readout--horizon" : "readout");
  return (
    <dl className={stackClass} aria-label={label}>
      {rows.map((row, index) => (
        <div
          className={cx(rowClass, row.className) || undefined}
          key={index}
        >
          <dt>{row.term}</dt>
          <dd>{row.value}</dd>
        </div>
      ))}
    </dl>
  );
}
