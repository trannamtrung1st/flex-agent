import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export type ReadoutListRowEmphasis = "title" | "inline";

export type ReadoutListRow = {
  term: ReactNode;
  value: ReactNode;
  emphasis?: ReadoutListRowEmphasis;
  className?: string;
};

const EMPHASIS_CLASS: Record<ReadoutListRowEmphasis, string> = {
  title: "readout--title",
  // Paint contract: clustered value + mark stays `.readout--record`.
  inline: "readout--record",
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
          className={cx(rowClass, row.emphasis && EMPHASIS_CLASS[row.emphasis], row.className) || undefined}
          key={index}
        >
          <dt>{row.term}</dt>
          <dd>{row.value}</dd>
        </div>
      ))}
    </dl>
  );
}
