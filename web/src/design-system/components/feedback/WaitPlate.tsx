import { cx } from "../../../lib/cx";

export function WaitPlate({
  label,
  note,
  inset = false,
  className,
}: {
  label: string;
  note?: string;
  inset?: boolean;
  className?: string;
}) {
  return (
    <div
      className={cx("wait-plate", inset && "wait-plate--inset", className)}
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <span className={cx("wait-mark", inset && "wait-mark--lg")} aria-hidden="true" />
      <span className="wait-plate-label">{label}</span>
      {note ? <p className="wait-plate-note">{note}</p> : null}
      <div className="scan-track is-waiting" aria-hidden="true">
        <span className="scan-fill" />
      </div>
    </div>
  );
}
