import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { PlateFoot } from "./EtchedFrame";
import { ReadoutList, type ReadoutListRow } from "../readouts/ReadoutList";

export function AssignmentPlate({
  label,
  released = false,
  rows,
  action,
}: {
  label: string;
  released?: boolean;
  rows: readonly ReadoutListRow[];
  action?: ReactNode;
}) {
  return (
    <article
      className={cx("assignment-plate", "frame-cut", released && "assignment-plate--released")}
      aria-label={label}
    >
      <div className="frame-in assignment-plate-in">
        <ReadoutList tone="horizon" rows={rows} />
        <PlateFoot
          className={cx("assignment-plate-keys", !action && "assignment-plate-keys--empty")}
          arrangement="end"
        >
          {action}
        </PlateFoot>
      </div>
    </article>
  );
}
