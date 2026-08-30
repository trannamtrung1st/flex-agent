import type { ProductionLayoutId } from "../design-system";

export function requireProductionShellLayout(
  assigned: ProductionLayoutId | undefined,
  pathname: string,
): "management" | "guided-task" {
  if (assigned === "guided-task") {
    return "guided-task";
  }
  if (assigned != null && assigned !== "management") {
    throw new Error(`Production shell does not implement '${assigned}' for ${pathname}`);
  }
  return "management";
}
