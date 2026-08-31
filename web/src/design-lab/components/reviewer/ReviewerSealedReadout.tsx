import type { ComponentProps } from "react";
import { cx } from "../../../lib/cx";
import { StateReadout } from "../../../design-system";

type ReviewerSealedReadoutProps = ComponentProps<typeof StateReadout>;

export function ReviewerSealedReadout({ className, variant = "sealed", solid = true, ...rest }: ReviewerSealedReadoutProps) {
  return <StateReadout {...rest} variant={variant} solid={solid} className={cx("sealed-mark", className)} />;
}
