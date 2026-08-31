import { cx } from "../../../lib/cx";

export type OperateBay =
  | "workspace"
  | "record"
  | "registry"
  | "ceremony";

export type OperateHug = "registry";

/** Visible matching rows at or below this count hug the etched plate; longer lists fill. */
export const REGISTRY_TABLE_HUG_MAX_ROWS = 4;

export function registryTableHug(visibleRowCount: number): OperateHug | undefined {
  return visibleRowCount <= REGISTRY_TABLE_HUG_MAX_ROWS ? "registry" : undefined;
}

const REGISTRY_TABLE_HUG_BAYS: ReadonlySet<OperateBay> = new Set(["registry"]);

const BAY_CLASSES: Record<OperateBay, string> = {
  workspace: "workspace-area work-plane",
  record: "workspace-area work-plane record-plane",
  registry: "workspace-area work-plane registry-wall",
  ceremony: "workspace-area work-plane work-plane--ceremony",
};

export function operateAreaClass(
  bay: OperateBay = "workspace",
  options?: {
    hug?: OperateHug;
    danger?: boolean;
    className?: string;
    hostClassName?: string;
  },
) {
  const host = options?.hostClassName;
  const hug =
    !host && options?.hug === "registry" && REGISTRY_TABLE_HUG_BAYS.has(bay)
      ? "registry-wall--hug"
      : undefined;

  return cx(host ?? BAY_CLASSES[bay], hug, options?.danger && "workspace-area--danger", options?.className);
}
