import type { ComponentProps } from "react";
import { cx } from "../../lib/cx";
import { ReadoutGrid } from "../../design-system";

type AssignmentInstrumentGridProps = ComponentProps<typeof ReadoutGrid>;

export function AssignmentInstrumentGrid({ className, ...rest }: AssignmentInstrumentGridProps) {
  return <ReadoutGrid {...rest} className={cx("assignment-instruments", className)} />;
}
