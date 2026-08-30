import { useLocation } from "react-router-dom";
import { BreadcrumbNav, type BreadcrumbNavItem } from "../../design-system";
import { isKnownProductionLocator } from "../../router/production-route-layouts";

const ACTIVITIES: BreadcrumbNavItem = { label: "Activities", href: "/activities" };

function productionBreadcrumbItems(pathname: string): BreadcrumbNavItem[] | null {
  if (pathname === "/" || !isKnownProductionLocator(pathname)) {
    return null;
  }

  if (pathname === "/activities") {
    return [{ label: "Activities", current: true }];
  }

  if (pathname === "/activities/new") {
    return [ACTIVITIES, { label: "Create assessment Campaign", current: true }];
  }

  if (/^\/activities\/[^/]+$/.test(pathname)) {
    return [ACTIVITIES, { label: "Setup and readiness", current: true }];
  }

  const setup = pathname.match(/^\/activities\/([^/]+)\/setup$/);
  if (setup) {
    return [ACTIVITIES, { label: "Setup and readiness", current: true }];
  }

  const roster = pathname.match(/^\/activities\/([^/]+)\/cohorts\/([^/]+)\/enrollments$/);
  if (roster) {
    const activityId = roster[1]!;
    return [
      ACTIVITIES,
      { label: "Setup and readiness", href: `/activities/${activityId}/setup` },
      { label: "Participants", current: true },
    ];
  }

  const enrollment = pathname.match(/^\/activities\/([^/]+)\/cohorts\/([^/]+)\/enrollments\/([^/]+)$/);
  if (enrollment) {
    const activityId = enrollment[1]!;
    const cohortId = enrollment[2]!;
    return [
      ACTIVITIES,
      { label: "Setup and readiness", href: `/activities/${activityId}/setup` },
      {
        label: "Participants",
        href: `/activities/${activityId}/cohorts/${cohortId}/enrollments`,
      },
      { label: "Enrollment", current: true },
    ];
  }

  if (pathname === "/my-work") {
    return [{ label: "My work", current: true }];
  }

  if (/^\/my-work\/[^/]+$/.test(pathname)) {
    return [{ label: "My work", href: "/my-work" }, { label: "Assignment", current: true }];
  }

  if (pathname === "/review") {
    return [{ label: "Review work", current: true }];
  }

  if (/^\/review\/[^/]+$/.test(pathname)) {
    return [{ label: "Review work", href: "/review" }, { label: "Review case", current: true }];
  }

  if (pathname === "/release") {
    return [{ label: "Release work", current: true }];
  }

  if (/^\/release\/[^/]+$/.test(pathname)) {
    return [{ label: "Release work", href: "/release" }, { label: "Release Result", current: true }];
  }

  if (pathname === "/results") {
    return [{ label: "Results", current: true }];
  }

  if (/^\/results\/[^/]+$/.test(pathname)) {
    return [{ label: "Results", href: "/results" }, { label: "Result", current: true }];
  }

  if (/^\/sessions\/[^/]+$/.test(pathname)) {
    return [{ label: "Session", current: true }];
  }

  return null;
}

export function Breadcrumbs() {
  const { pathname } = useLocation();
  const items = productionBreadcrumbItems(pathname);
  if (items == null || items.length === 0) {
    return null;
  }

  return <BreadcrumbNav items={items} />;
}
