import type { ReactNode } from "react";

/** Assignment heading status band: phase + record columns for guided-task chrome. */
export function AssignmentStatusReadout({
  phase,
  record,
  "aria-label": ariaLabel = "Assignment status",
}: {
  phase: ReactNode;
  record: ReactNode;
  "aria-label"?: string;
}) {
  return (
    <dl className="status-readout" aria-label={ariaLabel}>
      <div className="status-item">
        <dt>Phase</dt>
        <dd>{phase}</dd>
      </div>
      <div className="status-item">
        <dt>Record</dt>
        <dd>{record}</dd>
      </div>
    </dl>
  );
}
