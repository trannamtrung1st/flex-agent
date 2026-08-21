import { Navigate, createBrowserRouter, useLocation } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { ProductionAppShell } from "../components/shell/ProductionAppShell";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";
import { ProductionActivitiesPage } from "../pages/ProductionActivitiesPage";
import { ProductionAssessmentSetupRoute } from "../pages/ProductionAssessmentSetupRoute";
import { ProductionAuthGatePage } from "../pages/ProductionAuthGatePage";
import { ProductionHomePage } from "../pages/ProductionHomePage";

function ProductionGate() {
  const location = useLocation();
  const { apiState, errorMessage, shell } = useProductionApi();

  if (apiState === "loading") {
    return <ProtectedLoading label="Establishing session context…" />;
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

  const activitiesAvailable = shell?.navigation.some(
    (item) => item.destination_id === "activities" && item.is_available,
  );

  if (!activitiesAvailable && location.pathname.startsWith("/activities")) {
    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>Activities are not available for the current authorized relationship.</p>
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
      { path: "activities", element: <ProductionActivitiesPage /> },
      { path: "activities/:activityId", element: <Navigate to="setup" replace /> },
      { path: "activities/:activityId/setup", element: <ProductionAssessmentSetupRoute /> },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
