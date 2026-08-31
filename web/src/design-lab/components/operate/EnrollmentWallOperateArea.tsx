import type { ComponentProps } from "react";
import { OperateAreaHost } from "../../../design-system/components/plates/OperateArea";
import { registryHugClassName } from "./registryHugClassName";

type EnrollmentWallOperateAreaProps = Omit<
  ComponentProps<typeof OperateAreaHost>,
  "bay" | "hostClassName" | "headClassName" | "frame" | "frameClassName" | "frameInset"
> & {
  variant?: "table" | "plain";
};

export function EnrollmentWallOperateArea({
  variant = "table",
  hug,
  className,
  ...rest
}: EnrollmentWallOperateAreaProps) {
  const hugClassName = registryHugClassName(hug, className);

  if (variant === "plain") {
    return (
      <OperateAreaHost
        {...rest}
        hostClassName="wall"
        headClassName="wall-head"
        className={hugClassName}
      />
    );
  }

  return (
    <OperateAreaHost
      {...rest}
      hostClassName="wall"
      headClassName="wall-head"
      className={hugClassName}
      frameClassName="datatable-frame wall-frame"
      frameInset="flush"
    />
  );
}
