import type { ComponentProps } from "react";
import { cx } from "../../../lib/cx";
import { OperateAreaHost } from "../../../design-system/components/plates/OperateArea";

type HomeBoardOperateAreaProps = Omit<
  ComponentProps<typeof OperateAreaHost>,
  "bay" | "hostClassName" | "hug"
> & {
  hug?: "board";
};

export function HomeBoardOperateArea({ hug, className, ...rest }: HomeBoardOperateAreaProps) {
  return (
    <OperateAreaHost
      {...rest}
      hostClassName="workspace-area board"
      className={cx(hug === "board" && "assignment-board--hug", className)}
    />
  );
}
