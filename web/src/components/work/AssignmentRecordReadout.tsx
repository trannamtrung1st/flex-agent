import type { ComponentProps } from "react";
import { cx } from "../../lib/cx";
import { StateReadout } from "../../design-system";

type AssignmentRecordReadoutProps = ComponentProps<typeof StateReadout>;

export function AssignmentRecordReadout({ className, labelClassName, ...rest }: AssignmentRecordReadoutProps) {
  return (
    <StateReadout
      {...rest}
      className={cx("assignment-record", className)}
      labelClassName={cx("assignment-record-label", labelClassName)}
    />
  );
}
