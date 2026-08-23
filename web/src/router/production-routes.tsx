import type { ReactNode } from "react";
import { Link, Navigate, createBrowserRouter } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { ProductionAppShell } from "../components/shell/ProductionAppShell";
import { Button } from "../components/ui/Button";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";
import { ProductionActivitiesPage } from "../pages/ProductionActivitiesPage";
import { ProductionAssessmentSetupRoute } from "../pages/ProductionAssessmentSetupRoute";
import { ProductionAuthGatePage } from "../pages/ProductionAuthGatePage";
import { ProductionEnrollmentDetailPage } from "../pages/ProductionEnrollmentDetailPage";
import { ProductionEnrollmentPage } from "../pages/ProductionEnrollmentPage";
import { ProductionHomePage } from "../pages/ProductionHomePage";
import { ProductionMyWorkDetailPage } from "../pages/ProductionMyWorkDetailPage";
import { ProductionMyWorkPage } from "../pages/ProductionMyWorkPage";

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
    <StatusPanel title="Access denied" variant="danger">
      <p>{unavailableCopy}</p>
      <p><Link to="/">Return to Home</Link></p>
    </StatusPanel>
  );
}

function ProductionGate() {
  const { apiState, errorMessage, logout } = useProductionApi();

  if (apiState === "loading") {
    return <ProtectedLoading label="Establishing session context…" />;
  }

  if (apiState === "signing-out") {
    return (
      <StatusPanel title="Signing out">
        <p role={errorMessage ? "alert" : undefined}>{errorMessage ?? "Signing out…"}</p>
        {errorMessage ? (
          <p>
            <Button type="button" onClick={() => { void logout(); }}>Try again</Button>
          </p>
        ) : null}
      </StatusPanel>
    );
  }

  if (apiState === "idle") {
    return <ProductionAuthGatePage />;
  }

  if (apiState === "denied") {
    return (
      <StatusPanel title="Your access changed" variant="danger">
        <p>{errorMessage ?? "This destination is not available for the current authorized relationship."}</p>
      </StatusPanel>
    );
  }

  return <ProductionAppShell />;
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
            <ProductionAssessmentSetupRoute />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "activities/:activityId/cohorts/:cohortId/participants",
        element: (
          <ProductionDestinationGuard destinationId="activities" unavailableCopy="Activities are not available for the current authorized relationship.">
            <ProductionEnrollmentPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "activities/:activityId/cohorts/:cohortId/enrollments/:enrollmentId",
        element: (
          <ProductionDestinationGuard destinationId="activities" unavailableCopy="Activities are not available for the current authorized relationship.">
            <ProductionEnrollmentDetailPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "my-work",
        element: (
          <ProductionDestinationGuard destinationId="my-work" unavailableCopy="My work is not available for the current authorized relationship.">
            <ProductionMyWorkPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "my-work/:enrollmentId",
        element: (
          <ProductionDestinationGuard destinationId="my-work" unavailableCopy="My work is not available for the current authorized relationship.">
            <ProductionMyWorkDetailPage />
          </ProductionDestinationGuard>
        ),
      },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
