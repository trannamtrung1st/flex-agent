import type { ComponentProps } from "react";
import { OperateArea } from "../../design-system/components/plates/OperateArea";

type ReviewerQueueOperateAreaProps = Omit<ComponentProps<typeof OperateArea>, "bay" | "frame">;

export function ReviewerQueueOperateArea(props: ReviewerQueueOperateAreaProps) {
  return <OperateArea bay="registry" frame="registry" {...props} />;
}
