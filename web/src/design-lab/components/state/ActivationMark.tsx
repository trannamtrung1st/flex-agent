import { cx } from "../../../lib/cx";
import { StateReadout } from "../../../design-system/components/state/StateIndicator";

/** Full sentence in context bands; compact Frozen/Draft in table cells and filters. */
export function ActivationMark({
  frozen,
  placement = "inline",
  className,
  labelClassName,
  compact = false,
}: {
  frozen: boolean;
  /** Grid cells use centered state-label styling in readout grids. */
  placement?: "inline" | "grid";
  className?: string;
  labelClassName?: string;
  compact?: boolean;
}) {
  return (
    <StateReadout
      variant={frozen ? "sealed" : "dim"}
      solid={frozen}
      label={compact ? (frozen ? "Frozen" : "Draft") : frozen ? "Frozen at activation" : "Draft — not activated"}
      className={cx(placement === "grid" && "readout-grid-state", className)}
      labelClassName={labelClassName ?? (compact ? "state-label" : undefined)}
    />
  );
}
