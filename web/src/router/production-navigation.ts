import type { GangwayGroup } from "../design-system";

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
    .filter((destination) => available.has(destination.id));
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
