import { Link, useLocation } from "react-router-dom";

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
  if (segment === "review-work" && index === 0) {
    return "Review work";
  }
  if (segment === "release-work" && index === 0) {
    return "Release work";
  }
  if (segment === "results" && index === 0) {
    return "Results";
  }
  if (segments[index - 1] === "activities" || segments[index - 1] === "sessions") {
    return segment;
  }
  return segment;
}

export function Breadcrumbs() {
  const location = useLocation();
  const pathname = location.pathname;

  if (pathname === "/") {
    return null;
  }

  const segments = pathname.split("/").filter(Boolean);
  const crumbs = segments.map((segment, index) => {
    const path = `/${segments.slice(0, index + 1).join("/")}`;
    return {
      path,
      label: labelForSegment(segment, index, segments),
    };
  });

  return (
    <nav className="shell-breadcrumbs" aria-label="Breadcrumb">
      <ol className="breadcrumb-list">
        <li>
          <Link to="/">Home</Link>
        </li>
        {crumbs.map((crumb) => (
          <li key={crumb.path}>
            <span className="breadcrumb-separator" aria-hidden="true">/</span>
            {crumb.path === pathname ? (
              <span aria-current="page">{crumb.label}</span>
            ) : (
              <Link to={crumb.path}>{crumb.label}</Link>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}
