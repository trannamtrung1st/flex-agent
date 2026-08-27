import type { ProductionLayoutId } from "../design-system";

export function requireProductionShellLayout(
  assigned: ProductionLayoutId | undefined,
  pathname: string,
): "management" {
  if (assigned != null && assigned !== "management") {
    throw new Error(`Production shell requires management; manifest assigned '${assigned}' for ${pathname}`);
  }
  return "management";
}
