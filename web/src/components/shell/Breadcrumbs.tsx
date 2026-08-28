import { useLocation } from "react-router-dom";
import { BreadcrumbNav, type BreadcrumbNavItem } from "../../design-system";
import { isKnownProductionLocator } from "../../router/production-route-layouts";

const ResourceLocator = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function labelForLocator(previous: string | undefined): string {
  switch (previous) {
    case "activities":
      return "Activity";
    case "cohorts":
      return "Cohort";
    case "enrollments":
      return "Enrollment";
    case "my-work":
      return "Assignment";
    case "sessions":
      return "Session";
    default:
      return "Item";
  }
}

function labelForSegment(segment: string, index: number, segments: string[]): string {
  if (ResourceLocator.test(segment)) {
    return labelForLocator(segments[index - 1]);
  }

  if (segment === "activities" && index === 0) {
    return "Activities";
  }
  if (segment === "enrollment") {
    return "Enrollment";
  }
  if (segment === "setup") {
    return "Setup and readiness";
  }
  if (segment === "participants") {
    return "Assign Participants";
  }
  if (segment === "cohorts") {
    return "Cohorts";
  }
  if (segment === "enrollments") {
    return "Enrollment";
  }
  if (segment === "my-work" && index === 0) {
    return "My work";
  }
  if (segment === "sessions") {
    return "Session";
  }
  if (segment === "review" && index === 0) {
    return "Review work";
  }
  if (segment === "release" && index === 0) {
    return "Release work";
  }
  if (segment === "results" && index === 0) {
    return "Results";
  }
  if (segments[index - 1] === "activities" || segments[index - 1] === "sessions") {
    return labelForLocator(segments[index - 1]);
  }
  return segment;
}

function hrefForCrumb(path: string, label: string, previous: string | undefined): string {
  if (label === "Activity" && previous === "activities") {
    return "/activities";
  }
  if (label === "Cohorts") {
    const activityMatch = path.match(/^(\/activities\/[^/]+)\/cohorts$/);
    if (activityMatch) {
      return `${activityMatch[1]}/setup`;
    }
  }
  if (label === "Cohort" && previous === "cohorts") {
    return `${path}/enrollments`;
  }
  if (label === "Assignment" && previous === "my-work") {
    return "/my-work";
  }
  if (label === "Session" && previous === "sessions") {
    return path.replace(/\/[^/]+$/, "") || path;
  }
  return path;
}

export function Breadcrumbs() {
  const location = useLocation();
  const pathname = location.pathname;

  if (pathname === "/" || !isKnownProductionLocator(pathname)) {
    return null;
  }

  const segments = pathname.split("/").filter(Boolean);
  const items: BreadcrumbNavItem[] = segments.map((segment, index) => {
    const path = `/${segments.slice(0, index + 1).join("/")}`;
    const label = labelForSegment(segment, index, segments);
    const current = path === pathname;
    return {
      label,
      href: current ? undefined : hrefForCrumb(path, label, segments[index - 1]),
      current,
    };
  });

  return <BreadcrumbNav items={items} />;
}
