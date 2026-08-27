import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { StateIndicator } from "../state/StateIndicator";

export function EtchedFrame({
  children,
  className,
  revealing,
  sealing,
}: {
  children: ReactNode;
  className?: string;
  revealing?: boolean;
  sealing?: boolean;
}) {
  return (
    <div className={cx("frame-cut", className, revealing && "is-revealing", sealing && "is-sealing")}>
      <div className="frame-in">
        <span className="frame-tick frame-tick--top" aria-hidden="true" />
        <span className="frame-tick frame-tick--bottom" aria-hidden="true" />
        <span className="frame-node frame-node--tr" aria-hidden="true" />
        <span className="frame-node frame-node--br" aria-hidden="true" />
        {children}
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
