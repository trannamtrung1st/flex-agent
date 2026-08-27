import { NavLink, Outlet } from "react-router-dom";
import { useProductionApi } from "../../api/production-api";
import { Button } from "../ui/Button";
import { ProtectedLoading } from "../ui/ProtectedLoading";
import { StatusPanel } from "../ui/StatusPanel";
import { Breadcrumbs } from "./Breadcrumbs";
import { ThemeToggle } from "./ThemeToggle";

const DESTINATION_ROUTES: Record<string, { label: string; route: string }> = {
  home: { label: "Home", route: "/" },
  activities: { label: "Activities", route: "/activities" },
  "my-work": { label: "My work", route: "/my-work" },
};

export function ProductionAppShell() {
  const { apiState, errorMessage, logout, shell } = useProductionApi();

  if (apiState === "loading") {
    return <ProtectedLoading label="Establishing session context…" />;
  }

  if (apiState === "signing-out") {
    return (
      <div className="shell-content" style={{ padding: "2rem 1.25rem" }}>
        <StatusPanel title="Signing out">
          <p role={errorMessage ? "alert" : undefined}>{errorMessage ?? "Signing out…"}</p>
          {errorMessage ? (
            <p>
              <Button type="button" onClick={() => { void logout(); }}>Try again</Button>
            </p>
          ) : null}
        </StatusPanel>
      </div>
    );
  }

  if (apiState === "denied") {
    return (
      <div className="shell-content" style={{ padding: "2rem 1.25rem" }}>
        <StatusPanel title="Your access changed" variant="danger">
          <p>{errorMessage ?? "You do not have access to this content."}</p>
        </StatusPanel>
      </div>
    );
  }

  const destinations = [];
  for (const item of shell?.navigation ?? []) {
    if (!item.is_available) {
      continue;
    }

    if (item.destination_id !== "home" && item.destination_id !== "activities" && item.destination_id !== "my-work") {
      continue;
    }

    destinations.push({ id: item.destination_id, ...DESTINATION_ROUTES[item.destination_id] });
  }

  return (
    <div className="shell">
      <a href="#main-content" className="skip-link">Skip to main content</a>

      <header className="shell-header" role="banner">
        <div className="shell-brand">
          <span className="shell-org">Organization</span>
          <span className="shell-title">Activity workspace</span>
        </div>
        <div className="shell-header-actions">
          <ThemeToggle />
          <Button type="button" variant="ghost" size="sm" onClick={() => { void logout(); }}>
            Sign out
          </Button>
        </div>
      </header>
      {apiState === "ready" && errorMessage ? (
        <p role="alert">{errorMessage}</p>
      ) : null}

      <div className="shell-nav-mobile">
        <nav aria-label="Mobile navigation">
          <ul className="nav-list">
            {destinations.map((destination) => (
              <li key={destination.id}>
                <NavLink
                  to={destination.route}
                  end={destination.route === "/"}
                  className={({ isActive }) => ["nav-link", isActive ? "nav-link-active" : ""].filter(Boolean).join(" ")}
                >
                  {destination.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </div>

      <div className="shell-body">
        <aside className="shell-nav-rail" aria-label="Workspace navigation">
          <nav aria-label="Primary navigation">
            <ul className="nav-list">
              {destinations.map((destination) => (
                <li key={destination.id}>
                  <NavLink
                    to={destination.route}
                    end={destination.route === "/"}
                    className={({ isActive }) => ["nav-link", isActive ? "nav-link-active" : ""].filter(Boolean).join(" ")}
                  >
                    {destination.label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
        </aside>

        <main id="main-content" className="shell-main">
          <div className="shell-content">
            <Breadcrumbs />
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
