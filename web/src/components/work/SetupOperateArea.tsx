import type { ComponentProps } from "react";
import { cx } from "../../lib/cx";
import { OperateArea } from "../../design-system/components/plates/OperateArea";

type SetupOperateAreaProps = Omit<ComponentProps<typeof OperateArea>, "bay">;

export function SetupOperateArea({ className, frame = "record", ...rest }: SetupOperateAreaProps) {
  return (
    <OperateArea
      {...rest}
      bay="record"
      frame={frame}
      className={cx("record-plane--setup", className)}
    />
  );
}
