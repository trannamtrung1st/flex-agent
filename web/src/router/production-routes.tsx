import type { ReactNode } from "react";
import { Navigate, createBrowserRouter } from "react-router-dom";
import { ProtectedAuthSubtree, useProductionApi } from "../api/production-api";
import { ProductionAppShell } from "../components/shell/ProductionAppShell";
import { SessionLoadingScreen, SessionStatusScreen, SignOutRetryKey } from "../components/shell/SessionChrome";
import { ProductionActivitiesPage } from "../pages/ProductionActivitiesPage";
import { ProductionAuthGatePage } from "../pages/ProductionAuthGatePage";
import { ProductionHomePage } from "../pages/ProductionHomePage";
import { LaterWaveDestinationPage } from "../pages/LaterWaveDestinationPage";
import { Key, OperateArea } from "../design-system";

export function ProductionDestinationGuard({
  destinationId,
  unavailableCopy,
  children,
}: {
  destinationId: "activities" | "my-work";
  unavailableCopy: string;
  children: ReactNode;
}) {
  const { shell } = useProductionApi();
  const available = shell?.navigation.some((item) => item.destination_id === destinationId && item.is_available);
  if (available) {
    return children;
  }

  return (
    <OperateArea
      className="workspace-area workspace-area--danger"
      label="Access denied"
      title="Access denied"
    >
      <p>{unavailableCopy}</p>
      <p>
        <Key variant="open" to="/">Return to Home</Key>
      </p>
    </OperateArea>
  );
}

function ProductionGate() {
  const { apiState, errorMessage, logout } = useProductionApi();

  if (apiState === "loading") {
    return <SessionLoadingScreen />;
  }

  if (apiState === "signing-out") {
    return (
      <SessionStatusScreen title="Signing out">
        <p role={errorMessage ? "alert" : undefined}>{errorMessage ?? "Signing out…"}</p>
        {errorMessage ? (
          <p>
            <SignOutRetryKey onRetry={() => { void logout(); }} />
          </p>
        ) : null}
      </SessionStatusScreen>
    );
  }

  if (apiState === "idle") {
    return <ProductionAuthGatePage />;
  }

  if (apiState === "denied") {
    return (
      <SessionStatusScreen title="Your access changed" variant="danger">
        <p>This destination is not available for the current authorized relationship.</p>
      </SessionStatusScreen>
    );
  }

  return (
    <ProtectedAuthSubtree>
      <ProductionAppShell />
    </ProtectedAuthSubtree>
  );
}

export const productionRouter = createBrowserRouter([
  {
    path: "/",
    element: <ProductionGate />,
    children: [
      { index: true, element: <ProductionHomePage /> },
      {
        path: "activities",
        element: (
          <ProductionDestinationGuard destinationId="activities" unavailableCopy="Activities are not available for the current authorized relationship.">
            <ProductionActivitiesPage />
          </ProductionDestinationGuard>
        ),
      },
      { path: "activities/:activityId", element: <Navigate to="setup" replace /> },
      {
        path: "activities/:activityId/setup",
        element: (
          <ProductionDestinationGuard destinationId="activities" unavailableCopy="Activities are not available for the current authorized relationship.">
            <LaterWaveDestinationPage
              title="Setup and readiness"
              note="Campaign setup is not connected in this candidate build. The Campaign remains on the server."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "my-work",
        element: (
          <ProductionDestinationGuard destinationId="my-work" unavailableCopy="My work is not available for the current authorized relationship.">
            <LaterWaveDestinationPage
              title="My work"
              note="Assignments are not connected in this candidate build."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "my-work/:enrollmentId",
        element: (
          <ProductionDestinationGuard destinationId="my-work" unavailableCopy="My work is not available for the current authorized relationship.">
            <LaterWaveDestinationPage
              title="Assignment"
              note="Assignment detail is not connected in this candidate build."
            />
          </ProductionDestinationGuard>
        ),
      },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
