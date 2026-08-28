import type { ReactNode } from "react";
import { Navigate, createBrowserRouter } from "react-router-dom";
import { ProtectedAuthSubtree, useProductionApi } from "../api/production-api";
import { ProductionAppShell } from "../components/shell/ProductionAppShell";
import { AccessChangedScreen, CeremonyArea, CeremonyEmpty, SessionLoadingScreen, SessionStatusScreen, SignOutRetryKey } from "../components/shell/SessionChrome";
import { ContractUnavailablePage } from "../pages/ContractUnavailablePage";
import { UnknownDestinationPage } from "../pages/UnknownDestinationPage";
import { ProductionActivitiesPage } from "../pages/ProductionActivitiesPage";
import { ProductionAssessmentSetupRoute } from "../pages/ProductionAssessmentSetupRoute";
import { ProductionAuthGatePage } from "../pages/ProductionAuthGatePage";
import { ProductionEnrollmentDetailPage } from "../pages/ProductionEnrollmentDetailPage";
import { ProductionEnrollmentPage } from "../pages/ProductionEnrollmentPage";
import { ProductionHomePage } from "../pages/ProductionHomePage";
import { ProductionMyWorkDetailPage } from "../pages/ProductionMyWorkDetailPage";
import { ProductionMyWorkPage } from "../pages/ProductionMyWorkPage";
import { Key } from "../design-system";
import { isProductionDestinationOpen } from "./production-navigation";

export { isProductionDestinationOpen };

export function ProductionDestinationGuard({
  destinationId,
  unavailableCopy,
  children,
}: {
  destinationId: "activities" | "my-work" | "review" | "release" | "results" | "sessions";
  unavailableCopy: string;
  children: ReactNode;
}) {
  const { shell } = useProductionApi();
  const available = isProductionDestinationOpen(shell?.navigation, destinationId);
  if (available) {
    return children;
  }

  return (
    <CeremonyArea
      label="Access denied"
      title="Access denied"
      danger
    >
      <CeremonyEmpty note={unavailableCopy}>
        <Key variant="open" to="/">Return to Home</Key>
      </CeremonyEmpty>
    </CeremonyArea>
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
        <CeremonyEmpty note={errorMessage ?? "Signing out…"} alert={Boolean(errorMessage)}>
          {errorMessage ? (
            <SignOutRetryKey onRetry={() => { void logout(); }} />
          ) : null}
        </CeremonyEmpty>
      </SessionStatusScreen>
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
        path: "activities/:activityId/cohorts/:cohortId/enrollments",
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
      {
        path: "sessions/:sessionId",
        element: (
          <ProductionDestinationGuard destinationId="sessions" unavailableCopy="Sessions are not available for the current authorized relationship.">
            <ContractUnavailablePage
              title="Text Session"
              note="Session command and snapshot HTTP are not exposed to this SPA. The host maps SSE events only. The Session remains on the server."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "review",
        element: (
          <ProductionDestinationGuard destinationId="review" unavailableCopy="Review work is not available for the current authorized relationship.">
            <ContractUnavailablePage
              title="Review work"
              note="Review-case APIs are not exposed to this SPA yet. Evaluation, Human revision, and Review decision remain distinct server objects."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "review/:reviewId",
        element: (
          <ProductionDestinationGuard destinationId="review" unavailableCopy="Review work is not available for the current authorized relationship.">
            <ContractUnavailablePage
              title="Review case"
              note="This locator is not backed by a production Review API in the current contract set."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "release",
        element: (
          <ProductionDestinationGuard destinationId="release" unavailableCopy="Release work is not available for the current authorized relationship.">
            <ContractUnavailablePage
              title="Release work"
              note="Release APIs are not exposed to this SPA yet. Release remains independent of Review approval."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "release/:resultId",
        element: (
          <ProductionDestinationGuard destinationId="release" unavailableCopy="Release work is not available for the current authorized relationship.">
            <ContractUnavailablePage
              title="Release Result"
              note="This locator is not backed by a production Release API in the current contract set."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "results",
        element: (
          <ProductionDestinationGuard destinationId="results" unavailableCopy="Results are not available for the current authorized relationship.">
            <ContractUnavailablePage
              title="Results"
              note="Participant Result visibility is server-owned. This SPA has no Result list contract yet, so the destination stays unavailable rather than inventing scores."
            />
          </ProductionDestinationGuard>
        ),
      },
      {
        path: "results/:resultId",
        element: (
          <ProductionDestinationGuard destinationId="results" unavailableCopy="Results are not available for the current authorized relationship.">
            <ContractUnavailablePage
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
