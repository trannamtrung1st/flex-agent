import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { StateIndicator } from "../state/StateIndicator";

export type EtchedFrameTicks = "both" | "bottom";
export type EtchedFrameInset = "default" | "flush";

const FULL_TICK_FRAME_CLASSES = new Set(["frame-demo"]);

/** Register frame class names that must sit flush inside the etched hairline. */
const FLUSH_FRAME_CLASSES = new Set(["board-frame", "datatable-frame"]);

/** Operational plates omit the top center tick; gallery frame-demo keeps both. */
export function resolveFrameTicks(className?: string): EtchedFrameTicks {
  const tokens = className?.split(/\s+/) ?? [];
  return tokens.some((token) => FULL_TICK_FRAME_CLASSES.has(token)) ? "both" : "bottom";
}

/**
 * Full-bleed work bays (status columns, datatables) sit flush inside the etched hairline.
 * Add a frame class to `FLUSH_FRAME_CLASSES` when a new surface needs flush; pass `inset`
 * explicitly to override auto-resolution.
 */
export function resolveFrameInset(className?: string): EtchedFrameInset {
  const tokens = className?.split(/\s+/) ?? [];
  return tokens.some((token) => FLUSH_FRAME_CLASSES.has(token)) ? "flush" : "default";
}

export function EtchedFrame({
  children,
  className,
  revealing,
  sealing,
  ticks,
  inset,
}: {
  children: ReactNode;
  className?: string;
  revealing?: boolean;
  sealing?: boolean;
  ticks?: EtchedFrameTicks;
  inset?: EtchedFrameInset;
}) {
  const resolvedTicks = ticks ?? resolveFrameTicks(className);
  const resolvedInset = inset ?? resolveFrameInset(className);

  return (
    <div
      className={cx(
        "frame-cut",
        resolvedInset === "flush" && "frame-cut--flush",
        className,
        revealing && "is-revealing",
        sealing && "is-sealing",
      )}
    >
      <div className="frame-in">
        {resolvedTicks === "both" ? <span className="frame-tick frame-tick--top" aria-hidden="true" /> : null}
        <span className="frame-tick frame-tick--bottom" aria-hidden="true" />
        <span className="frame-node frame-node--tr" aria-hidden="true" />
        <span className="frame-node frame-node--br" aria-hidden="true" />
        <div className="frame-scroll">{children}</div>
      </div>
    </div>
  );
}

export function EmptyPlate({
  id,
  label,
  note,
  children,
  className,
}: {
  id?: string;
  label: string;
  note: string;
  children?: ReactNode;
  className?: string;
}) {
  return (
    <div id={id} className={cx("empty-plate", className)}>
      <StateIndicator />
      <span className="empty-plate-label">{label}</span>
      <p className="empty-plate-note">{note}</p>
      {children}
    </div>
  );
}

export function PlateFoot({ children, className }: { children: ReactNode; className?: string }) {
  return <footer className={cx("plate-foot", className)}>{children}</footer>;
}
