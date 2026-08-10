import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import { AppShell } from "../components/shell/AppShell";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";
import { ActivitiesPage } from "../pages/ActivitiesPage";
import { ActivityDetailPage } from "../pages/ActivityDetailPage";
import { AgentsPage } from "../pages/AgentsPage";
import { AuthGatePage } from "../pages/AuthGatePage";
import { EnrollmentPage } from "../pages/EnrollmentPage";
import { GovernancePage } from "../pages/GovernancePage";
import { HarnessesPage } from "../pages/HarnessesPage";
import { HomePage } from "../pages/HomePage";
import { MyWorkPage } from "../pages/MyWorkPage";
import { ReleaseDetailPage } from "../pages/ReleaseDetailPage";
import { ReleaseWorkPage } from "../pages/ReleaseWorkPage";
import { ResultDetailPage } from "../pages/ResultDetailPage";
import { ResultsPage } from "../pages/ResultsPage";
import { ReviewCasePage } from "../pages/ReviewCasePage";
import { ReviewWorkPage } from "../pages/ReviewWorkPage";
import { SessionPage } from "../pages/SessionPage";

function DestinationGuard({
  destinationId,
  allowPlannedTier = false,
  children,
}: {
  destinationId: string;
  allowPlannedTier?: boolean;
  children: ReactNode;
}) {
  const { navigation } = useBrowserApi();
  const destination = navigation?.destinations.find((item) => item.destination_id === destinationId);

  if (!destination) {
    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>This destination is not available for your actor context.</p>
      </StatusPanel>
    );
  }

  if (!destination.is_available) {
    if (allowPlannedTier && destination.tier === "p1") {
      return children;
    }

    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>{destination.unavailable_reason ?? "You do not have access to this destination."}</p>
      </StatusPanel>
    );
  }

  return children;
}

function SessionGuard({ children }: { children: ReactNode }) {
  const { actor } = useBrowserApi();
  const canAccess =
    actor?.capabilities.includes("participant") ||
    actor?.capabilities.includes("session_control");

  if (!canAccess) {
    return (
      <StatusPanel title="Access denied" variant="danger">
        <p>You do not have access to session surfaces.</p>
      </StatusPanel>
    );
  }

  return children;
}

function ActivityChildGuard({ children }: { children: ReactNode }) {
  return <DestinationGuard destinationId="activities">{children}</DestinationGuard>;
}

export function AppRoutes() {
  const { apiState } = useBrowserApi();

  if (apiState === "loading") {
    return <ProtectedLoading label="Establishing session context…" />;
  }

  if (apiState === "idle") {
    return <AuthGatePage />;
  }

  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route
          index
          element={
            <DestinationGuard destinationId="home">
              <HomePage />
            </DestinationGuard>
          }
        />
        <Route
          path="activities"
          element={
            <DestinationGuard destinationId="activities">
              <ActivitiesPage />
            </DestinationGuard>
          }
        />
        <Route
          path="activities/:activityId"
          element={
            <ActivityChildGuard>
              <ActivityDetailPage />
            </ActivityChildGuard>
          }
        />
        <Route
          path="activities/:activityId/enrollment"
          element={
            <ActivityChildGuard>
              <EnrollmentPage />
            </ActivityChildGuard>
          }
        />
        <Route
          path="my-work"
          element={
            <DestinationGuard destinationId="my-work">
              <MyWorkPage />
            </DestinationGuard>
          }
        />
        <Route
          path="sessions/:sessionId"
          element={
            <SessionGuard>
              <SessionPage />
            </SessionGuard>
          }
        />
        <Route
          path="review-work"
          element={
            <DestinationGuard destinationId="review-work">
              <ReviewWorkPage />
            </DestinationGuard>
          }
        />
        <Route
          path="review-work/:caseId"
          element={
            <DestinationGuard destinationId="review-work">
              <ReviewCasePage />
            </DestinationGuard>
          }
        />
        <Route
          path="release-work"
          element={
            <DestinationGuard destinationId="release-work">
              <ReleaseWorkPage />
            </DestinationGuard>
          }
        />
        <Route
          path="release-work/:releaseId"
          element={
            <DestinationGuard destinationId="release-work">
              <ReleaseDetailPage />
            </DestinationGuard>
          }
        />
        <Route
          path="results"
          element={
            <DestinationGuard destinationId="results">
              <ResultsPage />
            </DestinationGuard>
          }
        />
        <Route
          path="results/:resultId"
          element={
            <DestinationGuard destinationId="results">
              <ResultDetailPage />
            </DestinationGuard>
          }
        />
        <Route
          path="governance"
          element={
            <DestinationGuard destinationId="governance">
              <GovernancePage />
            </DestinationGuard>
          }
        />
        <Route
          path="agents"
          element={
            <DestinationGuard destinationId="agents" allowPlannedTier>
              <AgentsPage />
            </DestinationGuard>
          }
        />
        <Route
          path="harnesses"
          element={
            <DestinationGuard destinationId="harnesses" allowPlannedTier>
              <HarnessesPage />
            </DestinationGuard>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
