import type { ComponentProps } from "react";
import { cx } from "../../lib/cx";
import { OperateArea } from "../../design-system/components/plates/OperateArea";

type AssignmentBoardOperateAreaProps = Omit<
  ComponentProps<typeof OperateArea>,
  "bay" | "hug"
> & {
  hug?: "board";
};

export function AssignmentBoardOperateArea({
  hug,
  className,
  ...rest
}: AssignmentBoardOperateAreaProps) {
  return (
    <OperateArea
      {...rest}
      className={cx(hug === "board" && "assignment-board--hug", className)}
    />
  );
}
