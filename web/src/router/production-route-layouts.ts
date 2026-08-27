import type { ProductionLayoutId } from "../design-system";

export const PRODUCTION_ROUTE_LAYOUTS = {
  "/": "management",
  "/activities": "management",
  "/activities/:activityId/setup": "management",
  "/my-work": "management",
  "/my-work/:enrollmentId": "management",
} as const satisfies Record<string, ProductionLayoutId>;

export type ProductionRoutedPath = keyof typeof PRODUCTION_ROUTE_LAYOUTS;
