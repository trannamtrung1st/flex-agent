import type { GangwayGroup } from "../design-system";
import { isKnownProductionLocator } from "./production-route-layouts";

export type ProductionDestinationId =
  | "home"
  | "activities"
  | "my-work"
  | "review"
  | "release"
  | "results";

export type ProductionDestination = {
  id: ProductionDestinationId;
  label: string;
  route: string;
  abbr: string;
  group: "workspace" | "outcomes";
};

export type ProductionGuardedDestinationId =
  | Exclude<ProductionDestinationId, "home">
  | "sessions";

const PRODUCTION_DESTINATION_UNAVAILABLE_COPY: Record<ProductionGuardedDestinationId, string> = {
  activities: "Activities are not available for the current authorized relationship.",
  "my-work": "My work is not available for the current authorized relationship.",
  review: "Review work is not available for the current authorized relationship.",
  release: "Release work is not available for the current authorized relationship.",
  results: "Results are not available for the current authorized relationship.",
  sessions: "Sessions are not available for the current authorized relationship.",
};

export function productionDestinationUnavailableCopy(destinationId: ProductionGuardedDestinationId): string {
  return PRODUCTION_DESTINATION_UNAVAILABLE_COPY[destinationId];
}

export const PRODUCTION_DESTINATIONS: Record<ProductionDestinationId, ProductionDestination> = {
  home: { id: "home", label: "Home", route: "/", abbr: "HOM", group: "workspace" },
  activities: { id: "activities", label: "Activities", route: "/activities", abbr: "ACT", group: "workspace" },
  "my-work": { id: "my-work", label: "My work", route: "/my-work", abbr: "WRK", group: "workspace" },
  review: { id: "review", label: "Review work", route: "/review", abbr: "REV", group: "outcomes" },
  release: { id: "release", label: "Release work", route: "/release", abbr: "REL", group: "outcomes" },
  results: { id: "results", label: "Results", route: "/results", abbr: "RST", group: "outcomes" },
};

const DESTINATION_ORDER: ProductionDestinationId[] = [
  "home",
  "activities",
  "my-work",
  "review",
  "release",
  "results",
];

const CEREMONY_LOCATOR = /^\/(sessions|review|release|results)(\/|$)/;

export function isProductionDestinationOpen(
  navigation: Array<{ destination_id: string; is_available: boolean }> | undefined,
  destinationId: "activities" | "my-work" | "review" | "release" | "results" | "sessions",
) {
  const items = navigation ?? [];
  if (destinationId === "sessions") {
    return items.some(
      (item) => item.is_available && (item.destination_id === "sessions" || item.destination_id === "my-work"),
    );
  }

  return items.some((item) => item.destination_id === destinationId && item.is_available);
}

export function shouldHideProductionBreadcrumbs(
  pathname: string,
  navigation?: Array<{ destination_id: string; is_available: boolean }>,
): boolean {
  if (pathname === "/" || !isKnownProductionLocator(pathname)) {
    return true;
  }

  if (CEREMONY_LOCATOR.test(pathname)) {
    return true;
  }

  if (pathname === "/activities" || pathname === "/my-work") {
    return true;
  }

  if (pathname.startsWith("/activities") && !isProductionDestinationOpen(navigation, "activities")) {
    return true;
  }

  if (pathname.startsWith("/my-work") && !isProductionDestinationOpen(navigation, "my-work")) {
    return true;
  }

  return false;
}

export function availableProductionDestinations(
  navigation: Array<{ destination_id: string; is_available: boolean }> | undefined,
): ProductionDestination[] {
  const available = new Set(
    (navigation ?? [])
      .filter((item) => item.is_available)
      .map((item) => item.destination_id),
  );

  return DESTINATION_ORDER
    .map((id) => PRODUCTION_DESTINATIONS[id])
    .filter((destination) => available.has(destination.id))
    .filter((destination) => !(destination.id === "home" && available.has("my-work")));
}

export function productionWorkspaceHome(
  navigation: Array<{ destination_id: string; is_available: boolean }> | undefined,
): string {
  return isProductionDestinationOpen(navigation, "my-work")
    ? PRODUCTION_DESTINATIONS["my-work"].route
    : PRODUCTION_DESTINATIONS.home.route;
}

export function productionNavGroups(
  destinations: ProductionDestination[],
  pathname: string,
): GangwayGroup[] {
  const toItem = (destination: ProductionDestination) => ({
    to: destination.route,
    label: destination.label,
    abbr: destination.abbr,
    current:
      destination.route === "/"
        ? pathname === "/"
        : pathname === destination.route || pathname.startsWith(`${destination.route}/`),
  });

  const groups: GangwayGroup[] = [];
  const workspace = destinations.filter((destination) => destination.group === "workspace");
  const outcomes = destinations.filter((destination) => destination.group === "outcomes");

  if (workspace.length > 0) {
    groups.push({
      id: "workspace",
      label: "Workspace",
      items: workspace.map(toItem),
    });
  }

  if (outcomes.length > 0) {
    groups.push({
      id: "outcomes",
      label: "Outcomes",
      items: outcomes.map(toItem),
    });
  }

  return groups;
}
