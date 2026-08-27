import { cx } from "../../lib/cx";
import type { ReactNode } from "react";

export type StateIndicatorVariant = "rest" | "live" | "sealed" | "dim";

export function StateIndicator({
  variant = "rest",
  solid = false,
  className,
}: {
  variant?: StateIndicatorVariant;
  solid?: boolean;
  className?: string;
}) {
  const modifier =
    variant === "rest"
      ? undefined
      : `state-node--${variant}${solid && (variant === "live" || variant === "sealed") ? "-solid" : ""}`;

  return <span className={cx("state-node", modifier, className)} aria-hidden="true" />;
}

export function StateReadout({
  variant = "rest",
  solid = false,
  label,
  className,
  labelClassName,
}: {
  variant?: StateIndicatorVariant;
  solid?: boolean;
  label: ReactNode;
  className?: string;
  labelClassName?: string;
}) {
  return (
    <span className={className}>
      <StateIndicator variant={variant} solid={solid} />
      <span className={labelClassName}>{label}</span>
    </span>
  );
}

export function StateRing() {
  return (
    <svg className="state-ring" viewBox="0 0 13 13" aria-hidden="true">
      <circle cx="6.5" cy="6.5" r="5.8" />
      <path d="M4 6.6l1.8 1.9L9.2 4.9" />
    </svg>
  );
}

export function RecordSeal() {
  return (
    <svg className="record-seal" viewBox="0 0 22 22" aria-hidden="true" focusable="false">
      <circle cx="11" cy="11" r="10" />
      <circle cx="11" cy="11" r="7" />
      <path d="M7.6 11.2 L10 13.6 L14.6 8.4" />
    </svg>
  );
}

export function StageBars({ stage, total, complete }: { stage: number; total: number; complete?: boolean }) {
  return (
    <div className="stage-bars" aria-hidden="true">
      {Array.from({ length: total }, (_, i) => (
        <span
          key={i}
          className={complete || i < stage - 1 ? "is-done" : !complete && i === stage - 1 ? "is-now" : undefined}
        />
      ))}
    </div>
  );
}
