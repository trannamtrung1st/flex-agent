import type { ReactNode } from "react";
import { Navigate, createBrowserRouter, useLocation } from "react-router-dom";
import { ProtectedAuthSubtree, useProductionApi } from "../api/production-api";
import { ProductionAppShell } from "../components/shell/ProductionAppShell";
import { AccessChangedScreen, SessionLoadingScreen, SigningOutScreen } from "../components/shell/SessionChrome";
import { ContractUnavailablePage } from "../pages/ContractUnavailablePage";
import { UnknownDestinationPage } from "../pages/UnknownDestinationPage";
import { ProductionActivitiesPage } from "../pages/ProductionActivitiesPage";
import { ProductionCampaignCreatePage } from "../pages/ProductionCampaignCreatePage";
import { ProductionAssessmentSetupRoute } from "../pages/ProductionAssessmentSetupRoute";
import { ProductionAuthGatePage } from "../pages/ProductionAuthGatePage";
import { ProductionEnrollmentDetailPage } from "../pages/ProductionEnrollmentDetailPage";
import { ProductionEnrollmentPage } from "../pages/ProductionEnrollmentPage";
import { ProductionHomePage } from "../pages/ProductionHomePage";
import { ProductionMyWorkDetailPage } from "../pages/ProductionMyWorkDetailPage";
import { ProductionMyWorkPage } from "../pages/ProductionMyWorkPage";
import { AssignmentStationLayout } from "../components/work/AssignmentStationLayout";
import { AssignmentHead } from "../components/work/AssignmentHead";
import { CeremonyUnavailable, GuidedTaskFoot, Key, WorkWell, WorkWellSection } from "../design-system";
import { isProductionDestinationOpen, productionDestinationUnavailableCopy, productionWorkspaceHome } from "./production-navigation";
import { PRODUCTION_ROUTE_LAYOUTS } from "./production-route-layouts";
import { layoutIdForPath } from "./route-layout-match";

export { isProductionDestinationOpen };

export function ProductionDestinationGuard({
  destinationId,
  children,
}: {
  destinationId: "activities" | "my-work" | "review" | "release" | "results" | "sessions";
  children: ReactNode;
}) {
  const { shell } = useProductionApi();
  const location = useLocation();
  const available = isProductionDestinationOpen(shell?.navigation, destinationId);
  if (available) {
    return children;
  }

  const note = productionDestinationUnavailableCopy(destinationId);
  const homeTo = productionWorkspaceHome(shell?.navigation);
  if (layoutIdForPath(location.pathname, PRODUCTION_ROUTE_LAYOUTS) === "guided-task") {
    return (
      <AssignmentStationLayout
        instruments={null}
        heading={<AssignmentHead title="Access denied" />}
        actions={(
          <GuidedTaskFoot arrangement="end">
            <Key variant="quiet" to={homeTo}>Return to Home</Key>
          </GuidedTaskFoot>
        )}
      >
        <WorkWell live={false} label="Access denied">
          <WorkWellSection>
            <p>{note}</p>
          </WorkWellSection>
        </WorkWell>
      </AssignmentStationLayout>
    );
  }

  return (
    <CeremonyUnavailable
      title="Access denied"
      note={note}
      danger
      recovery={{ label: "Return to Home", to: homeTo }}
    />
  );
}

function ProductionContractUnavailable({ title, note }: { title: string; note: string }) {
  const { shell } = useProductionApi();
  return (
    <ContractUnavailablePage
      title={title}
      note={note}
      homeTo={productionWorkspaceHome(shell?.navigation)}
    />
  );
}

function ProductionGate() {
  const { apiState, errorMessage, logout } = useProductionApi();

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

  if (apiState === "idle") {
    return <ProductionAuthGatePage />;
  }

  if (apiState === "denied") {
    return <AccessChangedScreen />;
  }

  return (
    <ProtectedAuthSubtree>
      <ProductionAppShell />
    </ProtectedAuthSubtree>
  );
}

export function createProductionRouter() {
  return createBrowserRouter([
  {
    path: "/",
    element: <ProductionGate />,
    children: [
      { index: true, element: <ProductionHomePage /> },
      {
        path: "activities",
        element: (
          <ProductionDestinationGuard destinationId="activities">
            <ProductionActivitiesPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "activities/new",
        element: (
          <ProductionDestinationGuard destinationId="activities">
            <ProductionCampaignCreatePage />
          </ProductionDestinationGuard>
        ),
      },
      { path: "activities/:activityId", element: <Navigate to="setup" replace /> },
      {
        path: "activities/:activityId/setup",
        element: (
          <ProductionDestinationGuard destinationId="activities">
            <ProductionAssessmentSetupRoute />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "activities/:activityId/cohorts/:cohortId/enrollments",
        element: (
          <ProductionDestinationGuard destinationId="activities">
            <ProductionEnrollmentPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "activities/:activityId/cohorts/:cohortId/enrollments/:enrollmentId",
        element: (
          <ProductionDestinationGuard destinationId="activities">
            <ProductionEnrollmentDetailPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "my-work",
        element: (
          <ProductionDestinationGuard destinationId="my-work">
            <ProductionMyWorkPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "my-work/:enrollmentId",
        element: (
          <ProductionDestinationGuard destinationId="my-work">
            <ProductionMyWorkDetailPage />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "sessions/:sessionId",
        element: (
          <ProductionDestinationGuard destinationId="sessions">
            <ProductionContractUnavailable
              title="Text Session"
              note="Session command and snapshot HTTP are not exposed to this SPA. The host maps SSE events only. The Session remains on the server."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "review",
        element: (
          <ProductionDestinationGuard destinationId="review">
            <ProductionContractUnavailable
              title="Review work"
              note="Review-case APIs are not exposed to this SPA yet. Evaluation, Human revision, and Review decision remain distinct server objects."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "review/:reviewId",
        element: (
          <ProductionDestinationGuard destinationId="review">
            <ProductionContractUnavailable
              title="Review case"
              note="This locator is not backed by a production Review API in the current contract set."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "release",
        element: (
          <ProductionDestinationGuard destinationId="release">
            <ProductionContractUnavailable
              title="Release work"
              note="Release APIs are not exposed to this SPA yet. Release remains independent of Review approval."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "release/:resultId",
        element: (
          <ProductionDestinationGuard destinationId="release">
            <ProductionContractUnavailable
              title="Release Result"
              note="This locator is not backed by a production Release API in the current contract set."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "results",
        element: (
          <ProductionDestinationGuard destinationId="results">
            <ProductionContractUnavailable
              title="Results"
              note="Participant Result visibility is server-owned. This SPA has no Result list contract yet, so the destination stays unavailable rather than inventing scores."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "results/:resultId",
        element: (
          <ProductionDestinationGuard destinationId="results">
            <ProductionContractUnavailable
              title="Result"
              note="Participant Result visibility is server-owned. This SPA has no Result read contract yet, so the view stays unavailable rather than inventing a score."
            />
          </ProductionDestinationGuard>
        ),
      },
      { path: "*", element: <UnknownDestinationPage /> },
    ],
  },
  ]);
}

export const productionRouter = createProductionRouter();
