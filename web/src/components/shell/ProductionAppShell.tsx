import { useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import {
  AreaGroupList,
  Bulkhead,
  CommandStrip,
  Gangway,
  Key,
  type GangwayGroup,
  type OperatorIdentity,
  type OperatorRole,
} from "../../design-system";
import { maxWidthQuery } from "../../lib/breakpoints";
import { useMediaQuery } from "../../lib/useMediaQuery";
import { useProductionApi } from "../../api/production-api";
import { Alert } from "../ui/Alert";
import { SessionLoadingScreen, SessionStatusScreen, SignOutRetryKey } from "./SessionChrome";
import { Breadcrumbs } from "./Breadcrumbs";
import { ThemeToggle } from "./ThemeToggle";
import { useTheme } from "../../hooks/useTheme";

const DESTINATION_ROUTES: Record<string, { label: string; route: string; abbr: string }> = {
  home: { label: "Home", route: "/", abbr: "HOM" },
  activities: { label: "Activities", route: "/activities", abbr: "ACT" },
  "my-work": { label: "My work", route: "/my-work", abbr: "WRK" },
};

function operatorRole(relationship: string | undefined, destinationIds: string[]): OperatorRole {
  const rel = relationship?.toLowerCase() ?? "";
  if (rel.includes("review")) {
    return "Reviewer";
  }
  if (rel.includes("admin")) {
    return "Administrator";
  }
  if (rel.includes("participant")) {
    return "Participant";
  }
  if (destinationIds.includes("activities")) {
    return "Administrator";
  }
  return "Participant";
}

export function ProductionAppShell() {
  const { apiState, errorMessage, logout, shell } = useProductionApi();
  const { theme, toggleTheme } = useTheme();
  const location = useLocation();
  const [gangwayCollapsed, setGangwayCollapsed] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const isDrawerLayout = useMediaQuery(maxWidthQuery("adminDrawer"));

  if (apiState === "loading") {
    return <SessionLoadingScreen />;
  }

  if (apiState === "signing-out") {
    return (
      <SessionStatusScreen title="Signing out">
        <p role={errorMessage ? "alert" : undefined}>{errorMessage ?? "Signing out…"}</p>
        {errorMessage ? (
          <SignOutRetryKey onRetry={() => { void logout(); }} />
        ) : null}
      </SessionStatusScreen>
    );
  }

  if (apiState === "denied") {
    return (
      <SessionStatusScreen title="Your access changed" variant="danger">
        <p>{errorMessage ?? "You do not have access to this content."}</p>
      </SessionStatusScreen>
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

  const destinationIds = destinations.map((item) => item.id);
  const role = operatorRole(shell?.relationship, destinationIds);
  const identity: OperatorIdentity = {
    shortId: "ORG",
    fullId: "Organization",
    role,
    home: "/",
  };

  const groups: GangwayGroup[] = [
    {
      id: "workspace",
      label: "Workspace",
      items: destinations.map((destination) => ({
        to: destination.route,
        label: destination.label,
        abbr: destination.abbr,
        current:
          destination.route === "/"
            ? location.pathname === "/"
            : location.pathname === destination.route || location.pathname.startsWith(`${destination.route}/`),
      })),
    },
  ];

  const currentLabel = destinations.find((destination) => {
    if (destination.route === "/") {
      return location.pathname === "/";
    }
    return location.pathname === destination.route || location.pathname.startsWith(`${destination.route}/`);
  })?.label ?? "Home";

  return (
    <div className="workspace-root">
      <a href="#main-content" className="skip-link">Skip to main content</a>
      <CommandStrip
        homeTo="/"
        homeLabel="Home"
        origin
        readout="Organization"
        profile={identity}
        identLeading={(
          <>
            {!isDrawerLayout ? <ThemeToggle theme={theme} onToggle={toggleTheme} /> : null}
            <Key variant="quiet" size="compact" onClick={() => { void logout(); }}>
              Sign out
            </Key>
          </>
        )}
        actions={isDrawerLayout
          ? [{
              id: "theme",
              label: theme === "dark" ? "Switch to light theme" : "Switch to dark theme",
              state: "enabled",
              onSelect: toggleTheme,
            }]
          : []}
      />
      {apiState === "ready" && errorMessage ? (
        <Alert variant="danger" title="Request could not be completed">{errorMessage}</Alert>
      ) : null}

      <div className="workspace-shell">
        {!isDrawerLayout ? (
          <Gangway
            title={role}
            groups={groups}
            collapsed={gangwayCollapsed}
            onCollapsedChange={setGangwayCollapsed}
            ariaLabel="Primary navigation"
          />
        ) : null}
        <div className="workspace-content">
          {isDrawerLayout ? (
            <div className="workspace-drawer-bar">
              <span className="workspace-drawer-label">{currentLabel}</span>
              <Key
                size="compact"
                ariaExpanded={navOpen}
                ariaControls="workspaceNavBulkhead"
                onClick={() => setNavOpen(true)}
              >
                Menu
              </Key>
            </div>
          ) : null}
          <div id="main-content" className="workspace-main">
            <Breadcrumbs />
            <Outlet />
          </div>
        </div>
      </div>
      <Bulkhead
        id="workspaceNavBulkhead"
        open={isDrawerLayout && navOpen}
        onClose={() => setNavOpen(false)}
        title={role}
        titleId="workspaceNavBulkheadTitle"
      >
        <nav className="nav-rail" aria-label="Primary navigation">
          <AreaGroupList groups={groups} variant="rail" onNavigate={() => setNavOpen(false)} />
        </nav>
      </Bulkhead>
    </div>
  );
}
