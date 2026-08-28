import { Outlet, useLocation } from "react-router-dom";
import {
  Alert,
  Key,
  LayoutAssignment,
  ManagementLayout,
  type OperatorIdentity,
  type OperatorRole,
} from "../../design-system";
import { maxWidthQuery } from "../../lib/breakpoints";
import { layoutIdForPath } from "../../router/route-layout-match";
import { PRODUCTION_ROUTE_LAYOUTS } from "../../router/production-route-layouts";
import { availableProductionDestinations, productionNavGroups, shouldHideProductionBreadcrumbs } from "../../router/production-navigation";
import { requireProductionShellLayout } from "../../router/require-production-shell-layout";
import { useMediaQuery } from "../../lib/useMediaQuery";
import { useProductionApi } from "../../api/production-api";
import { AccessChangedScreen, CeremonyEmpty, SessionLoadingScreen, SessionStatusScreen, SignOutRetryKey } from "./SessionChrome";
import { Breadcrumbs } from "./Breadcrumbs";
import { ThemeToggle } from "./ThemeToggle";
import { useTheme } from "../../hooks/useTheme";

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
  const isDrawerLayout = useMediaQuery(maxWidthQuery("adminDrawer"));

  if (apiState === "loading") {
    return <SessionLoadingScreen />;
  }

  if (apiState === "signing-out") {
    return (
      <SessionStatusScreen title="Signing out">
        <CeremonyEmpty note={errorMessage ?? "Signing out…"} alert={Boolean(errorMessage)}>
          {errorMessage ? (
            <SignOutRetryKey onRetry={() => { void logout(); }} />
          ) : null}
        </CeremonyEmpty>
      </SessionStatusScreen>
    );
  }

  if (apiState === "denied") {
    return <AccessChangedScreen />;
  }

  const destinations = availableProductionDestinations(shell?.navigation);
  const destinationIds = destinations.map((item) => item.id);
  const groups = productionNavGroups(destinations, location.pathname);
  const role = operatorRole(shell?.relationship, destinationIds);
  const identity: OperatorIdentity = {
    shortId: "ORG",
    fullId: "Organization",
    role,
    home: "/",
  };

  const currentLabel = destinations.find((destination) => {
    if (destination.route === "/") {
      return location.pathname === "/";
    }
    return location.pathname === destination.route || location.pathname.startsWith(`${destination.route}/`);
  })?.label ?? "Home";

  const assigned = requireProductionShellLayout(
    layoutIdForPath(location.pathname, PRODUCTION_ROUTE_LAYOUTS),
    location.pathname,
  );

  return (
    <LayoutAssignment id={assigned}>
      <ManagementLayout
        contain={false}
        commandStrip={{
          homeTo: "/",
          homeLabel: "Home",
          origin: true,
          readout: "Organization",
          profile: identity,
          identLeading: (
            <>
              {!isDrawerLayout ? <ThemeToggle theme={theme} onToggle={toggleTheme} /> : null}
              <Key variant="quiet" size="compact" onClick={() => { void logout(); }}>
                Sign out
              </Key>
            </>
          ),
          actions: isDrawerLayout
            ? [{
                id: "theme",
                label: theme === "dark" ? "Switch to light theme" : "Switch to dark theme",
                state: "enabled",
                onSelect: toggleTheme,
              }]
            : [],
        }}
        navigation={{
          title: role,
          groups,
          currentLabel,
          ariaLabel: "Primary navigation",
          bulkheadId: "workspaceNavBulkhead",
        }}
        banner={apiState === "ready" && errorMessage ? (
          <Alert variant="danger" title="Request could not be completed">{errorMessage}</Alert>
        ) : null}
        breadcrumbs={shouldHideProductionBreadcrumbs(location.pathname, shell?.navigation)
          ? null
          : <Breadcrumbs />}
      >
        <Outlet />
      </ManagementLayout>
    </LayoutAssignment>
  );
}
