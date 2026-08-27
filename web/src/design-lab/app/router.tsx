import { createBrowserRouter, Navigate, useSearchParams, type RouteObject } from "react-router-dom";
import type { ReactElement } from "react";
import { LayoutAssignment } from "../../design-system";
import { CampaignsArea } from "../features/admin/CampaignsArea";
import { EnrollmentsArea } from "../features/admin/EnrollmentsArea";
import {
  AuditLogArea,
  CohortsArea,
  PoliciesArea,
  SessionsArea,
  UsersAccessArea,
} from "../features/admin/sampleAreas";
import { AdminPage } from "../routes/AdminPage";
import { GalleryPage } from "../routes/GalleryPage";
import { HomePage } from "../routes/HomePage";
import { JourneyPage } from "../routes/JourneyPage";
import { NotFoundPage } from "../routes/NotFoundPage";
import { ReviewerPage } from "../routes/ReviewerPage";
import { SessionPage } from "../routes/SessionPage";
import { SurfacesPage } from "../routes/SurfacesPage";
import { DESIGN_LAB_ROUTE_LAYOUTS, type DesignLabRoutedPath } from "./design-lab-route-layouts";

function assignLabLayout(path: DesignLabRoutedPath, element: ReactElement) {
  return <LayoutAssignment id={DESIGN_LAB_ROUTE_LAYOUTS[path]}>{element}</LayoutAssignment>;
}

function AdminIndexRedirect() {
  const [searchParams] = useSearchParams();
  return <Navigate to={{ pathname: "enrollments", search: searchParams.toString() }} replace />;
}

export const designLabRoutes: RouteObject[] = [
  { index: true, element: <Navigate to="/surfaces" replace /> },
  { path: "/surfaces", element: assignLabLayout("/surfaces", <SurfacesPage />) },
  { path: "/participant-home", element: assignLabLayout("/participant-home", <HomePage />) },
  { path: "/participant-journey", element: assignLabLayout("/participant-journey", <JourneyPage />) },
  { path: "/participant-session", element: assignLabLayout("/participant-session", <SessionPage />) },
  {
    path: "/admin-console",
    element: assignLabLayout("/admin-console", <AdminPage />),
    children: [
      { index: true, element: <AdminIndexRedirect /> },
      { path: "campaigns", element: <CampaignsArea /> },
      { path: "cohorts", element: <CohortsArea /> },
      { path: "enrollments", element: <EnrollmentsArea /> },
      { path: "sessions", element: <SessionsArea /> },
      { path: "users-access", element: <UsersAccessArea /> },
      { path: "policies", element: <PoliciesArea /> },
      { path: "audit-log", element: <AuditLogArea /> },
    ],
  },
  { path: "/reviewer-console", element: assignLabLayout("/reviewer-console", <ReviewerPage />) },
  { path: "/shared/gallery", element: assignLabLayout("/shared/gallery", <GalleryPage />) },
  { path: "*", element: assignLabLayout("*", <NotFoundPage />) },
];

export const DESIGN_LAB_BASENAME = "/design-lab";

export const designLabRouter = createBrowserRouter(designLabRoutes, { basename: DESIGN_LAB_BASENAME });
