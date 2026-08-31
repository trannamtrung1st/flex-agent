import type { ComponentProps } from "react";
import { OperateAreaHost } from "../../../design-system/components/plates/OperateArea";
import { registryHugClassName } from "./registryHugClassName";

type CampaignsOperateAreaProps = Omit<
  ComponentProps<typeof OperateAreaHost>,
  "bay" | "hostClassName" | "headClassName" | "frame" | "frameClassName" | "frameInset"
> & {
  variant: "registry" | "record" | "plain";
};

export function CampaignsOperateArea({ variant, hug, className, ...rest }: CampaignsOperateAreaProps) {
  const hugClassName = registryHugClassName(hug, className);

  if (variant === "registry") {
    return (
      <OperateAreaHost
        {...rest}
        hostClassName="campaigns-wall"
        headClassName="campaigns-head"
        className={hugClassName}
        frameClassName="datatable-frame campaigns-registry-frame"
        frameInset="flush"
      />
    );
  }

  if (variant === "record") {
    return (
      <OperateAreaHost
        {...rest}
        hostClassName="campaigns-wall"
        headClassName="campaigns-head"
        className={hugClassName}
        frameClassName="campaigns-frame"
      />
    );
  }

  return (
    <OperateAreaHost
      {...rest}
      hostClassName="campaigns-wall"
      headClassName="campaigns-head"
      className={hugClassName}
    />
  );
}
