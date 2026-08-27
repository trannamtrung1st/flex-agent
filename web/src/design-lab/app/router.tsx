import { createBrowserRouter, Navigate, useSearchParams, type RouteObject } from "react-router-dom";
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

function AdminIndexRedirect() {
  const [searchParams] = useSearchParams();
  return <Navigate to={{ pathname: "enrollments", search: searchParams.toString() }} replace />;
}

export const designLabRoutes: RouteObject[] = [
  { index: true, element: <Navigate to="/surfaces" replace /> },
  { path: "/surfaces", element: <SurfacesPage /> },
  { path: "/participant-home", element: <HomePage /> },
  { path: "/participant-journey", element: <JourneyPage /> },
  { path: "/participant-session", element: <SessionPage /> },
  {
    path: "/admin-console",
    element: <AdminPage />,
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
  { path: "/reviewer-console", element: <ReviewerPage /> },
  { path: "/shared/gallery", element: <GalleryPage /> },
  { path: "*", element: <NotFoundPage /> },
];

export const DESIGN_LAB_BASENAME = "/design-lab";

export const designLabRouter = createBrowserRouter(designLabRoutes, { basename: DESIGN_LAB_BASENAME });
