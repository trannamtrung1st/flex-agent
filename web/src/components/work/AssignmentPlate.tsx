import type { ReactNode } from "react";
import { cx } from "../../lib/cx";
import { ReadoutList, type ReadoutListRow } from "../../design-system";

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
      className={cx("assignment-plate", released && "assignment-plate--released")}
      aria-label={label}
    >
      <div className="assignment-plate-in">
        <ReadoutList className="assignment-plate-readout" rowClassName="assignment-plate-row" rows={rows} />
        <div className={cx("assignment-plate-keys", !action && "assignment-plate-keys--empty")}>
          {action}
        </div>
      </div>
    </article>
  );
}
