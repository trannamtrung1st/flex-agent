import type { ProductionLayoutId } from "../design-system";

export function requireProductionShellLayout(
  assigned: ProductionLayoutId | undefined,
  pathname: string,
): "management" | "guided-task" | "live-session" {
  if (assigned === "guided-task" || assigned === "live-session") {
    return assigned;
  }
  if (assigned != null && assigned !== "management") {
    throw new Error(`Production shell does not implement '${assigned}' for ${pathname}`);
  }
  return "management";
}
