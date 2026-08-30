import type { ProductionLayoutId } from "../design-system";
import { layoutIdForPath } from "./route-layout-match";

export const PRODUCTION_ROUTE_LAYOUTS = {
  "/": "management",
  "/activities": "management",
  "/activities/new": "management",
  "/activities/:activityId/setup": "management",
  "/activities/:activityId/cohorts/:cohortId/enrollments": "management",
  "/activities/:activityId/cohorts/:cohortId/enrollments/:enrollmentId": "management",
  "/my-work": "management",
  "/my-work/:enrollmentId": "guided-task",
  "/sessions/:sessionId": "management",
  "/review": "management",
  "/review/:reviewId": "management",
  "/release": "management",
  "/release/:resultId": "management",
  "/results": "management",
  "/results/:resultId": "management",
  "*": "management",
} as const satisfies Record<string, ProductionLayoutId>;

export type ProductionRoutedPath = keyof typeof PRODUCTION_ROUTE_LAYOUTS;

const KNOWN_PRODUCTION_ROUTE_LAYOUTS = Object.fromEntries(
  Object.entries(PRODUCTION_ROUTE_LAYOUTS).filter(([path]) => path !== "*"),
) as Omit<typeof PRODUCTION_ROUTE_LAYOUTS, "*">;

export function isKnownProductionLocator(pathname: string): boolean {
  return layoutIdForPath(pathname, KNOWN_PRODUCTION_ROUTE_LAYOUTS) != null;
}
