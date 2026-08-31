import type { ComponentProps, ReactNode } from "react";
import { OperateAreaHost } from "../../../design-system/components/plates/OperateArea";

type SampleWallOperateAreaProps = Omit<
  ComponentProps<typeof OperateAreaHost>,
  "bay" | "hostClassName" | "headClassName" | "frame" | "frameClassName" | "hug"
> & {
  children?: ReactNode;
  withSampleFrame?: boolean;
};

export function SampleWallOperateArea({
  children,
  withSampleFrame = true,
  ...rest
}: SampleWallOperateAreaProps) {
  return (
    <OperateAreaHost
      {...rest}
      hostClassName="campaigns-wall sample-wall"
      headClassName="campaigns-head"
      frameClassName={withSampleFrame ? "campaigns-frame sample-frame" : undefined}
    >
      {children}
    </OperateAreaHost>
  );
}
