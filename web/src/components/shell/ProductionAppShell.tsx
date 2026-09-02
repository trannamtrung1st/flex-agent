import { Outlet, useLocation } from "react-router-dom";
import {
  Alert,
  LayoutAssignment,
  ManagementLayout,
  operatorAccountActions,
  ToastHost,
} from "../../design-system";
import { layoutIdForPath } from "../../router/route-layout-match";
import { PRODUCTION_ROUTE_LAYOUTS } from "../../router/production-route-layouts";
import { availableProductionDestinations, productionNavGroups, shouldHideProductionBreadcrumbs } from "../../router/production-navigation";
import { requireProductionShellLayout } from "../../router/require-production-shell-layout";
import { useProductionApi } from "../../api/production-api";
import { AccessChangedScreen, SessionLoadingScreen, SigningOutScreen } from "./SessionChrome";
import { Breadcrumbs } from "./Breadcrumbs";
import { useTheme } from "../../lib/useTheme";
import { productionOperatorIdentity } from "./production-operator";

export function ProductionAppShell() {
  const { apiState, errorMessage, logout, shell } = useProductionApi();
  const { theme, toggleTheme } = useTheme();
  const location = useLocation();

  if (apiState === "loading") {
    return <SessionLoadingScreen />;
  }

  if (apiState === "signing-out") {
    return (
      <SigningOutScreen
        errorMessage={errorMessage}
        onRetry={() => { void logout(); }}
      />
    );
  }

  if (apiState === "denied") {
    return <AccessChangedScreen />;
  }

  const destinations = availableProductionDestinations(shell?.navigation);
  const destinationIds = destinations.map((item) => item.id);
  const groups = productionNavGroups(destinations, location.pathname);
  const identity = productionOperatorIdentity(shell?.relationship, destinationIds, shell?.display_name);

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

  if (assigned === "guided-task" || assigned === "live-session") {
    return (
      <ToastHost>
        <LayoutAssignment id={assigned}>
          <Outlet />
        </LayoutAssignment>
      </ToastHost>
    );
  }

  return (
    <ToastHost>
      <LayoutAssignment id={assigned}>
        <ManagementLayout
          contain={false}
          commandStrip={{
            homeTo: identity.home,
            homeLabel: "Home",
            profile: identity,
            actions: operatorAccountActions(theme, toggleTheme, () => { void logout(); }),
          }}
          navigation={{
            title: identity.role,
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
    </ToastHost>
  );
}
