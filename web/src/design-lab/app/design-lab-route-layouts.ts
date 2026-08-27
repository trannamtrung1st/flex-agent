import type { ApprovedLayoutId } from "../../design-system";

export const DESIGN_LAB_ROUTE_LAYOUTS = {
  "/surfaces": "reference",
  "/participant-home": "management",
  "/participant-journey": "guided-task",
  "/participant-session": "live-session",
  "/admin-console": "management",
  "/reviewer-console": "management",
  "/shared/gallery": "reference",
  "*": "reference",
} as const satisfies Record<string, ApprovedLayoutId>;

export type DesignLabRoutedPath = keyof typeof DESIGN_LAB_ROUTE_LAYOUTS;
