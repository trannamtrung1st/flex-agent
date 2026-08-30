import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Inline } from "../layout/Inline";
import type { LayoutJustify } from "../layout/types";
import { StateIndicator } from "../state/StateIndicator";

export type EtchedFrameTicks = "both" | "bottom";
export type EtchedFrameInset = "default" | "flush";

export function EtchedFrame({
  children,
  className,
  revealing,
  sealing,
  ticks = "bottom",
  inset = "default",
}: {
  children: ReactNode;
  className?: string;
  revealing?: boolean;
  sealing?: boolean;
  ticks?: EtchedFrameTicks;
  inset?: EtchedFrameInset;
}) {
  return (
    <div
      className={cx(
        "frame-cut",
        inset === "flush" && "frame-cut--flush",
        className,
        revealing && "is-revealing",
        sealing && "is-sealing",
      )}
    >
      <div className="frame-in">
        {ticks === "both" ? <span className="frame-tick frame-tick--top" aria-hidden="true" /> : null}
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
  noteRole,
  children,
  className,
  inset,
}: {
  id?: string;
  label?: string;
  note: string;
  noteRole?: "alert" | "status";
  children?: ReactNode;
  className?: string;
  inset?: boolean;
}) {
  return (
    <div id={id} className={cx("empty-plate", inset && "empty-plate--inset", className)}>
      <StateIndicator />
      {label ? <span className="empty-plate-label">{label}</span> : null}
      <p className="empty-plate-note" role={noteRole}>{note}</p>
      {children}
    </div>
  );
}

export type PlateFootArrangement = "start" | "center" | "end" | "split";

const ARRANGEMENT_JUSTIFY: Record<PlateFootArrangement, LayoutJustify> = {
  start: "start",
  center: "center",
  end: "end",
  split: "between",
};

export function PlateFoot({
  children,
  className,
  arrangement = "end",
  hairline = true,
  secondary,
  primary,
}: {
  children?: ReactNode;
  className?: string;
  arrangement?: PlateFootArrangement;
  /** In-plate stratum divider. Hull chrome (guided-task bay sibling) sets false. */
  hairline?: boolean;
  secondary?: ReactNode;
  primary?: ReactNode;
}) {
  const cluster = arrangement === "split" ? (
    <>
      <div className="plate-foot-slot plate-foot-slot--secondary">{secondary}</div>
      <div className="plate-foot-slot plate-foot-slot--primary">{primary ?? children}</div>
    </>
  ) : (
    children
  );

  return (
    <Inline
      as="footer"
      className={cx("plate-foot", className)}
      gap="2"
      align="center"
      justify={ARRANGEMENT_JUSTIFY[arrangement]}
      wrap
      data-arrangement={arrangement}
      data-hairline={hairline ? "true" : "false"}
    >
      {cluster}
    </Inline>
  );
}
