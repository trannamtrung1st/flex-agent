import type { ComponentProps } from "react";
import { OperateAreaHost } from "../../../design-system/components/plates/OperateArea";
import { registryHugClassName } from "./registryHugClassName";

type ReviewerQueueOperateAreaProps = Omit<
  ComponentProps<typeof OperateAreaHost>,
  "bay" | "hostClassName" | "frame" | "frameClassName" | "frameInset"
>;

export function ReviewerQueueOperateArea({ hug, className, ...rest }: ReviewerQueueOperateAreaProps) {
  return (
    <OperateAreaHost
      {...rest}
      hostClassName="workspace-area queue-view"
      frame="datatable"
      className={registryHugClassName(hug, className)}
    />
  );
}
