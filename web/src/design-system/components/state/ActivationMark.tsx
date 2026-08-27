import { StateReadout } from "./StateIndicator";

/** Full sentence in context bands; compact Frozen/Draft in table cells and filters. */
export function ActivationMark({
  frozen,
  className,
  labelClassName,
  compact = false,
}: {
  frozen: boolean;
  className?: string;
  labelClassName?: string;
  compact?: boolean;
}) {
  return (
    <StateReadout
      variant={frozen ? "sealed" : "dim"}
      solid={frozen}
      label={compact ? (frozen ? "Frozen" : "Draft") : frozen ? "Frozen at activation" : "Draft — not activated"}
      className={className}
      labelClassName={labelClassName}
    />
  );
}
