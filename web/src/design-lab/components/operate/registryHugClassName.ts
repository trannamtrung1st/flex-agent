import { cx } from "../../../lib/cx";
import type { OperateHug } from "../../../design-system/components/plates/operateAreaClass";

export function registryHugClassName(hug?: OperateHug, className?: string) {
  return cx(hug === "registry" && "registry-wall--hug", className);
}
